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
        Ecouter<MessageTourCombat>(msg =>
        {
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);
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
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageTourCombatAbrak>(msg =>
        {
            if (!msg.EstTour) return; // GTSX (sorts) — pas un tour
            _etat.Combat.IdentifiantAllie = _etat.Personnage.Identifiant;
            _etat.Combat.PassageEnCombat();
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);
            Journaliseur.Info($"[COMBAT] Tour de #{msg.IdentifiantCombattant} (tour {msg.NumeroTour})"
                + (msg.IdentifiantCombattant == _etat.Personnage.Identifiant ? " ← MOI" : ""));
        });
    }

    private void OnCombattantsAbrak(BotDofus.Commun.Messages.VersClient.Jeu.MessageCombattantsAbrak msg)
    {
        if (msg.Combattants.Count == 0) return;

        _etat.Combat.IdentifiantAllie = _etat.Personnage.Identifiant;

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

            // id < 0 et préfixe '+' → PNJ. L'id PNJ (pour nom/dialogue) est
            // le DERNIER champ numérique de la trame (ex. …;;9089), pas le
            // gfx champ[4]. Fallback : gfx.
            {
                var id = idEntite != 0 ? idEntite : -Math.Abs(cellule + 1);
                int idPnj = 0;
                for (int k = champs.Length - 1; k >= 5 && idPnj == 0; k--)
                {
                    var brutK = (champs[k] ?? "").Split('^', ',')[0];
                    if (int.TryParse(brutK, out var vK) && vK > 0) idPnj = vK;
                }
                if (idPnj == 0) idPnj = ParserInt(champ4.Split(',', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(0));

                var npc = Divers.Donnees.BaseDonnees.Instance.Npc(idPnj);
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
        // (résultat de récolte ex. '168,11900,201', animations, etc.) contiennent
        // des virgules / caractères hors alphabet : on les ignore ici, sinon on
        // décodait '01' → cellule bidon 3381 qui CORROMPAIT la position perso et
        // rendait la carte interactive inutilisable jusqu'à un clic dans Dofus.exe.
        foreach (var ch in chemin)
        {
            if (BotDofus.Utilitaires.Crypto.HashCarte.IndexCar(ch) < 0)
            {
                Journaliseur.Info(
                    $"[GA0] payload non-déplacement ignoré (acteur p[2]={p[2]}, " +
                    $"charge='{chemin}')");
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
            _etat.Personnage.CellulePosition = cell;
            _etat.CarteCourante?.SignalerRechargee();
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
        perso.MetiersSkills.Clear();
        perso.SkillsConnus.Clear();
        foreach (var kv in msg.Metiers)
        {
            perso.MetiersSkills[kv.Key] = kv.Value;
            foreach (var s in kv.Value) perso.SkillsConnus.Add(s);
        }
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
        foreach (var o in msg.ObjetsParse)
        {
            // Évite les doublons : si l'id existe déjà, on remplace quantité/position.
            var existant = inv.FirstOrDefault(x => x.Identifiant == o.Identifiant);
            if (existant != null)
            {
                existant.Quantite = o.Quantite;
                existant.Position = o.Position;
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
            existant.Quantite = msg.NouvelleQuantite;
            Journaliseur.Debogue($"[INV] objet {msg.IdentifiantObjet} → qty {msg.NouvelleQuantite}");
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
}
