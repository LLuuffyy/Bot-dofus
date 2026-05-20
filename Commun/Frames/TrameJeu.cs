using BotDofus.Commun.Messages.VersClient.Authentification;
using BotDofus.Commun.Messages.VersClient.Base;
using BotDofus.Commun.Messages.VersClient.Chat;
using BotDofus.Commun.Messages.VersClient.Info;
using BotDofus.Commun.Messages.VersClient.Jeu;
using BotDofus.Commun.Messages.VersClient.Objet;
using BotDofus.Commun.Reseau;
using BotDofus.Divers;
using BotDofus.Divers.Cartes.Entites;
using BotDofus.Divers.Enums;
using BotDofus.Divers.Jeu;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Phase "en jeu" principale : hors combat, hors dialogue. Écoute les changements
/// de carte, les messages d'information, les mises à jour d'inventaire, les messages
/// de chat, et maintient le modèle <see cref="EtatJeu"/> à jour.
///
/// Cette trame peut être recouverte par <see cref="TrameCombat"/> ou
/// <see cref="TrameDialogue"/> de manière transitoire (empilement).
/// </summary>
public sealed class TrameJeu : TrameBase
{
    private readonly Compte _compte;
    private readonly EtatJeu _etat;
    private readonly SessionProxy _session;

    // Anti-spam logs : acteurs déjà annoncés, et dernier état de combat loggué.
    private readonly System.Collections.Generic.HashSet<int> _acteursVus = new();
    private int _dernierNbVivants = -1;
    private int _dernierNbCombattants = -1;

    public TrameJeu(Repartiteur repartiteur, Compte compte, EtatJeu etat, SessionProxy session)
        : base(repartiteur)
    {
        _compte = compte;
        _etat = etat;
        _session = session;
    }

    protected override void EnregistrerGestionnaires()
    {
        Ecouter<MessageDonneesCarte>(OnDonneesCarte);
        Ecouter<MessageDonneesCarteFin>(OnElementsInteractifs);
        Ecouter<MessageMetiersSkills>(OnMetiersSkills);
        Ecouter<MessageMetiersXp>(OnMetiersXp);
        Ecouter<MessageInfoMessage>(OnInfoMessage);
        Ecouter<MessageInfoVie>(OnInfoVie);
        Ecouter<MessageSelectionPersonnage>(OnSelectionPersonnage);
        Ecouter<MessageStats>(OnStats);
        Ecouter<BotDofus.Commun.Messages.VersClient.Authentification.MessageListeSorts>(msg =>
        {
            foreach (var kv in msg.Sorts) _etat.Personnage.SortsAppris[kv.Key] = kv.Value;
            Journaliseur.Info($"[SORTS] {msg.Sorts.Count} sort(s) scanné(s) : "
                + string.Join(", ", msg.Sorts.Select(s => $"#{s.Key} niv{s.Value}")));
        });
        Ecouter<MessageObjetAjout>(OnObjetAjout);
        Ecouter<MessageObjetRetrait>(OnObjetRetrait);
        Ecouter<MessageObjetQuantite>(OnObjetQuantite);
        Ecouter<MessageObjetPoids>(msg => _etat.Personnage.ActualiserPoids(msg.PoidsActuel, msg.PoidsMax));
        Ecouter<MessageMouvementCarte>(OnMouvementCarte);
        Ecouter<MessageActionJeu>(OnActionJeu);
        Ecouter<MessagePositionsCombat>(OnPositionsCombat);
        Ecouter<MessageChatMessage>(OnChatMessage);
        Ecouter<MessageChatServeur>(msg => Journaliseur.Info($"[SERVEUR] {msg.Texte}"));

        // Mises à jour de l'état combat (GS, GE, GT) — alimente Combat.Etat / NumeroTour pour la vue live.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageDebutCombat>(_ =>
        {
            _etat.Combat.Demarrer();
            _etat.Combat.PassageEnCombat();
            _compte.ChangerEtat(EtatsCompte.EnCombat);
            PeuplerCombatDepuisCarte();
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageFinCombat>(_ =>
        {
            _etat.Combat.Reinitialiser();
            _dernierNbVivants = -1;
            _dernierNbCombattants = -1;
            // Purge les combattants affichés sur la grille (sinon ils
            // restent collés après le combat — l'overworld n'a pas d'entités
            // en clair de toute façon).
            _etat.CarteCourante?.Entites.Clear();
            _etat.CarteCourante?.SignalerRechargee();
            _compte.ChangerEtat(EtatsCompte.EnJeu);
            Journaliseur.Info("[COMBAT] Combat terminé");
        });
        Ecouter<MessageTourCombat>(async msg =>
        {
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);
            // IA combat basique (capture user 12:41:11) : à mon tour, je cast
            // le sort offensif de plus haut niveau sur l'ennemi le plus
            // proche, puis je passe le tour. Si je n'ai aucun sort ou aucun
            // ennemi → pass turn direct. Pas de déplacement, pas de tactique
            // — l'IA avancée viendra dans un commit séparé.
            if (_session is null) return;
            if (msg.IdentifiantCombattant != _etat.Personnage.Identifiant) return;
            if (_compte.ModePassif)
            {
                Journaliseur.Info("[COMBAT] mode passif actif → IA désactivée.");
                return;
            }
            try
            {
                await JouerTourCombatAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[COMBAT] erreur IA tour : {ex.Message}");
                // Fail-safe : on passe le tour quoi qu'il arrive pour ne pas
                // bloquer le combat (sinon GTS timeout 45 s et perte de tour).
                try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
                catch { /* swallow */ }
            }
        });

        Ecouter<MessagePingMoyen>(_ => { /* silence ping */ });

        // Abrak v1.48 : acteurs map = famille N* (identité en clair, position
        // chiffrée). On loggue l'identité (nom/niveau) — exploitable pour la
        // détection de joueurs/staff même sans cellule.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageActeurAbrak>(msg =>
        {
            foreach (var a in msg.Spawns)
                if (_acteursVus.Add(a.Id)) // 1 ligne par acteur (anti-spam NLK/Nx)
                    Journaliseur.Info($"[ENT] acteur Abrak vu : « {a.Nom} » niv {a.Niveau} (#{a.Id})");
            foreach (var id in msg.Despawns)
                Journaliseur.Debogue($"[ENT] acteur Abrak parti : #{id}");
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageActeurAbrakRetrait>(msg =>
            Journaliseur.Debogue($"[ENT] acteur Abrak parti : #{msg.Identifiant}"));

        // === COMBAT ABRAK EN CLAIR : positions des combattants ===
        // GTM = liste combattants+cellules ; GTS = à qui le tour. C'est ICI
        // qu'on récupère enfin les entités positionnées (pour l'IA combat).
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageCombattantsAbrak>(OnCombattantsAbrak);
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageTourCombatAbrak>(async msg =>
        {
            if (!msg.EstTour) return; // GTSX (sorts) — pas un tour
            _etat.Combat.IdentifiantAllie = _etat.Personnage.Identifiant;
            _etat.Combat.PassageEnCombat();
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);
            Journaliseur.Info($"[COMBAT] Tour de #{msg.IdentifiantCombattant} (tour {msg.NumeroTour})"
                + (msg.IdentifiantCombattant == _etat.Personnage.Identifiant ? " ← MOI" : ""));

            // IA combat : capture user 12:41:18. Hystoria utilise le format
            // Abrak (GTS<id>|<temps>|<num>) → le handler classique GTSx ligne
            // 90 ne se déclenche JAMAIS sur ce serveur. C'est ICI qu'il faut
            // brancher l'IA, sinon le bot reste 45 s muet par tour (cf. log
            // 13:09 → 6 tours sans le moindre GA300).
            if (_session is null) return;
            if (msg.IdentifiantCombattant != _etat.Personnage.Identifiant) return;
            // Mode passif global : le bot n'agit jamais en auto (capture
            // protocole, observation, ou simplement « stop ! »). L'utilisateur
            // joue à la main, on n'interfère pas.
            if (_compte.ModePassif)
            {
                Journaliseur.Info("[COMBAT] mode passif actif → IA désactivée, à toi de jouer.");
                return;
            }
            try
            {
                await JouerTourCombatAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[COMBAT] erreur IA tour : {ex.Message}");
                try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
                catch { /* swallow */ }
            }
        });
    }

    private void OnCombattantsAbrak(BotDofus.Commun.Messages.VersClient.Jeu.MessageCombattantsAbrak msg)
    {
        if (msg.Combattants.Count == 0) return;

        _etat.Combat.IdentifiantAllie = _etat.Personnage.Identifiant;

        // CRITIQUE : le GTM est la liste AUTORITATIVE des combattants engagés.
        // Avant ce patch, Combat.Ennemis contenait aussi les mobs de la carte
        // ajoutés par PeuplerCombatDepuisCarte (groupes overworld pas encore
        // agressés). Au combat 15:03 (log 14:48), 5 mobs map en plus du vrai
        // protecteur → l'IA ciblait « Petit Tournesol Sauvage » #-3081 cell
        // 175 (groupe map à 5 cases) au lieu du protecteur #-1 cell 192
        // (combat réel à 8 cases). Cast GA300183;175 sur cell vide → serveur
        // kick (ObjectDisposedException sur NetworkStream).
        //
        // Fix : à chaque GTM reçu, on purge les entrées dont l'ID n'est PAS
        // dans le paquet — resync complète, conserve les vrais combattants
        // (qui sont updatés par le foreach ci-dessous).
        var idsRecus = new System.Collections.Generic.HashSet<int>(
            msg.Combattants.Select(c => c.Id));
        _etat.Combat.Allies.RemoveAll(a => !idsRecus.Contains(a.Identifiant));
        _etat.Combat.Ennemis.RemoveAll(e => !idsRecus.Contains(e.Identifiant));

        foreach (var c in msg.Combattants)
        {
            // Heuristique PvM Incarnam : id < 0 = monstre/ennemi, id > 0 = joueur/allié.
            bool ennemi = c.Id < 0;
            var liste = ennemi ? _etat.Combat.Ennemis : _etat.Combat.Allies;

            var existant = liste.FirstOrDefault(x => x.Identifiant == c.Id);
            if (existant == null)
            {
                existant = ennemi
                    ? new BotDofus.Divers.Combats.Combattants.CombattantMonstre { Identifiant = c.Id, Equipe = 1 }
                    : new BotDofus.Divers.Combats.Combattants.CombattantAllie { Identifiant = c.Id, Equipe = 0 };
                liste.Add(existant);
            }
            existant.CellulePosition = c.Cellule;
            existant.PV = c.Pv;
            existant.PVMax = c.PvMax;
            existant.PA = c.Pa;
            existant.PM = c.Pm;
            existant.EstMort = !c.Vivant;
        }

        // Affichage sur la GRILLE Carte : en combat, GTM donne les cellules
        // (en clair) — on peuple carte.Entites pour que les combattants
        // s'affichent enfin sur la map (overworld reste chiffré, mais le
        // combat NON). Monstres = id < 0, joueurs = id > 0.
        var carte = _etat.CarteCourante;
        if (carte != null)
        {
            foreach (var c in msg.Combattants)
            {
                if (!c.Vivant) { carte.Entites.TryRemove(c.Id, out _); continue; }
                if (c.Cellule <= 0) continue;
                if (c.Id < 0)
                    carte.Entites[c.Id] = new EntiteMonstre
                    {
                        Identifiant = c.Id, CellulePosition = c.Cellule,
                        Nom = $"Monstre {c.Id}", NiveauGroupe = 0
                    };
                else
                    carte.Entites[c.Id] = new EntiteJoueur
                    {
                        Identifiant = c.Id, CellulePosition = c.Cellule,
                        Nom = c.Id == _etat.Personnage.Identifiant ? _etat.Personnage.Nom : $"Joueur {c.Id}"
                    };
            }
            carte.SignalerRechargee();
        }

        _etat.Combat.SignalerCombattantsMaj();

        int vivants = msg.Combattants.Count(x => x.Vivant);
        // Anti-spam : GTM arrive à CHAQUE tour avec souvent la même compo.
        // On ne loggue le détail que si la composition change (mort, arrivée).
        if (vivants != _dernierNbVivants || msg.Combattants.Count != _dernierNbCombattants)
        {
            _dernierNbVivants = vivants;
            _dernierNbCombattants = msg.Combattants.Count;
            Journaliseur.Info(
                $"[COMBAT] {msg.Combattants.Count} combattant(s) ({vivants} vivants) — "
                + $"alliés={_etat.Combat.Allies.Count} ennemis={_etat.Combat.Ennemis.Count} "
                + $"| cellules: {string.Join(",", msg.Combattants.Select(x => $"#{x.Id}@{x.Cellule}"))}");
        }
    }

    private void OnSelectionPersonnage(MessageSelectionPersonnage msg)
    {
        // ASK : sert à la fois à confirmer la sélection ET à pousser l'identité du perso (nom/niveau/classe).
        var perso = _etat.Personnage;
        perso.Identifiant = msg.Identifiant;
        perso.Nom = msg.Nom;
        perso.Niveau = msg.Niveau;
        perso.IdClasse = msg.IdClasse;
        _compte.PseudoAffiche = msg.Nom;

        // Inventaire initial inclus dans le paquet ASK (pas besoin d'attendre des OAK)
        if (msg.ObjetsInitiaux.Count > 0)
        {
            perso.Inventaire.Clear();
            foreach (var o in msg.ObjetsInitiaux)
            {
                perso.Inventaire.Add(new BotDofus.Divers.Jeu.Personnage.ObjetInventaire
                {
                    Identifiant = o.Identifiant,
                    IdTemplate = o.IdTemplate,
                    Quantite = o.Quantite,
                    Position = o.Position
                });
            }
            Journaliseur.Info($"[INV] Inventaire initial chargé : {msg.ObjetsInitiaux.Count} objets");
            perso.NotifierInventaireChange();
        }

        Journaliseur.Info($"Personnage : {msg.Nom} (classe #{msg.IdClasse}, niv {msg.Niveau})");
    }

    private void OnStats(MessageStats msg)
    {
        // As : statistiques complètes du perso (PV / Énergie / PA / PM / Kamas / XP / pts caracs / pts sorts).
        var perso = _etat.Personnage;
        perso.XpActuelle = msg.XpActuelle;
        perso.XpPalierCourant = msg.XpPalier;
        perso.XpPalierSuivant = msg.XpProchainPalier;
        perso.Kamas = msg.Kamas;
        perso.PointsCaracteristiques = msg.PointsCaracteristiques;
        perso.PointsSorts = msg.PointsSorts;
        foreach (var kv in msg.Caracteristiques) perso.Caracteristiques[kv.Key] = kv.Value;
        perso.PA = msg.PA;
        perso.PM = msg.PM;
        perso.ActualiserVie(msg.Vie, msg.VieMax);
        perso.ActualiserEnergie(msg.Energie, msg.EnergieMax);
    }

    private void OnDonneesCarte(MessageDonneesCarte msg)
    {
        Journaliseur.Info($"Changement de carte : #{msg.IdentifiantCarte}");
        _etat.ChangerCarte(msg.IdentifiantCarte, msg.DateVersion, msg.ClefCarte);
    }

    private void OnPositionsCombat(MessagePositionsCombat msg)
    {
        _etat.Combat.DefinirPositionsPlacement(msg.PositionsEquipe1, msg.PositionsEquipe2, msg.EquipeCourante);
        _compte.ChangerEtat(EtatsCompte.EnCombat);
        Journaliseur.Info($"[COMBAT] Placement : equipe 1={msg.PositionsEquipe1.Count}, equipe 2={msg.PositionsEquipe2.Count}, equipe={msg.EquipeCourante}");
    }

    private void OnMouvementCarte(MessageMouvementCarte msg)
    {
        var carte = _etat.CarteCourante;
        if (carte == null)
        {
            Journaliseur.Avertir($"[ENT] GM reçu mais CarteCourante==null (entrées={msg.Entrees.Count}) — entités perdues");
            return;
        }

        // Diagnostic Abrak : GM brut + nb d'entrées parsées. Permet de voir si le
        // format match le parseur (cellule;type;...;id;nom;...).
        var brut = msg.Charge ?? string.Empty;
        Journaliseur.Info(
            $"[ENT] GM {msg.Entrees.Count} entrée(s) | brut={(brut.Length > 180 ? brut[..180] + "…" : brut)}");

        foreach (var entree in msg.Entrees)
        {
            if (entree.Operation == OperationGM.Despawn)
            {
                carte.Entites.TryRemove(entree.IdentifiantEntite, out _);
                continue;
            }

            var champs = entree.ContenuBrut.Length > 1
                ? entree.ContenuBrut[1..].Split(';')
                : Array.Empty<string>();
            if (champs.Length < 2 || !int.TryParse(champs[0], out var cellule))
            {
                continue;
            }

            var idEntite = ParserInt(champs, 3);
            var champ4 = champs.ElementAtOrDefault(4) ?? string.Empty;

            // === CLASSIFICATION PILOTÉE PAR LE PRÉFIXE GM (fiable) ===
            // '~' (OperationGM.MonstreGroupe) = TOUJOURS un groupe de monstres.
            // '+'/'=' = acteur : id>0 → joueur (pseudo) ; id<0 → PNJ (sprite
            // serveur avec couleurs + id PNJ en fin de trame). Fini l'ancienne
            // heuristique look≥3000 qui prenait des PNJ pour des monstres
            // (ex. +220;1;0;-6;857;…;6363b3;ffe926;d1cdad;…;9089 = PNJ #9089).

            // ---- Groupe de monstres ('~') ----
            if (entree.Operation == OperationGM.MonstreGroupe)
            {
                var gabarits = champ4.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (gabarits.Length == 0) continue;
                var idGabarit = ParserInt(gabarits.ElementAtOrDefault(0));
                var id = idEntite != 0 ? idEntite : -Math.Abs(cellule + 1);

                int niveauGroupe = 0;
                var noms = new System.Collections.Generic.List<string>();
                foreach (var g in gabarits)
                {
                    var gid = ParserInt(g);
                    if (gid == 0) continue;
                    var m = Divers.Donnees.BaseDonnees.Instance.Monstre(gid);
                    if (m != null) niveauGroupe += m.Niveau;
                    noms.Add(NomMonstre(gid));
                }

                carte.Entites[id] = new EntiteMonstre
                {
                    Identifiant = id,
                    CellulePosition = cellule,
                    IdGabarit = idGabarit,
                    NiveauGroupe = niveauGroupe,
                    TailleGroupe = Math.Max(1, gabarits.Length),
                    Nom = noms.Count > 0 ? string.Join(", ", noms) : NomMonstre(idGabarit)
                };
                continue;
            }

            // ---- Acteur ('+' / '=') : joueur (id>0) ou PNJ (id<0) ----
            bool champ4Numerique = champ4.Length > 0
                && champ4.All(c => char.IsDigit(c) || c == ',' || c == '-');

            if (idEntite > 0)
            {
                // Joueur (id de compte + pseudo non numérique).
                if (idEntite == _etat.Personnage.Identifiant
                    || champ4 == _etat.Personnage.Nom)
                {
                    _etat.Personnage.CellulePosition = cellule;
                    continue;
                }
                carte.Entites[idEntite] = new EntiteJoueur
                {
                    Identifiant = idEntite,
                    CellulePosition = cellule,
                    Nom = champ4Numerique || champ4.Length == 0 ? $"Joueur {idEntite}" : champ4,
                    Niveau = ParserNiveau(champs.ElementAtOrDefault(6)),
                    Sexe = ParserInt(champs, 5)
                };
                continue;
            }

            // id < 0 et préfixe '+' → PNJ. L'id PNJ (template, pour nom/dialogue)
            // est le modèle en champ[4] (ex. +220;1;0;-6;857;… → 857 = « Posteur
            // Nhin »), PAS le dernier champ (9089 = id contextuel, jamais en BDD).
            // Le format Hystoria n'étant pas garanti, on est ROBUSTE : on teste
            // champ[4] puis les autres champs numériques et on retient le PREMIER
            // qui correspond à un vrai PNJ connu de la base.
            {
                var id = idEntite != 0 ? idEntite : -Math.Abs(cellule + 1);
                var bdd = Divers.Donnees.BaseDonnees.Instance;

                // Candidats par ordre de priorité : champ[4] (modèle) d'abord,
                // puis les champs 5..fin (du début vers la fin).
                var candidats = new System.Collections.Generic.List<int>();
                void Ajouter(string? brut)
                {
                    var t = (brut ?? "").Split('^', ',')[0];
                    if (int.TryParse(t, out var v) && v > 0 && !candidats.Contains(v))
                        candidats.Add(v);
                }
                Ajouter(champ4);
                for (int k = 5; k < champs.Length; k++) Ajouter(champs[k]);

                int idPnj = candidats.FirstOrDefault(c => bdd.Npc(c) != null);
                if (idPnj == 0) idPnj = candidats.FirstOrDefault(); // rien en BDD : on garde au moins le modèle

                var npc = bdd.Npc(idPnj);
                carte.Entites[id] = new EntitePNJ
                {
                    Identifiant = id,
                    CellulePosition = cellule,
                    IdGabarit = idPnj,
                    Nom = !string.IsNullOrWhiteSpace(npc?.Nom) ? npc!.Nom : $"PNJ #{idPnj}"
                };
            }
        }

        int nbJ = carte.Entites.Values.Count(e => e is EntiteJoueur);
        int nbM = carte.Entites.Values.Count(e => e is EntiteMonstre);
        int nbP = carte.Entites.Values.Count(e => e is EntitePNJ);
        Journaliseur.Info(
            $"[ENT] carte #{carte.Identifiant} → {carte.Entites.Count} entité(s) " +
            $"(J={nbJ} M={nbM} P={nbP}), perso cell={_etat.Personnage.CellulePosition?.ToString() ?? "?"}");
    }

    /// <summary>
    /// GA0/GA1 : déplacement d'un acteur (serveur → client, déchiffré).
    /// Format : <c>GA&lt;type&gt;;&lt;?&gt;;&lt;acteurId&gt;;&lt;cheminCompressé&gt;</c>
    /// (ex. <c>GA0;1;401770;aeEhdy</c>). La cellule destination = les 2 derniers
    /// caractères du chemin (hash cellule). Met à jour la position perso/entité
    /// EN LIVE (avant ce handler, la position ne bougeait qu'au changement de map).
    /// </summary>
    private void OnActionJeu(MessageActionJeu msg)
    {
        var p = (msg.Charge ?? string.Empty).Split(';');
        // Seulement les déplacements : type 0/1, acteur numérique, chemin présent.
        if (p.Length < 4) return;
        if (p[0] != "0" && p[0] != "1") return;
        if (!int.TryParse(p[2], out var acteurId)) return;
        var chemin = p[3];
        if (string.IsNullOrEmpty(chemin) || chemin.Length < 2) return;

        // GARDE : un chemin de déplacement compressé n'utilise QUE l'alphabet
        // hash Dofus (alphanumérique + '-' '_'). Les GA0 « non déplacement »
        // (ex. '168,11900,201' = ACTION SUR INTERACTIF) contiennent des
        // virgules / caractères hors alphabet : on les ignore comme path,
        // mais on en TIRE des infos utiles (durée d'action serveur, type).
        foreach (var ch in chemin)
        {
            if (BotDofus.Utilitaires.Crypto.HashCarte.IndexCar(ch) < 0)
            {
                Journaliseur.Debogue(
                    $"[GA0] payload non-déplacement (acteur p[2]={p[2]}, " +
                    $"charge='{chemin}')");
                // Format Retro : « <cell>,<durationMs>,<actionStatus> »
                // actionStatus = 201 (interactif/récolte), 200 (combat),
                // etc. Si c'est NOUS qui sommes l'acteur ET status=201 →
                // c'est notre récolte qui démarre, on annonce la durée.
                if (acteurId == _etat.Personnage.Identifiant)
                {
                    var parts = chemin.Split(',');
                    if (parts.Length >= 3
                        && int.TryParse(parts[0], out var cellAct)
                        && int.TryParse(parts[1], out var dureeMs)
                        && parts[2] == "201" && dureeMs > 0)
                    {
                        Journaliseur.Info(
                            $"[ACTION] Récolte en cours : cellule {cellAct} "
                            + $"(durée serveur ~{dureeMs / 1000.0:F1}s)");
                    }
                }
                return;
            }
        }

        // Le chemin compressé Dofus = (dirChar + cell2chars) répété par
        // changement de direction. La cellule d'ARRIVÉE = les 2 derniers chars.
        var hashDest = chemin.Substring(chemin.Length - 2);
        int cell = BotDofus.Utilitaires.Crypto.HashCarte.DecoderCellule(hashDest);
        bool moi = acteurId == _etat.Personnage.Identifiant;
        Journaliseur.Info(
            $"[GA0] acteur #{acteurId} {(moi ? "(MOI)" : "")} chemin='{chemin}' " +
            $"dest='{hashDest}' → cell {cell} (perso.Id={_etat.Personnage.Identifiant})");
        if (cell <= 0) return;

        if (moi)
        {
            int? avant = _etat.Personnage.CellulePosition;
            _etat.Personnage.CellulePosition = cell;
            _etat.CarteCourante?.SignalerRechargee();
            // En combat, c'est la CONFIRMATION serveur du déplacement. Émet
            // l'event Combat.MouvementBotConfirme pour que PipelineDeplacementCombat
            // résolve l'attente bloquante de JouerTourCombatAsync (cf. ADR-002 §4.2).
            if (_etat.Combat.Etat != Divers.Combats.Enums.EtatCombat.Inactif)
            {
                Journaliseur.Info($"[ACTION-MV] Position confirmée par serveur : cell {avant} → {cell} (broadcast GA;{p[0]};)");
                _etat.Combat.DeclencherMouvementBot(acteurId, cell, chemin);
            }
        }
        else
        {
            var carte = _etat.CarteCourante;
            if (carte != null && carte.Entites.TryGetValue(acteurId, out var ent))
            {
                ent.CellulePosition = cell;
                carte.SignalerRechargee();
            }
        }
    }

    /// <summary>
    /// GDF : états des éléments interactifs de la carte. Deux usages :
    ///  - au chargement : liste complète <c>cell;etat;dispo|cell;etat;dispo|…</c>
    ///    (ex. <c>142;0;1|154;0;1|…</c>) — tout est dispo (3e champ=1).
    ///  - en jeu : mise à jour ponctuelle (ex. <c>211;3;0</c> après récolte
    ///    de la ressource cell 211 → 3e champ=0 = épuisée).
    /// On répercute sur <see cref="Cellule.RessourceDisponible"/> et on
    /// rafraîchit la carte : c'est ce qui faisait que « la carte ne
    /// s'actualisait pas quand je récolte à la main ».
    /// </summary>
    private void OnElementsInteractifs(MessageDonneesCarteFin msg)
    {
        var carte = _etat.CarteCourante;
        if (carte == null) { return; }

        var charge = msg.Charge ?? string.Empty;
        if (charge.Length == 0 || !charge.Contains(';'))
        {
            // GDF « vide » = simple fin de chargement → on rafraîchit.
            carte.SignalerRechargee();
            return;
        }

        int maj = 0;
        foreach (var entree in charge.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var champs = entree.Split(';');
            if (champs.Length < 2) continue;
            if (!int.TryParse(champs[0], out var idCell)) continue;

            var cellule = carte.Obtenir(idCell);
            if (cellule == null) continue;

            int.TryParse(champs[1], out var etat);
            // 3e champ = disponibilité (1 = exploitable, 0 = épuisée).
            // Absent → on se rabat sur etat==0 = dispo.
            bool dispo = champs.Length >= 3
                ? champs[2].Trim() == "1"
                : etat == 0;

            cellule.EtatInteractif = etat;
            cellule.RessourceDisponible = dispo;
            maj++;
        }

        if (maj > 0)
        {
            Journaliseur.Info($"[GDF] {maj} élément(s) interactif(s) mis à jour "
                + $"(carte #{carte.Identifiant}).");
            carte.SignalerRechargee();
        }
    }

    /// <summary>
    /// JSK : skills (récoltes/recettes) connus par métier. On agrège tous les
    /// idSkill dans <see cref="Personnage.SkillsConnus"/> : c'est ce qui permet
    /// de savoir, sur la carte, si une ressource est récoltable PAR CE perso
    /// (le gfx interactif → skill via la BDD auto-apprise ; si ce skill n'est
    /// pas connu, le serveur refuse la récolte = « je peux pas tout récolter »).
    /// </summary>
    private void OnMetiersSkills(MessageMetiersSkills msg)
    {
        var perso = _etat.Personnage;
        // IMPORTANT : les JSK partiels du serveur Hystoria peuvent écraser un
        // job avec une liste RÉDUITE (constaté log 08:48:07.234 : 2ᵉ JSK
        // efface skill 53 de job#28 Céréale → Orge plus récoltable jusqu'au
        // prochain JSK complet). Solution : on fait l'UNION par job au lieu
        // d'écraser. Un perso ne perd JAMAIS un skill connu — seul level-up
        // peut en AJOUTER. SkillsConnus reconstruit ensuite depuis l'union.
        var bddSkills = Divers.Donnees.BaseDonnees.Instance;
        foreach (var kv in msg.Metiers)
        {
            if (!perso.MetiersSkills.TryGetValue(kv.Key, out var existants))
            {
                perso.MetiersSkills[kv.Key] = new List<int>(kv.Value);
                continue;
            }
            foreach (var s in kv.Value)
            {
                if (existants.Contains(s)) continue;
                existants.Add(s);
                // Nouveau skill débloqué (level-up → palier métier).
                // Visible côté utilisateur : on saura tout de suite quelle
                // ressource devient récoltable suite au level-up.
                var nomVerbe = bddSkills.Skill(s);
                Journaliseur.Info($"[ACTION] Nouveau skill débloqué : "
                    + $"#{s} {(string.IsNullOrEmpty(nomVerbe) ? "(verbe inconnu)" : nomVerbe)} "
                    + $"— ressources avec ce skill maintenant récoltables.");
            }
        }

        perso.SkillsConnus.Clear();
        foreach (var kv in perso.MetiersSkills)
            foreach (var s in kv.Value) perso.SkillsConnus.Add(s);

        LoggerMetiers();
    }

    /// <summary>JXK : niveau par métier.</summary>
    private void OnMetiersXp(MessageMetiersXp msg)
    {
        var perso = _etat.Personnage;
        perso.MetiersNiveaux.Clear();
        foreach (var kv in msg.Niveaux) perso.MetiersNiveaux[kv.Key] = kv.Value;
        LoggerMetiers();
    }

    /// <summary>
    /// Résumé lisible des métiers : pour chaque job, niveau + famille déduite
    /// du 1er skill connu (on n'a pas de table jobId→nom, mais les NOMS de
    /// skills oui via skills_hystoria.json → Couper=Bois, Faucher=Céréale…).
    /// </summary>
    private void LoggerMetiers()
    {
        var perso = _etat.Personnage;
        if (perso.MetiersNiveaux.Count == 0 && perso.MetiersSkills.Count == 0) return;

        var bdd = Divers.Donnees.BaseDonnees.Instance;
        var ids = new SortedSet<int>(perso.MetiersNiveaux.Keys);
        foreach (var k in perso.MetiersSkills.Keys) ids.Add(k);

        var sb = new System.Text.StringBuilder("[MÉTIERS] ");
        foreach (var jobId in ids)
        {
            int niveau = perso.MetiersNiveaux.TryGetValue(jobId, out var n) ? n : 0;
            perso.MetiersSkills.TryGetValue(jobId, out var skills);
            string famille = "—";
            if (skills is { Count: > 0 })
            {
                var nomsk = bdd.Skill(skills[0]);
                famille = Divers.Donnees.BaseDonnees.FamilleRessource(nomsk);
            }
            sb.Append($"job#{jobId}={famille} niv{niveau}");
            if (skills is { Count: > 0 }) sb.Append($" (skills {string.Join(",", skills)})");
            sb.Append(" | ");
        }
        Journaliseur.Info(sb.ToString().TrimEnd(' ', '|'));
        if (perso.SkillsConnus.Count > 0)
            Journaliseur.Info($"[MÉTIERS] {perso.SkillsConnus.Count} skill(s) connus → "
                + "les ressources dont le skill n'est PAS dans cette liste ne sont "
                + "pas récoltables par ce perso (niveau/métier insuffisant).");
    }

    private void OnInfoMessage(MessageInfoMessage msg)
    {
        Journaliseur.Info($"Info #{msg.Code} : {msg.Arguments}");
    }

    private void OnObjetAjout(MessageObjetAjout msg)
    {
        var inv = _etat.Personnage.Inventaire;
        var bddItems = Divers.Donnees.BaseDonnees.Instance;
        foreach (var o in msg.ObjetsParse)
        {
            // Évite les doublons : si l'id existe déjà, on remplace quantité/position.
            var existant = inv.FirstOrDefault(x => x.Identifiant == o.Identifiant);
            if (existant != null)
            {
                int delta = o.Quantite - existant.Quantite;
                existant.Quantite = o.Quantite;
                existant.Position = o.Position;
                if (delta > 0)
                {
                    var nom = bddItems.Item(o.IdTemplate)?.Nom ?? $"Item #{o.IdTemplate}";
                    Journaliseur.Info($"[ACTION] +{delta} {nom} (total {o.Quantite})");
                }
            }
            else
            {
                inv.Add(new BotDofus.Divers.Jeu.Personnage.ObjetInventaire
                {
                    Identifiant = o.Identifiant,
                    IdTemplate = o.IdTemplate,
                    Quantite = o.Quantite,
                    Position = o.Position
                });
                if (o.Quantite > 0 && o.Position == 63)  // 63 = sac (récolte fraîche)
                {
                    var nom = bddItems.Item(o.IdTemplate)?.Nom ?? $"Item #{o.IdTemplate}";
                    Journaliseur.Info($"[ACTION] +{o.Quantite} {nom} (nouveau)");
                }
            }
        }
        Journaliseur.Debogue($"[INV] +{msg.ObjetsParse.Count} objets (total = {inv.Count})");
        _etat.Personnage.NotifierInventaireChange();
    }

    private void OnObjetRetrait(MessageObjetRetrait msg)
    {
        var inv = _etat.Personnage.Inventaire;
        var n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
        if (n > 0)
        {
            Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
            _etat.Personnage.NotifierInventaireChange();
        }
    }

    private void OnObjetQuantite(MessageObjetQuantite msg)
    {
        var existant = _etat.Personnage.Inventaire.FirstOrDefault(x => x.Identifiant == msg.IdentifiantObjet);
        if (existant != null)
        {
            int delta = msg.NouvelleQuantite - existant.Quantite;
            existant.Quantite = msg.NouvelleQuantite;
            Journaliseur.Debogue($"[INV] objet {msg.IdentifiantObjet} → qty {msg.NouvelleQuantite}");
            // [ACTION] visible dans le Chat : « +1 Frêne (total 18) »
            // Ne déclenche que si gain positif (loot, pas dépose / vente).
            if (delta > 0)
            {
                var nom = Divers.Donnees.BaseDonnees.Instance.Item(existant.IdTemplate)?.Nom
                          ?? $"Item #{existant.IdTemplate}";
                Journaliseur.Info($"[ACTION] +{delta} {nom} (total {msg.NouvelleQuantite})");
                // Signale aux boucles de récolte : un loot vient d'arriver
                // pour CE perso → la récolte courante a réussi (cf. cas
                // map partagée avec un autre joueur).
                _etat.Personnage.NbLootsRecus++;
            }
            _etat.Personnage.NotifierInventaireChange();
        }
    }

    private void OnInfoVie(MessageInfoVie msg)
    {
        // Sécurité : on n'écrase pas la vie réelle (déjà fournie par le paquet As) avec 0/0
        // si jamais le paquet IL arrive sous une variante non-life-info (ex: "ILS2000").
        if (msg.VieMax <= 0) return;
        _etat.Personnage.ActualiserVie(msg.Vie, msg.VieMax);
    }

    private void OnChatMessage(MessageChatMessage msg)
    {
        Journaliseur.Info($"[{msg.Canal}] {msg.PseudoEmetteur} : {msg.Texte}");
    }

    private static bool EstEntreeJoueur(int type, string[] champs)
    {
        if (type == 3 || type == 4) return true;
        return champs.Length > 5
               && int.TryParse(champs.ElementAtOrDefault(3), out var id)
               && id > 0
               && !string.IsNullOrWhiteSpace(champs.ElementAtOrDefault(4));
    }

    private static int ParserInt(string? valeur)
        => int.TryParse(valeur, out var v) ? v : 0;

    private static int ParserInt(string[] champs, int index)
        => ParserInt(champs.ElementAtOrDefault(index));

    private static int ParserNiveau(string? valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur)) return 0;
        var brut = valeur.Split('^', ',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return ParserInt(brut);
    }

    private static string NomMonstre(int idGabarit)
    {
        var nom = Divers.Donnees.BaseDonnees.Instance.Monstre(idGabarit)?.Nom;
        return string.IsNullOrWhiteSpace(nom) ? $"Monstre #{idGabarit}" : nom;
    }

    /// <summary>Quand un combat démarre, on copie les entités de la carte vers Combat.Allies/Ennemis
    /// pour que la Vue Combat puisse afficher les combattants en live.</summary>
    private void PeuplerCombatDepuisCarte()
    {
        var carte = _etat.CarteCourante;
        if (carte == null) return;

        _etat.Combat.Allies.Clear();
        _etat.Combat.Ennemis.Clear();

        foreach (var ent in carte.Entites.Values)
        {
            if (ent is BotDofus.Divers.Cartes.Entites.EntiteJoueur joueur)
            {
                _etat.Combat.Allies.Add(new BotDofus.Divers.Combats.Combattants.CombattantAllie
                {
                    Identifiant = joueur.Identifiant,
                    Nom = joueur.Nom,
                    CellulePosition = joueur.CellulePosition,
                    Niveau = joueur.Niveau,
                    Equipe = 0
                });
            }
            else if (ent is BotDofus.Divers.Cartes.Entites.EntiteMonstre mob)
            {
                _etat.Combat.Ennemis.Add(new BotDofus.Divers.Combats.Combattants.CombattantMonstre
                {
                    Identifiant = mob.Identifiant,
                    Nom = NomMonstre(mob.IdGabarit),
                    CellulePosition = mob.CellulePosition,
                    IdGabarit = mob.IdGabarit,
                    NiveauGabarit = mob.NiveauGroupe,
                    Equipe = 1
                });
            }
        }

        // S'assure que le perso est bien dans les alliés (s'il n'a pas été capturé via GM).
        if (!_etat.Combat.Allies.Any(a => a.Nom == _etat.Personnage.Nom)
            && !string.IsNullOrEmpty(_etat.Personnage.Nom))
        {
            _etat.Combat.Allies.Add(new BotDofus.Divers.Combats.Combattants.CombattantAllie
            {
                Identifiant = _etat.Personnage.Identifiant,
                Nom = _etat.Personnage.Nom,
                CellulePosition = _etat.Personnage.CellulePosition ?? 0,
                Niveau = _etat.Personnage.Niveau,
                IdClasse = _etat.Personnage.IdClasse,
                PV = _etat.Personnage.Vie,
                PVMax = _etat.Personnage.VieMax,
                PA = _etat.Personnage.PA,
                PM = _etat.Personnage.PM,
                Equipe = 0
            });
        }

        Journaliseur.Info($"[COMBAT] {_etat.Combat.Allies.Count} allié(s) vs {_etat.Combat.Ennemis.Count} ennemi(s)");
    }

    /// <summary>
    /// IA combat basique (capture user 12:41:11) : cast le sort offensif
    /// le plus puissant connu sur l'ennemi le plus proche, puis pass turn.
    /// Aligné sur la séquence manuelle du joueur (GA300sort;cell + Gt).
    /// Fallback : si aucun sort utilisable ou aucun ennemi → Gt direct.
    /// Cas concret : protecteur de ressource à partir du niv 20 métier.
    /// </summary>
    private async Task JouerTourCombatAsync()
    {
        var perso = _etat.Personnage;
        var combat = _etat.Combat;
        int maCell = perso.CellulePosition ?? 0;

        // Délai de réaction humanisé. Capture user passif 16:21-16:22 montre :
        //   - Tour 6 : GTS 16:21:58.058 → GA300 16:21:59.754 = 1696 ms
        //   - Tour 7 : GTS 16:22:02.921 → GA300 16:22:04.494 = 1573 ms
        // Vrais humains : 1.5-1.7 s entre voir le tour et cliquer un sort
        // (déplacer souris vers la barre, viser la cible, double-clic). Notre
        // ancien Random(600, 1200) était encore trop rapide. Random(1400, 2100)
        // colle au timing réel sans être suspect.
        int delaiReaction = System.Random.Shared.Next(1400, 2100);
        await Task.Delay(delaiReaction).ConfigureAwait(false);

        // Log diag : voir EXACTEMENT ce que le bot perçoit du combat.
        Journaliseur.Info($"[COMBAT] >>> Mon tour : cell {maCell}, PA={perso.PA}, PM={perso.PM}, "
            + $"alliés={combat.Allies.Count}, ennemis={combat.Ennemis.Count} "
            + $"(vivants : {combat.Ennemis.Count(e => !e.EstMort)})");

        // 1) Ennemi le plus proche (distance iso Dofus = Chebyshev sur (x,y)).
        // Filtre robuste : on jette les cibles « fantômes » (PV<=0 OU PVMax<=0
        // = non encore initialisées par un GTM complet). Sans ce filtre, le bot
        // peut cibler un Monstre #0 PV=0/0 (cas reproduit log 18:27:09) → cast
        // sur cadavre → GAF échec côté serveur → tour perdu.
        var ennemisVivants = combat.Ennemis
            .Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0)
            .ToList();
        if (ennemisVivants.Count == 0)
        {
            Journaliseur.Info("[ACTION] Aucun ennemi vivant → Gt");
            await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
            return;
        }

        // === Phase 1 moteur règles SynFus/dyshay ===
        // Si l'user a configuré des règles dans peleas/<perso>.json, on les
        // évalue dans l'ordre de priorité décroissante. Si une règle passe
        // (sort appris + PA OK + cible valide + portée OK), on la cast direct.
        // Sinon, fallback sur le comportement legacy (ennemi le plus proche
        // + sort de plus haut niveau + déplacement si hors portée).
        var cfg = _compte.ConfigCombat;
        if (cfg != null && cfg.Regles.Count > 0 && _etat.CarteCourante != null)
        {
            var resultat = Divers.Combats.IA.MoteurReglesCombat.Evaluer(combat, cfg, perso.SortsAppris, _etat.CarteCourante);
            if (resultat != null)
            {
                await ExecuterRegleAsync(resultat).ConfigureAwait(false);
                return;
            }
            Journaliseur.Info($"[COMBAT] Aucune règle SynFus en portée ({cfg.Regles.Count} règle(s) configurée(s)) → fallback legacy");
        }

        var ennemi = ennemisVivants
            .OrderBy(e => DistanceDofus(maCell, e.CellulePosition))
            .First();
        int distEnnemi = DistanceDofus(maCell, ennemi.CellulePosition);
        Journaliseur.Info($"[COMBAT] Cible : « {ennemi.Nom} » #{ennemi.Identifiant} "
            + $"cell {ennemi.CellulePosition} (PV={ennemi.PV}/{ennemi.PVMax}, dist={distEnnemi})");

        // 2) Sort offensif de plus haut niveau parmi ceux appris.
        var idsAppris = perso.SortsAppris.Keys;
        var offensifs = Divers.Jeu.Personnage.Spells.BaseSorts.Instance.SortsOffensifs(idsAppris).ToList();
        Journaliseur.Info($"[COMBAT] Sorts offensifs disponibles : {offensifs.Count} "
            + $"(sur {idsAppris.Count} sorts appris)");

        // Trier par niveau perso desc puis CoutPA asc (préfère sorts boostés).
        offensifs.Sort((a, b) =>
        {
            int nivA = perso.SortsAppris.TryGetValue(a.Identifiant, out var na) ? na : 0;
            int nivB = perso.SortsAppris.TryGetValue(b.Identifiant, out var nb) ? nb : 0;
            if (nivA != nivB) return nivB.CompareTo(nivA);
            return a.CoutPA.CompareTo(b.CoutPA);
        });

        // Cherche d'abord un sort lançable IMMÉDIATEMENT (sans bouger).
        // Les stats du sort viennent du XML dyshay (StatsParNiveau[niv]) qui
        // donne les vraies valeurs au niveau APPRIS du perso. Fallback sur les
        // champs legacy (= niv 1) si XML pas chargé.
        Divers.Jeu.Personnage.Spells.InfoSort? sort = null;
        int sortCoutPA = 0; int sortPorteeMin = 0; int sortPorteeMax = 0;
        foreach (var s in offensifs)
        {
            int niv = perso.SortsAppris.TryGetValue(s.Identifiant, out var n) ? n : 0;
            var statsNiv = s.Stats(niv);
            int coutPA = statsNiv?.CoutPA ?? s.CoutPA;
            int porteeMin = statsNiv?.PorteeMin ?? s.PorteeMin;
            int porteeMax = statsNiv?.PorteeMax ?? s.PorteeMax;
            if (coutPA > 0 && perso.PA > 0 && coutPA > perso.PA)
            {
                Journaliseur.Debogue($"[COMBAT] rejet « {s.Nom} » (#{s.Identifiant} niv{niv}) : PA {coutPA} > {perso.PA}");
                continue;
            }
            if (porteeMax > 0 && distEnnemi > porteeMax)
            {
                Journaliseur.Debogue($"[COMBAT] rejet « {s.Nom} » (#{s.Identifiant} niv{niv}) : portée {distEnnemi} > max {porteeMax}");
                continue;
            }
            if (distEnnemi < porteeMin)
            {
                Journaliseur.Debogue($"[COMBAT] rejet « {s.Nom} » (#{s.Identifiant} niv{niv}) : portée {distEnnemi} < min {porteeMin}");
                continue;
            }
            sort = s;
            sortCoutPA = coutPA;
            sortPorteeMin = porteeMin;
            sortPorteeMax = porteeMax;
            break;
        }

        // 2bis) DÉPLACEMENT si aucun sort en portée. On essaie de se rapprocher
        // jusqu'à ce qu'un sort soit utilisable, en respectant les PM dispos.
        if (sort == null && perso.PM > 0)
        {
            // On part du meilleur sort (PA OK au niveau appris) sans contrainte
            // de portée. Stats par niveau via XML dyshay.
            Divers.Jeu.Personnage.Spells.InfoSort? sortVise = null;
            foreach (var s in offensifs)
            {
                int nivV = perso.SortsAppris.TryGetValue(s.Identifiant, out var nVv) ? nVv : 0;
                var stV = s.Stats(nivV);
                int pa = stV?.CoutPA ?? s.CoutPA;
                int rmaxV = stV?.PorteeMax ?? s.PorteeMax;
                if (pa > 0 && perso.PA > 0 && pa > perso.PA) continue;
                if (rmaxV <= 0) continue;
                sortVise = s;
                break;
            }
            if (sortVise != null)
            {
                // Stats par niveau XML dyshay pour le log (sinon affichait
                // valeurs legacy niv 1 = trompeur, ex « portée 1-6 » alors
                // que Ronce niv 5 est 1-8).
                int nivVise = perso.SortsAppris.TryGetValue(sortVise.Identifiant, out var nVL) ? nVL : 0;
                var stVL = sortVise.Stats(nivVise);
                int paVL = stVL?.CoutPA ?? sortVise.CoutPA;
                int pminVL = stVL?.PorteeMin ?? sortVise.PorteeMin;
                int pmaxVL = stVL?.PorteeMax ?? sortVise.PorteeMax;

                var resApproche = TrouverApprocheCombat(perso, combat, ennemi, sortVise, _compte.ConfigCombat);
                if (resApproche.HasValue)
                {
                    var (chemin, distApres) = resApproche.Value;
                    int nbPasMove = chemin.Count - 1;
                    int cellArrivee = chemin[^1].Identifiant;
                    var cellsTrace = string.Join("→", chemin.Select(c => c.Identifiant.ToString()));
                    var paquetDep = BotDofus.Divers.Cartes.Deplacement.Pathfinder.PaquetDeplacement(chemin);
                    var modeStr = _compte.ConfigCombat?.Mode.ToString() ?? "Equilibre";
                    Journaliseur.Info($"[PATHFINDING] Mode={modeStr} | départ cell {maCell} → arrivée cell {cellArrivee} | {nbPasMove} pas | dist après={distApres} | sort « {sortVise.Nom} » niv{nivVise} portée {pminVL}-{pmaxVL} | chemin: {cellsTrace}");
                    Journaliseur.Info($"[ACTION-MV] Envoi GA001 → '{paquetDep}' (cells {chemin[0].Identifiant}→{cellArrivee})");
                    await _session.EnvoyerAuServeurAsync(paquetDep).ConfigureAwait(false);

                    // Attente animation déplacement : ~330 ms par case + 200 ms
                    // buffer (réf. wukzu cadernis #1585 : `wait(distance * 330)`).
                    int dureeDeplacement = nbPasMove * 330 + 200;
                    await Task.Delay(dureeDeplacement).ConfigureAwait(false);

                    // OPTIMISTIC UPDATE : on suppose que le serveur a accepté le
                    // GA001 et déplace le perso vers la cell d'arrivée. Si le
                    // serveur a rejeté en silence (≠proxy(décalé) sur cipher),
                    // le GTM du tour suivant corrigera la position. Mais en
                    // attendant, le cast utilise la NOUVELLE position pour
                    // calculer la portée correctement (fix log 20:36-20:37 où le
                    // bot croyait toujours être à 326/193 entre les tours).
                    int cellAvantMv = maCell;
                    _etat.Personnage.CellulePosition = cellArrivee;
                    maCell = cellArrivee;
                    Journaliseur.Info($"[ACTION-MV] Position optimiste mise à jour : cell {cellAvantMv} → {cellArrivee} (en attente confirmation serveur GA;1;)");

                    // GKK0 = ack action déplacement (capture vrai client : ~150 ms
                    // après l'arrivée). Sans, le serveur attend toujours et bloque
                    // la suite du tour.
                    Journaliseur.Info($"[ACTION-MV] Envoi GKK0 (ack déplacement)");
                    await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
                    await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);

                    // Le sort est maintenant en portée → on l'utilise.
                    sort = sortVise;
                    sortCoutPA = paVL;
                    sortPorteeMin = pminVL;
                    sortPorteeMax = pmaxVL;
                    distEnnemi = distApres;
                }
                else
                {
                    Journaliseur.Info($"[PATHFINDING] AUCUNE cellule cible trouvée pour atteindre l'ennemi (cell {ennemi.CellulePosition}) en {perso.PM} PM avec sort « {sortVise.Nom} » portée {pminVL}-{pmaxVL}. Mode={_compte.ConfigCombat?.Mode}. Possible : ennemi inaccessible / PM insuffisants / toutes cells candidates bloquées par combattants.");
                }
            }
        }

        if (sort == null)
        {
            Journaliseur.Info($"[ACTION] Aucun sort utilisable (dist={distEnnemi}, PA={perso.PA}, PM={perso.PM}) → Gt");
            await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
            return;
        }

        // 3) Cast — format GA300<idSort>;<celluleCible>.
        // Séquence exacte du vrai client Dofus (capture user passif 16:21:59) :
        //   GA300<id>;<cell>  → 300-400 ms  → GKK0  → 1-1.5 s  → Gt
        // Le GKK0 = ack d'action (confirmé wukzu cadernis #1585 : GKK0 utilisé
        // après chaque action overworld ET combat pour débloquer la séquence).
        // Sans le GKK0, notre IA précédente envoyait GA300 → Gt en 800 ms et
        // le serveur kickait au tour 1 (log 16:05:06).
        var paquetSort = $"GA300{sort.Identifiant};{ennemi.CellulePosition}";
        int nivChoisi = perso.SortsAppris.TryGetValue(sort.Identifiant, out var nv) ? nv : 0;
        Journaliseur.Info($"[ACTION] Sort « {sort.Nom} » (#{sort.Identifiant} niv{nivChoisi}) "
            + $"sur cell {ennemi.CellulePosition} (cible « {ennemi.Nom} », {sortCoutPA} PA, portée {sortPorteeMin}-{sortPorteeMax})");
        await _session.EnvoyerAuServeurAsync(paquetSort).ConfigureAwait(false);

        // GKK0 : capture user 16:22:00.130 → 376 ms après GA300. Random 300-500.
        await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);
        await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);

        // Gt : capture user montre que le serveur termine le tour ~1.5 s après
        // GKK0 quand le client a vidé ses PA. Pour rester sûr, on envoie Gt
        // explicite après 1-1.5 s (humanisé). Si le serveur a déjà fermé le
        // tour (GTF reçu), Gt est inoffensif (le serveur l'ignore).
        await Task.Delay(System.Random.Shared.Next(1000, 1500)).ConfigureAwait(false);
        Journaliseur.Info("[ACTION] Passe le tour (Gt)");
        await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
    }

    /// <summary>
    /// Distance « cases Dofus » entre 2 cell-id (grille iso). Utilise
    /// <see cref="BotDofus.Divers.Cartes.Cellule.CalculerCoordonnees"/>
    /// (formule dyshay) + Chebyshev — c'est cette métrique que Dofus utilise
    /// pour la portée des sorts (cases adjacentes en diagonale = distance 1).
    /// </summary>
    /// <remarks>
    /// ⚠ Bug majeur fixé le 20/05/2026 : on hardcodait <c>mapWidth=14</c> alors
    /// que les cartes Hystoria sont 15×17 (<see cref="BotDofus.Divers.Cartes.Carte.LargeurParDefaut"/>=15).
    /// Les <see cref="BotDofus.Divers.Cartes.Cellule.X"/>/<c>Y</c> de la carte
    /// étaient en mw=15 (construits par <see cref="BotDofus.Divers.Cartes.Carte"/>)
    /// mais les recalculs ad-hoc en mw=14 produisaient des coords incohérentes
    /// → distance fausse, bot mal positionné (log 19:47-19:53 du 20/05). Désormais
    /// on lit la largeur de la carte courante.
    /// </remarks>
    private int DistanceDofus(int idA, int idB)
    {
        int mw = _etat.CarteCourante?.Largeur ?? BotDofus.Divers.Cartes.Carte.LargeurParDefaut;
        var (xA, yA) = BotDofus.Divers.Cartes.Cellule.CalculerCoordonnees(idA, mw);
        var (xB, yB) = BotDofus.Divers.Cartes.Cellule.CalculerCoordonnees(idB, mw);
        return System.Math.Max(System.Math.Abs(xA - xB), System.Math.Abs(yA - yB));
    }

    /// <summary>
    /// Trouve un chemin combat qui rapproche le perso jusqu'à mettre l'ennemi
    /// en portée du sort visé, en respectant les PM dispos et en évitant les
    /// combattants (alliés + ennemis vivants).
    ///
    /// Stratégie : on enumère les cellules dont la distance Chebyshev à
    /// l'ennemi est dans [PorteeMin, PorteeMax], puis on garde la plus PROCHE
    /// de notre position (= chemin Pathfinder le plus court &lt;= PM).
    ///
    /// Renvoie (chemin, distApresMove) ou null si rien d'atteignable.
    /// </summary>
    /// <remarks>
    /// Refondue 20/05/2026 pour respecter <c>cfg.Mode</c> (cf. ADR-001 §3) :
    /// la cellule cible n'est plus juste « la plus proche en pas » mais celle qui
    /// minimise le score selon le mode (Agressif → dist=1 / Eloigne → dist=portéeMax /
    /// Equilibre → |dist - DistancePref| / Fuyard → idem Eloigne en cast). Tie-break
    /// sur nbPas A* (économise PM pour casts suivants).
    /// </remarks>
    private (System.Collections.Generic.List<BotDofus.Divers.Cartes.Cellule> chemin, int distFinale)?
        TrouverApprocheCombat(
            BotDofus.Divers.Jeu.Personnage.Personnage perso,
            BotDofus.Divers.Combats.Combat combat,
            BotDofus.Divers.Combats.Combattants.Combattant ennemi,
            BotDofus.Divers.Jeu.Personnage.Spells.InfoSort sort,
            BotDofus.Divers.Combats.IA.ConfigCombat? cfg)
    {
        var carte = _etat.CarteCourante;
        if (carte == null || perso.CellulePosition is not int maCellId) return null;
        var depart = carte.Obtenir(maCellId);
        if (depart == null) return null;

        var mode = cfg?.Mode ?? BotDofus.Divers.Combats.IA.ModeCombat.Equilibre;
        int distPref = cfg?.DistancePreferee ?? 5;
        int pmMax = perso.PM > 0 ? perso.PM : 3;
        // Stats du sort au NIVEAU appris (XML dyshay). Ronce niv 5 = 1-8 / PA 4.
        int niveauSort = perso.SortsAppris.TryGetValue(sort.Identifiant, out var nivS) ? nivS : 0;
        var statsApp = sort.Stats(niveauSort);
        int porteeMin = statsApp?.PorteeMin ?? sort.PorteeMin;
        int porteeMax = statsApp?.PorteeMax ?? (sort.PorteeMax > 0 ? sort.PorteeMax : 6);

        // Cellules occupées par les combattants (sauf moi) = obstacles.
        var interdites = new System.Collections.Generic.HashSet<BotDofus.Divers.Cartes.Cellule>();
        foreach (var a in combat.Allies)
        {
            if (a.Identifiant == perso.Identifiant) continue;
            var c = carte.Obtenir(a.CellulePosition);
            if (c != null) interdites.Add(c);
        }
        foreach (var e in combat.Ennemis)
        {
            if (e.EstMort) continue;
            var c = carte.Obtenir(e.CellulePosition);
            if (c != null) interdites.Add(c);
        }

        // Coordonnées (x,y) de l'ennemi (référence portée). On utilise la largeur
        // RÉELLE de la carte courante (15 sur Hystoria, pas 14) — sinon mismatch
        // avec c.X/c.Y qui sont stockés par Carte avec sa Largeur propre.
        int mw = carte.Largeur > 0 ? carte.Largeur : BotDofus.Divers.Cartes.Carte.LargeurParDefaut;
        var (xE, yE) = BotDofus.Divers.Cartes.Cellule.CalculerCoordonnees(ennemi.CellulePosition, mw);

        // Énumère les candidates : cells marchables, non interactif, non
        // occupées par un combattant, dist Chebyshev à l'ennemi dans [min, max].
        // Sélection guidée par le Mode (cf. ScoreCelluleMode) + tie-break nbPas.
        BotDofus.Divers.Cartes.Cellule? meilleureCible = null;
        System.Collections.Generic.List<BotDofus.Divers.Cartes.Cellule>? meilleurChemin = null;
        double meilleurScore = double.MaxValue;
        int meilleurNbPasTie = int.MaxValue;
        int meilleureDist = -1;
        foreach (var c in carte.Cellules)
        {
            if (c == null) continue;
            if (!c.EstMarchable) continue;
            if (c.IdInteractif >= 0) continue;
            if (interdites.Contains(c)) continue;
            int d = System.Math.Max(System.Math.Abs(c.X - xE), System.Math.Abs(c.Y - yE));
            if (d < porteeMin || d > porteeMax) continue;
            // Estimation Chebyshev de notre déplacement (borne basse) — on
            // jette les candidats hors de portée PM AVANT l'A* coûteux.
            int dEstimee = System.Math.Max(
                System.Math.Abs(c.X - depart.X), System.Math.Abs(c.Y - depart.Y));
            if (dEstimee > pmMax) continue;

            var chemin = BotDofus.Divers.Cartes.Deplacement.Pathfinder.Trouver(
                carte, depart, c, interdites);
            if (chemin == null) continue;
            int nbPas = chemin.Count - 1;
            if (nbPas == 0) continue; // déjà à cette case (sort aurait dû passer plus tôt)
            if (nbPas > pmMax) continue;

            double score = ScoreCelluleMode(d, mode, porteeMax, distPref);
            // Sélection : score min, tie-break nbPas A* min (économise PM).
            if (score < meilleurScore || (score == meilleurScore && nbPas < meilleurNbPasTie))
            {
                meilleurScore = score;
                meilleurNbPasTie = nbPas;
                meilleureCible = c;
                meilleurChemin = chemin;
                meilleureDist = d;
            }
        }

        if (meilleurChemin == null) return null;
        return (meilleurChemin, meilleureDist);
    }

    /// <summary>
    /// Score d'une cellule cible candidate selon le <see cref="BotDofus.Divers.Combats.IA.ModeCombat"/>
    /// (plus bas = meilleur). Algorithme aligné dyshay <c>FightExtensions.get_Mover</c>
    /// et ADR-001 §3.
    /// </summary>
    private static double ScoreCelluleMode(int distEnnemi, BotDofus.Divers.Combats.IA.ModeCombat mode,
                                            int porteeMax, int distancePreferee)
    {
        return mode switch
        {
            // Agressif : dist=1 (CAC) parfait → score 0. Sinon pénalité = dist-1.
            BotDofus.Divers.Combats.IA.ModeCombat.Agressif => System.Math.Max(0, distEnnemi - 1),
            // Eloigne : dist=porteeMax parfait → score 0. Pénalité = porteeMax-dist.
            BotDofus.Divers.Combats.IA.ModeCombat.Eloigne => System.Math.Max(0, porteeMax - distEnnemi),
            // Fuyard en cast (sans fuite active) : comportement Eloigne. La fuite
            // active (PV%<seuil) est gérée en amont dans JouerTourCombatAsync.
            BotDofus.Divers.Combats.IA.ModeCombat.Fuyard => System.Math.Max(0, porteeMax - distEnnemi),
            // Equilibre : on s'approche de la DistancePreferee (clamp dans la portée).
            BotDofus.Divers.Combats.IA.ModeCombat.Equilibre => System.Math.Abs(distEnnemi - distancePreferee),
            _ => 0
        };
    }

    /// <summary>
    /// Exécute une règle SynFus déjà validée par <see cref="Divers.Combats.IA.MoteurReglesCombat"/>
    /// (sort appris, PA OK, portée OK, cible valide). Envoie la séquence
    /// <c>GA300&lt;id&gt;;&lt;cell&gt;</c> → GKK0 → Gt avec les timings humanisés
    /// capturés du vrai client (cf. capture user passif 16:21-16:22).
    /// </summary>
    /// <remarks>
    /// Phase 1 du moteur règles : pas encore de déplacement intégré (le moteur
    /// rejette les règles hors portée → fallback legacy). Le multi-cast par
    /// tour viendra en Phase 2 (drain PA, respecte NombreParTour).
    /// </remarks>
    private async Task ExecuterRegleAsync(Divers.Combats.IA.MoteurReglesCombat.ResultatRegle r)
    {
        Journaliseur.Info($"[COMBAT] Règle SynFus appliquée : « {r.Sort.Nom} » "
            + $"(#{r.Sort.Identifiant} niv{r.NiveauAppris}) focus={r.Regle.Focus}, "
            + $"cible « {r.Cible.Nom} » cell {r.Cible.CellulePosition} "
            + $"(dist={r.Distance}, {r.CoutPA} PA, portée {r.PorteeMin}-{r.PorteeMax})");

        var paquet = $"GA300{r.Sort.Identifiant};{r.Cible.CellulePosition}";
        Journaliseur.Info($"[ACTION] Sort « {r.Sort.Nom} » niv{r.NiveauAppris} "
            + $"sur cell {r.Cible.CellulePosition} (cible « {r.Cible.Nom} », "
            + $"{r.CoutPA} PA, portée {r.PorteeMin}-{r.PorteeMax})");
        await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);

        // Incrémente le compteur NombreParTour de la règle (clé = idSort). Lu par
        // MoteurReglesCombat au prochain appel pour empêcher de relancer ce sort
        // au-delà de RegleSort.NombreParTour pendant le même tour (limite SynFus).
        var combat = _etat.Combat;
        combat.CompteursRegleParTour[r.Sort.Identifiant] =
            (combat.CompteursRegleParTour.TryGetValue(r.Sort.Identifiant, out var cnt) ? cnt : 0) + 1;

        // Séquence capture user 16:22 : GA300 → 376 ms → GKK0 → ~1.3 s → Gt
        await Task.Delay(System.Random.Shared.Next(300, 500)).ConfigureAwait(false);
        await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
        await Task.Delay(System.Random.Shared.Next(1000, 1500)).ConfigureAwait(false);
        Journaliseur.Info("[ACTION] Passe le tour (Gt)");
        await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
    }
}
