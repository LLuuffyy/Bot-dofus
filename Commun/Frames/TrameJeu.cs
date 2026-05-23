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

    /// <summary>Détecteur mode héros (Abrak) — observe les GTSX pour enrôler les héros liés.</summary>
    private readonly BotDofus.Divers.MultiAccount.DetecteurModeHeros _detecteurHeros;

    // Cache identité acteurs (id → nom/niveau) — alimenté par NLK et consulté
    // au moment de l'enrôlement mode héros (les NLK arrivent à la connexion,
    // bien avant le 1er GTSX du 1er combat → on peut donc enrichir tout de suite).
    private readonly System.Collections.Generic.Dictionary<int, (string Nom, int Niveau)> _acteursVus = new();
    private int _dernierNbVivants = -1;
    private int _dernierNbCombattants = -1;

    public TrameJeu(Repartiteur repartiteur, Compte compte, EtatJeu etat, SessionProxy session)
        : base(repartiteur)
    {
        _compte = compte;
        _etat = etat;
        _session = session;
        _detecteurHeros = new BotDofus.Divers.MultiAccount.DetecteurModeHeros(_compte, _etat);
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
            // BATCH (anti-crash race UI thread) — une seule notification
            // SortsChanges après tout le SL, au lieu de N events qui faisaient
            // crasher VuePersonnage.Rafraichir L 84 (Dictionary modifié en
            // pleine itération `.OrderBy().ToList()` → ArgumentException copy_to).
            _etat.Personnage.AjouterPlusieursSortsAppris(msg.Sorts);
            Journaliseur.Info($"[SORTS] {msg.Sorts.Count} sort(s) scanné(s) : "
                + string.Join(", ", msg.Sorts.Select(s => $"#{s.Key} niv{s.Value}")));
        });
        Ecouter<MessageObjetAjout>(OnObjetAjout);
        Ecouter<MessageObjetRetrait>(OnObjetRetrait);
        Ecouter<BotDofus.Commun.Messages.VersClient.Objet.MessageEchangeFin>(_ =>
        {
            // EV reçu : fenêtre échange/banque fermée. Le PiloteBanque en cours
            // détecte ce flag et arrête ses dépôts pour éviter de spammer le
            // serveur après fermeture user (cas observé log 17:10:38).
            BotDofus.Divers.Banque.PiloteBanque.BanqueFermeeObservee = true;
            BotDofus.Utilitaires.Journaux.Journaliseur.Debogue("[BANQUE] EV reçu → flag fermée");
        });
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
            // Force refresh des vues inventaire/perso : le loot reçu pendant
            // le combat (OQ/OAK) a déjà mis à jour Personnage.Inventaire, mais
            // l'UI peut être en retard (ex: pods/equip). Ré-émettre garantit
            // un snapshot propre à la sortie de combat.
            _etat.Personnage.NotifierInventaireChange();
            // Reset l'ordre des tours du groupe héros (le combat suivant aura
            // un nouvel ordre — la compo elle-même reste, on ne dissout qu'à
            // la déconnexion).
            _compte.GroupeHeros?.ReinitialiserOrdreTours();
            // Reset flag turbo : les délais doivent redevenir humanisés en
            // overworld (récolte, zaap, déplacement) — sinon signature anti-bot.
            Divers.Combats.IA.TimingsCombat.Reset();
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
            {
                bool nouveau = !_acteursVus.ContainsKey(a.Id);
                _acteursVus[a.Id] = (a.Nom, a.Niveau);
                if (nouveau)
                    Journaliseur.Info($"[ENT] acteur Abrak vu : « {a.Nom} » niv {a.Niveau} (#{a.Id})");
            }
            // Enrichit les MembreHeros du groupe avec nom/niveau si déjà connus
            // (cas typique : un nouveau lié arrive et son NLK a déjà été reçu).
            EnrichirMembresHerosDepuisCache();
            foreach (var id in msg.Despawns)
                Journaliseur.Debogue($"[ENT] acteur Abrak parti : #{id}");
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageActeurAbrakRetrait>(msg =>
            Journaliseur.Debogue($"[ENT] acteur Abrak parti : #{msg.Identifiant}"));

        // Party / mode héros — détection HORS combat via PM.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessagePartyMembres>(msg =>
        {
            _detecteurHeros.OnPartyMembres(msg);
            EnrichirMembresHerosDepuisCache();
            // Procédure de capture sorts/stats : à chaque nouveau membre ajouté,
            // envoyer Nh<id> + Ns<id> au serveur pour récupérer ses sorts (sinon
            // on n'a aucun signal sur ce que le perso connaît).
            _ = DemanderSortsMembresAsync();
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessagePartyLeader>(msg =>
            _detecteurHeros.OnPartyLeader(msg));

        // PI : confirmations / erreurs d'invitation côté serveur.
        // Format observé Hystoria : « PIEa » = erreur (perso introuvable /
        // déjà dans un groupe). On notifie le Compte pour que l'AutoInviteur
        // sorte de son attente sans gaspiller 4s de timeout.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessagePartyInvitation>(msg =>
        {
            if (!string.IsNullOrEmpty(msg.Code))
            {
                Journaliseur.Avertir(
                    $"[GH-INVIT] Serveur refuse l'invitation : code='{msg.Code}' "
                    + "(probable perso introuvable / déjà dans un groupe)");
                _compte.DeclencherInvitationRefusee(msg.Code);
            }
        });

        // NO<flags>~<id>;<etat>|... — liste héros liés Hystoria Abrak.
        // Transmis à l'ActivateurHerosAbrak qui poursuit la séquence NA<ids>.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageHerosOrdre>(msg =>
        {
            _compte.ActivateurHerosAbrak?.OnHerosOrdre(msg);
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageHerosSorts>(msg =>
        {
            // Nh<id>|<sortId>~<niv>~<pos>;... — sorts d'un membre.
            if (msg.IdHeros == 0 || msg.Sorts.Count == 0) return;
            var groupe = _compte.GroupeHeros;
            if (groupe is null) return;
            groupe.NotifierSortsMembre(msg.IdHeros, msg.Sorts, msg.PositionsBarre);
            // Persistance disque : sauvegarde dans peleas/heros/<id>.json pour
            // que l'user puisse voir/éditer la config offline (et la
            // configuration soit chargée à la prochaine session).
            var membre = groupe.TrouverParIdJeu(msg.IdHeros);
            if (membre is not null)
            {
                BotDofus.Divers.MultiAccount.ServiceConfigsHeros.Sauvegarder(membre);
            }
        });

        // === COMBAT ABRAK EN CLAIR : positions des combattants ===
        // GTM = liste combattants+cellules ; GTS = à qui le tour. C'est ICI
        // qu'on récupère enfin les entités positionnées (pour l'IA combat).
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageCombattantsAbrak>(OnCombattantsAbrak);
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageTourCombatAbrak>(async msg =>
        {
            // Mode héros (Abrak) : GTSX = signal exclusif d'enrôlement, pas un tour.
            // Le détecteur instancie / enrichit le GroupeHeros du compte si applicable.
            if (msg.EstGTSX)
            {
                _detecteurHeros.OnGTSX(msg);
                // Les NLK des liés ont déjà été reçus à la connexion → on a leur
                // identité en cache, on peut résoudre Nom/Niveau immédiatement.
                EnrichirMembresHerosDepuisCache();
                // Si on découvre le groupe via GTSX (sans PM préalable), il faut
                // aussi demander les sorts des liés au serveur (l'auto-Nh/Ns du
                // PM handler n'a pas tourné parce que pas de PM).
                _ = DemanderSortsMembresAsync();
                return;
            }
            if (!msg.EstTour) return; // GTS mal formé
            _etat.Combat.IdentifiantAllie = _etat.Personnage.Identifiant;
            _etat.Combat.PassageEnCombat();
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);

            // Tracking ordre des tours pour l'UI groupe (no-op si pas en mode héros).
            _compte.GroupeHeros?.NotifierTourServeur(msg.IdentifiantCombattant, msg.NumeroTour);

            Journaliseur.Info($"[COMBAT] Tour de #{msg.IdentifiantCombattant} (tour {msg.NumeroTour})"
                + (msg.IdentifiantCombattant == _etat.Personnage.Identifiant ? " ← MOI" : ""));

            // IA combat : capture user 12:41:18. Hystoria utilise le format
            // Abrak (GTS<id>|<temps>|<num>) → le handler classique GTSx ligne
            // 90 ne se déclenche JAMAIS sur ce serveur. C'est ICI qu'il faut
            // brancher l'IA, sinon le bot reste 45 s muet par tour (cf. log
            // 13:09 → 6 tours sans le moindre GA300).
            if (_session is null) return;
            if (_compte.ModePassif)
            {
                Journaliseur.Info("[COMBAT] mode passif actif → IA désactivée, à toi de jouer.");
                return;
            }

            // Tour d'un héros lié (mode héros Abrak) : pilotage via IA dédiée.
            if (msg.IdentifiantCombattant != _etat.Personnage.Identifiant)
            {
                var groupe = _compte.GroupeHeros;
                var membre = groupe?.TrouverParIdJeu(msg.IdentifiantCombattant);
                if (membre is not null && membre.Role == BotDofus.Divers.MultiAccount.RoleDansGroupe.Suiveur)
                {
                    Journaliseur.Info($"[COMBAT] tour héros lié {membre.Nom} → IA dédiée");
                    try
                    {
                        await BotDofus.Divers.MultiAccount.IACombatHerosSimple.JouerTourAsync(
                            membre, _etat.Combat, _etat.CarteCourante, _session).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Journaliseur.Avertir($"[COMBAT-HEROS] erreur IA tour : {ex.Message}");
                        try { await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false); }
                        catch { /* swallow */ }
                    }
                }
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

    /// <summary>
    /// Envoie <c>Nh&lt;id&gt;</c> + <c>Ns&lt;id&gt;</c> pour chaque membre lié du
    /// groupe dont les sorts ne sont pas encore connus, et synchronise les
    /// sorts du master depuis <see cref="Personnage.SortsAppris"/>.
    ///
    /// Skip en mode passif (aucune injection automatique) et hors session.
    /// Émet espacé pour ne pas spammer le serveur.
    /// </summary>
    private async Task DemanderSortsMembresAsync()
    {
        var groupe = _compte.GroupeHeros;
        if (groupe is null) return;

        // Sync sorts du master depuis le SL standard (déjà reçu à la connexion) —
        // permet à la VueGroupeHeros et aux configs uniformes d'avoir les 8 persos
        // avec leurs sorts au même endroit (MembreHeros.SortsAppris).
        SynchroniserSortsMaster();

        if (_compte.ModePassif) return;
        if (_session is null) return;

        // Snapshot des liés avec sorts manquants (pas re-demander si déjà connus).
        var aDemander = new System.Collections.Generic.List<int>();
        foreach (var m in groupe.Membres)
        {
            if (m.IdJeu == 0) continue;
            if (m.Role == BotDofus.Divers.MultiAccount.RoleDansGroupe.Leader) continue; // master = SL déjà reçu
            if (m.SortsAppris.Count > 0) continue;
            aDemander.Add(m.IdJeu);
        }
        if (aDemander.Count == 0) return;

        foreach (var id in aDemander)
        {
            try
            {
                await _session.EnvoyerAuServeurAsync($"Nh{id}").ConfigureAwait(false);
                await Task.Delay(80).ConfigureAwait(false);
                await _session.EnvoyerAuServeurAsync($"Ns{id}").ConfigureAwait(false);
                await Task.Delay(120).ConfigureAwait(false);
                Journaliseur.Info($"[MODE-HEROS] Demande sorts pour id={id} (Nh + Ns envoyés)");
            }
            catch (Exception ex)
            {
                Journaliseur.Avertir($"[MODE-HEROS] Échec demande sorts id={id} : {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Copie les sorts du master (reçus via SL à la connexion, stockés dans
    /// <see cref="Personnage.SortsAppris"/>) dans son <see cref="BotDofus.Divers.MultiAccount.MembreHeros"/>.
    /// Permet d'avoir une vue uniforme « 8 persos avec leurs sorts » côté
    /// VueGroupeHeros (au lieu d'avoir le master sorts à un endroit + liés à
    /// un autre).
    /// </summary>
    private void SynchroniserSortsMaster()
    {
        var groupe = _compte.GroupeHeros;
        if (groupe is null) return;
        var leader = groupe.Leader;
        if (leader is null) return;
        var sortsPerso = _etat.Personnage.SortsAppris;
        if (sortsPerso is null || sortsPerso.Count == 0) return;
        if (leader.SortsAppris.Count == sortsPerso.Count) return; // déjà synchro

        var snapshotSorts = new System.Collections.Generic.Dictionary<int, int>(sortsPerso);
        groupe.NotifierSortsMembre(leader.IdJeu, snapshotSorts);
    }

    /// <summary>
    /// Tente de résoudre Nom/Niveau pour chaque <see cref="BotDofus.Divers.MultiAccount.MembreHeros"/>
    /// dont ces champs sont vides, en piochant dans <see cref="_acteursVus"/>
    /// (alimenté par NLK). No-op si pas de groupe ou tous les membres déjà résolus.
    /// </summary>
    private void EnrichirMembresHerosDepuisCache()
    {
        var groupe = _compte.GroupeHeros;
        if (groupe is null) return;
        foreach (var m in groupe.Membres)
        {
            if (!_acteursVus.TryGetValue(m.IdJeu, out var info)) continue;
            bool change = false;
            if (string.IsNullOrWhiteSpace(m.Nom) && !string.IsNullOrWhiteSpace(info.Nom))
            { m.Nom = info.Nom; change = true; }
            if (m.Niveau == 0 && info.Niveau > 0) { m.Niveau = info.Niveau; change = true; }
            if (change)
                Journaliseur.Info($"[MODE-HEROS] Identité résolue : id {m.IdJeu} → {info.Nom} (niv {info.Niveau})");
        }
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
            // Tracking invocations Sadida : si l'ID a déjà été identifié comme
            // MON invocation, c'est un allié peu importe le signe de l'ID.
            // Si nouveau combattant à une cell ciblée par un de mes casts
            // d'invoc → idem, et on l'ajoute au registre.
            bool estMonInvoc = _etat.Combat.MesInvocationsIds.Contains(c.Id);
            if (!estMonInvoc && _etat.Combat.CellsInvocationsAttendues.Contains(c.Cellule))
            {
                estMonInvoc = true;
                _etat.Combat.MesInvocationsIds.Add(c.Id);
                _etat.Combat.CellsInvocationsAttendues.Remove(c.Cellule);
                Journaliseur.Info($"[INVOC] Combattant #{c.Id} à cell {c.Cellule} identifié comme MON invocation (Sadida).");
            }

            // Heuristique PvM Incarnam : id < 0 = monstre/ennemi, id > 0 = joueur/allié.
            // EXCEPTION : si l'ID est dans MesInvocationsIds → toujours allié.
            bool ennemi = !estMonInvoc && c.Id < 0;
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
            // Marque les invocations alliées (utilisé par MoteurReglesCombat.ChoisirCible
            // pour PlusFaible/PlusFort qui skip les invocs adverses, ET pour ne PAS
            // que mes propres invocs soient ciblées comme ennemis).
            if (estMonInvoc) existant.EstInvocation = true;

            // FIX CRITIQUE (21/05 morning) : si c'est MOI, resync aussi
            // _etat.Personnage.CellulePosition. Sinon le mode SECOURS optimiste
            // (post-GA001 timeout broadcast) laisse `perso.CellulePosition` à
            // la cell d'arrivée espérée alors que le serveur n'a peut-être
            // jamais validé le déplacement. Le bot calculait ensuite ses
            // distances depuis la fausse cell → cast à 12 cases alors que
            // portée 8 (bug user log 063241 / screenshot Dofus).
            if (c.Id == _etat.Personnage.Identifiant && c.Cellule > 0)
            {
                int? avant = _etat.Personnage.CellulePosition;
                _etat.Personnage.CellulePosition = c.Cellule;
                if (avant != c.Cellule)
                    Journaliseur.Info($"[ACTION-MV] GTM resync : ma cell {avant} → {c.Cellule} (autoritative serveur)");
            }
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

        // Fan-out vers les MembreHeros du groupe (s'il existe) — met à jour PV/PA/PM/cell
        // live pour l'UI VueGroupeHeros. Ne déclenche d'event que si une valeur a changé.
        var groupe = _compte.GroupeHeros;
        if (groupe != null)
        {
            foreach (var c in msg.Combattants)
            {
                groupe.NotifierStatsCombattant(c.Id, c.Pv, c.PvMax, c.Pa, c.Pm, c.Cellule, c.Vivant);
            }
        }

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
            // Lock pour éviter race avec UI thread (VueCombat/VueInventaire qui itèrent).
            // Clear + Add tout dans un seul bloc lock = mutations atomiques côté thread réseau.
            lock (perso.Inventaire)
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
            }
            Journaliseur.Info($"[INV] Inventaire initial chargé : {msg.ObjetsInitiaux.Count} objets");
            perso.NotifierInventaireChange();
        }

        Journaliseur.Info($"Personnage : {msg.Nom} (classe #{msg.IdClasse}, niv {msg.Niveau})");

        // Bascule état en jeu — déclenche AutoInviteurHeros + ActivateurHerosAbrak
        // côté ContexteCompte. Sans ça, ni l'auto-invitation PI ni l'activation
        // mode héros NA n'ont lieu (forensic 2026-05-22 log 155158 — aucun
        // [ACTIV-HEROS] ni [GH-INVIT] sans cet appel).
        if (_compte.Etat != Divers.Enums.EtatsCompte.EnJeu
            && _compte.Etat != Divers.Enums.EtatsCompte.EnCombat)
        {
            _compte.ChangerEtat(Divers.Enums.EtatsCompte.EnJeu);
        }
    }

    private void OnStats(MessageStats msg)
    {
        // As : statistiques complètes du perso (PV / Énergie / PA / PM / Kamas / XP / pts caracs / pts sorts).
        var perso = _etat.Personnage;
        // Détection LEVEL UP via comparaison palier XP (msg.XpPalier différent
        // → on a passé un cap = nouveau niveau).
        long ancienPalier = perso.XpPalierCourant;
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

        // Detect level up : palier qui change ↑ + notification Discord si webhook configuré.
        if (ancienPalier > 0 && msg.XpPalier > ancienPalier && perso.Niveau > 0)
        {
            int nouveauNiveau = perso.Niveau + 1;
            Journaliseur.Info($"[LEVEL-UP] 🎉 {perso.Nom} → niveau {nouveauNiveau} !");
            var webhook = _compte.WebhookDiscordUrl;
            if (!string.IsNullOrWhiteSpace(webhook))
                _ = Divers.Notifications.NotificateurDiscord.NotifierLevelUpAsync(webhook, perso.Nom, nouveauNiveau);

            // Distribution auto des points carac selon la config du compte.
            // Fire-and-forget : si la config est Manuel ou null, le distributeur
            // retourne immédiatement sans envoyer de paquet.
            var cfgCaracs = _compte.ConfigCaracs;
            if (cfgCaracs != null && cfgCaracs.Mode != Divers.Caracteristiques.ModeDistribCaracs.Manuel
                && _compte.Api is { } api)
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        // Petite pause pour laisser le serveur stabiliser les points caracs côté Personnage.
                        await System.Threading.Tasks.Task.Delay(800).ConfigureAwait(false);
                        var distrib = new Divers.Caracteristiques.DistributeurCaracs(api, perso, cfgCaracs);
                        await distrib.DistribuerAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Journaliseur.Avertir($"[CARACS] Distribution auto échouée : {ex.Message}");
                    }
                });
            }
        }
    }

    private void OnDonneesCarte(MessageDonneesCarte msg)
    {
        Journaliseur.Info($"Changement de carte : #{msg.IdentifiantCarte}");
        _etat.ChangerCarte(msg.IdentifiantCarte, msg.DateVersion, msg.ClefCarte);
        // Snapshot inventaire à chaque changement de map : assure que les vues
        // affichent l'état courant (utile quand on déplace pendant un script Lua
        // sans qu'un OQ ait été reçu sur la map précédente).
        _etat.Personnage.NotifierInventaireChange();
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
            // Sur Hystoria, '~' est aussi utilisé pour UPDATE acteur (pas que mobs).
            // On exige idEntite<0 ET champ4 strictement numérique (liste gabarits)
            // pour ne pas confondre avec un update de joueur.
            bool champ4LooksLikeNumericList = champ4.Length > 0
                && champ4.All(c => char.IsDigit(c) || c == ',' || c == '-');
            if (entree.Operation == OperationGM.MonstreGroupe
                && idEntite < 0
                && champ4LooksLikeNumericList)
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
        var charge = msg.Charge ?? string.Empty;

        // ═══ Parser GAF<code>|<id> — fin d'action serveur (succès ou échec) ═══
        // Format Dofus 1.29 : GAF + code 1-2 digits + | + idActeur.
        // Code 0 = succès. ≠ 0 = échec (sort hors portée, cible morte, etc.)
        if (charge.StartsWith("F") && charge.Contains('|'))
        {
            var pipe = charge.IndexOf('|');
            var codeStr = charge.Substring(1, pipe - 1);
            var idStr = charge.Substring(pipe + 1);
            if (int.TryParse(codeStr, out var code)
                && int.TryParse(idStr, out var idActeur)
                && idActeur == _etat.Personnage.Identifiant
                && code != 0)
            {
                string raison = code switch
                {
                    1 => "cible/cellule invalide",
                    2 => "hors portée",
                    3 => "ligne de vue obstruée",
                    4 => "pas assez de PA",
                    5 => "déjà lancé ce tour",
                    6 => "cooldown actif",
                    _ => $"code {code} (inconnu)"
                };
                Journaliseur.Avertir($"[GAF-ECHEC] Action serveur refusée code={code} ({raison}) pour mon perso");
            }
        }

        var p = charge.Split(';');
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
            // EN COMBAT : si l'acteur est un héros lié allié, on met à jour sa
            // position dans Combat.Allies ET on déclenche MouvementBotConfirme
            // pour que IACombatHerosSimple.DeplacerAsync sorte de son attente
            // sans timeout (sinon 2500-3000ms perdus par déplacement de héros,
            // cf. logs 14:28:14 / 14:28:17 etc.).
            if (_etat.Combat.Etat != Divers.Combats.Enums.EtatCombat.Inactif)
            {
                var allie = _etat.Combat.Allies.FirstOrDefault(a => a.Identifiant == acteurId);
                if (allie != null)
                {
                    int avantHeros = allie.CellulePosition;
                    allie.CellulePosition = cell;
                    Journaliseur.Info(
                        $"[ACTION-MV] Position héros #{acteurId} confirmée : cell {avantHeros} → {cell} (broadcast GA;{p[0]};)");
                    _etat.Combat.DeclencherMouvementBot(acteurId, cell, chemin);
                    _etat.Combat.SignalerCombattantsMaj();
                    return;
                }
            }
            // Sinon (overworld) : entité de la carte courante.
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
        // Lock pour éviter race UI thread (cf. fix VueCombat/VueInventaire snapshot).
        lock (inv)
        {
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
        }
        Journaliseur.Debogue($"[INV] +{msg.ObjetsParse.Count} objets (total = {inv.Count})");
        _etat.Personnage.NotifierInventaireChange();
    }

    private void OnObjetRetrait(MessageObjetRetrait msg)
    {
        var inv = _etat.Personnage.Inventaire;
        int n;
        lock (inv) // race UI thread
        {
            n = inv.RemoveAll(x => x.Identifiant == msg.IdentifiantObjet);
        }
        if (n > 0)
        {
            Journaliseur.Debogue($"[INV] -1 objet (id {msg.IdentifiantObjet}, total = {inv.Count})");
            _etat.Personnage.NotifierInventaireChange();
        }
        // Compteur monotone consommé par PiloteBanque.AttendreObjectRemoveAsync
        // pour synchroniser les dépôts (chaque EMO+ déclenche un OR<id>|<uid>).
        System.Threading.Interlocked.Increment(ref BotDofus.Divers.Banque.PiloteBanque.CompteurObjectRemove);
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

        // Active le flag turbo global SI cfg le demande (master pilote).
        Divers.Combats.IA.TimingsCombat.AppliquerConfig(_compte.ConfigCombat);

        // Délai de réaction humanisé. Capture user passif 16:21-16:22 montre :
        //   - Tour 6 : GTS 16:21:58.058 → GA300 16:21:59.754 = 1696 ms
        //   - Tour 7 : GTS 16:22:02.921 → GA300 16:22:04.494 = 1573 ms
        // Vrais humains : 1.5-1.7 s entre voir le tour et cliquer un sort.
        // En mode turbo, ce délai est réduit à 50ms (cf. TimingsCombat).
        int delaiReaction = Divers.Combats.IA.TimingsCombat.DelaiActionCombat(1400, 2100);
        Journaliseur.Info($"[TOUR-START] Tour #{combat.NumeroTour} — cell {maCell}, PA={perso.PA}, PM={perso.PM}, "
            + $"alliés={combat.Allies.Count}, ennemis={combat.Ennemis.Count(e => !e.EstMort)}/{combat.Ennemis.Count}, "
            + $"mode={_compte.ConfigCombat?.Mode}, règles={_compte.ConfigCombat?.Regles.Count ?? 0}, "
            + $"délai réaction={delaiReaction}ms");
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

        // === SOIN AUTO CONSOMMABLE (Phase 9 PLAN-REFONTE) ===
        // Si PV% < seuil ET un consommable est configuré ET dispo dans l'inventaire,
        // l'utiliser via OAU<uid>. Capture la quantité avant/après via OQ.
        var cfgSoin = _compte.ConfigCombat;
        if (cfgSoin != null && cfgSoin.ConsommableSoinIdTemplate > 0 && perso.VieMax > 0)
        {
            int pvPct = (int)(100.0 * perso.Vie / perso.VieMax);
            if (pvPct < cfgSoin.ConsommableUtiliserSiPvInfPct)
            {
                var conso = perso.Inventaire.FirstOrDefault(o => o.IdTemplate == cfgSoin.ConsommableSoinIdTemplate && o.Quantite > 0);
                if (conso != null)
                {
                    Journaliseur.Info($"[SOIN] PV {pvPct}% < seuil {cfgSoin.ConsommableUtiliserSiPvInfPct}% → utilise consommable #{conso.IdTemplate} (uid={conso.Identifiant}, qte={conso.Quantite})");
                    await _session.EnvoyerAuServeurAsync($"OU{conso.Identifiant}").ConfigureAwait(false);
                    int delaiMin = cfgSoin.ConsommableDelaiMinMs > 0 ? cfgSoin.ConsommableDelaiMinMs : 150;
                    int delaiMax = cfgSoin.ConsommableDelaiMaxMs > delaiMin ? cfgSoin.ConsommableDelaiMaxMs : delaiMin + 250;
                    await Task.Delay(System.Random.Shared.Next(delaiMin, delaiMax)).ConfigureAwait(false);
                }
                else
                {
                    Journaliseur.Avertir($"[SOIN] PV {pvPct}% bas mais consommable #{cfgSoin.ConsommableSoinIdTemplate} ABSENT de l'inventaire");
                }
            }
        }

        // === ORDRE INVERSÉ (refonte 2026-05-22 — demande user) ===
        // CAST D'ABORD depuis la position actuelle (préserve le tacle CAC),
        // déplacement uniquement si aucun sort en portée (fallback), puis
        // repositionnement final pour préparer le tour suivant.
        // Avant : PRE-MOVE → multi-cast → FIN-TOUR. Problème : si le perso
        // bougeait depuis un CAC où il tacle un ennemi, il perdait son tacle
        // pour se rapprocher d'un autre mob → tour gaspillé bêtement.
        // → Le PreMouvementSelonModeAsync est maintenant déplacé APRÈS les casts.

        // === Phase 1 moteur règles SynFus/dyshay + MULTI-CAST (N.1) ===
        // Si l'user a configuré des règles dans peleas/<perso>.json, on les
        // évalue dans l'ordre de priorité décroissante. **Boucle multi-cast**
        // tant qu'une règle reste utilisable (PA dispo + sort en portée +
        // NombreParTour pas atteint). Garde max 8 itérations anti-boucle.
        var cfg = _compte.ConfigCombat;
        if (cfg != null && cfg.Regles.Count > 0 && _etat.CarteCourante != null)
        {
            int castsEffectues = 0;
            const int MAX_CASTS_PAR_TOUR = 8;
            while (castsEffectues < MAX_CASTS_PAR_TOUR)
            {
                // ═══ SYNC critical : avant chaque Evaluer(), aligner la cell
                // du Combattant représentant MOI dans Combat.Allies sur la
                // valeur autoritative perso.CellulePosition (mise à jour par
                // pré-mouvement / GTM resync). Sans ça, Evaluer voit l'ancienne
                // cell et TrouverCelluleVide retourne des cells adjacentes à
                // la mauvaise position → ANTI-BAN refuse le cast (log 09:57:24
                // « cible cell 381 dist=1 » mais réelle dist=3).
                var moiAllie = combat.Allies.FirstOrDefault(a => a.Identifiant == perso.Identifiant);
                if (moiAllie != null && perso.CellulePosition.HasValue)
                    moiAllie.CellulePosition = perso.CellulePosition.Value;

                var resultat = Divers.Combats.IA.MoteurReglesCombat.Evaluer(
                    combat, cfg, perso.SortsAppris, _etat.CarteCourante);
                if (resultat == null) break;
                await EnvoyerCastAsync(resultat).ConfigureAwait(false);
                castsEffectues++;
            }
            if (castsEffectues > 0)
            {
                // === GET_FIN_TURNO (dyshay) — repositionnement fin de tour ===
                // Refonte 2026-05-22 : généralise le POST-CAST-KITE Eloigne/Fuyard
                // à TOUS les modes qui ont une logique de positionnement :
                //   - Agressif sans CAC → avance (rush l'ennemi pour CAC tour suivant)
                //   - Fuyard ennemi proche < 8 → recule (kite)
                //   - Fuyard ennemi loin > 12 → avance (reste en portée)
                //   - Eloigne distance < DistanceMinEloigne → recule
                //   - Equilibre / Tactique → rien (préserve PM)
                // Réutilise PreMouvementSelonModeAsync car son score gère déjà tous
                // ces cas via mode + DistancePreferee + DistanceMinEloigne.
                if (perso.PM > 0)
                {
                    var ennemisEnVie = combat.Ennemis.Where(e => !e.EstMort && e.PV > 0 && e.PVMax > 0).ToList();
                    if (ennemisEnVie.Count > 0)
                    {
                        int maCellFin = perso.CellulePosition ?? 0;
                        int distMinFin = ennemisEnVie.Min(e => DistanceDofus(maCellFin, e.CellulePosition));
                        bool repositionner = cfg.Mode switch
                        {
                            Divers.Combats.IA.ModeCombat.Agressif => distMinFin > 1,
                            Divers.Combats.IA.ModeCombat.Fuyard => distMinFin <= 1 || distMinFin < 8 || distMinFin > 12,
                            Divers.Combats.IA.ModeCombat.Eloigne => distMinFin < cfg.DistanceMinEloigne,
                            _ => false
                        };
                        if (repositionner)
                        {
                            Journaliseur.Info($"[FIN-TOUR] Mode={cfg.Mode}, distMin={distMinFin}, PM={perso.PM} → repositionne");
                            await PreMouvementSelonModeAsync(perso, combat, ennemisEnVie).ConfigureAwait(false);
                        }
                    }
                }
                // GKK0 final pour fermer toute action en cours (déplacement
                // post-cast) avant le Gt. Sans ça, le serveur peut ignorer
                // le Gt jusqu'à 13s — forensic 2026-05-22 18:39:51 Ukdeshan.
                // Les GKK0 redondants sont safe (serveur ignore).
                try { await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false); } catch { /* best-effort */ }
                await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiApresDeplacement(150, 300)).ConfigureAwait(false);

                // Pass turn après tous les casts effectués ce tour.
                await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiPasserTour(800, 1300)).ConfigureAwait(false);
                Journaliseur.Info($"[ACTION] Passe le tour (Gt) — {castsEffectues} cast(s) ce tour");
                await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
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
                    int cellAvantMv = maCell;
                    var cellsTrace = string.Join("→", chemin.Select(c => c.Identifiant.ToString()));
                    var paquetDep = BotDofus.Divers.Cartes.Deplacement.Pathfinder.PaquetDeplacement(chemin);
                    var modeStr = _compte.ConfigCombat?.Mode.ToString() ?? "Equilibre";
                    Journaliseur.Info($"[PATHFINDING] Mode={modeStr} | départ cell {maCell} → arrivée cell {cellArrivee} | {nbPasMove} pas | dist après={distApres} | sort « {sortVise.Nom} » niv{nivVise} portée {pminVL}-{pmaxVL} | chemin: {cellsTrace}");
                    Journaliseur.Info($"[ACTION-MV] Envoi GA001 → '{paquetDep}' (cells {cellAvantMv}→{cellArrivee})");

                    // Pipeline event-based ADR-002 §3.3 : on envoie GA001 puis on
                    // attend le broadcast GA;0/1;<monId> via Combat.MouvementBotConfirme.
                    // Timeout passé à 3500ms (observé 2500ms parfois trop court
                    // sur cartes laggy — agent DIAG063 a vu jusqu'à 45s en cas
                    // extrême mais 3500ms suffit pour le cas typique laggy).
                    int idMoi = _etat.Personnage.Identifiant;
                    int timeoutMs = System.Math.Max(3500, nbPasMove * 500 + 1500);
                    await _session.EnvoyerAuServeurAsync(paquetDep).ConfigureAwait(false);

                    var resultat = await Divers.Combats.IA.PipelineDeplacementCombat
                        .AttendreMouvementOuTimeoutAsync(_etat.Combat, idMoi, cellArrivee, timeoutMs, default)
                        .ConfigureAwait(false);

                    bool deplacementValide = false;
                    int distApresMove = distApres;
                    switch (resultat)
                    {
                        case Divers.Combats.IA.ResultatDeplacementCombat.Confirme:
                            // perso.CellulePosition déjà mis à jour par OnActionJeu
                            maCell = _etat.Personnage.CellulePosition ?? cellArrivee;
                            Journaliseur.Info($"[ACTION-MV] Mouvement CONFIRMÉ serveur : cell {cellAvantMv}→{maCell}");
                            deplacementValide = true;
                            break;

                        case Divers.Combats.IA.ResultatDeplacementCombat.ConfirmePartiel:
                            // Serveur a tronqué le chemin (cell occupée mid-path)
                            maCell = _etat.Personnage.CellulePosition ?? cellAvantMv;
                            distApresMove = DistanceDofus(maCell, ennemi.CellulePosition);
                            Journaliseur.Avertir($"[ACTION-MV] Mouvement PARTIEL : visé cell {cellArrivee}, atteint {maCell} (dist réelle {distApresMove}, portée {pminVL}-{pmaxVL})");
                            // Cast possible si la dist reste dans la portée du sort
                            deplacementValide = distApresMove >= pminVL && (pmaxVL <= 0 || distApresMove <= pmaxVL);
                            break;

                        case Divers.Combats.IA.ResultatDeplacementCombat.TimeoutSilencieux:
                            // Aucune confirmation = serveur a refusé. Soit on tente
                            // le cast aveugle (mode secours, comportement legacy
                            // = pré-ADR-002), soit on abandonne le tour (sûr).
                            bool secours = _compte.ConfigCombat?.ModeDeplacementOptimisteSecours ?? true;
                            if (secours)
                            {
                                Journaliseur.Avertir($"[ACTION-MV] Timeout {timeoutMs}ms (pas de broadcast GA;0/1) → mode SECOURS optimiste activé (cast aveugle, perso supposé cell {cellArrivee})");
                                _etat.Personnage.CellulePosition = cellArrivee;
                                maCell = cellArrivee;
                                deplacementValide = true;
                            }
                            else
                            {
                                Journaliseur.Erreur($"[ACTION-MV] Mouvement REFUSÉ silencieux (timeout {timeoutMs}ms) : perso reste cell {cellAvantMv}, abandon tour");
                                await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
                                return;
                            }
                            break;
                    }

                    if (deplacementValide)
                    {
                        // G.4 (dyshay) — PAS de GKK0 proactif après GA001 combat.
                        // Dyshay laisse le serveur émettre GA;0/1 puis GAF, et
                        // c'est MapFrame.GAF qui renvoie GKK<n> en réaction.
                        // L'ancien GKK0 forcé pouvait être une autre cause du
                        // rejet silent côté serveur. Cf. agent REFPLACE BUG #4
                        // + docs/REFERENCE-PLACEMENT-DEPLACEMENT-DYSHAY.md §2.1.
                        await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiApresDeplacement(150, 300)).ConfigureAwait(false);

                        sort = sortVise;
                        sortCoutPA = paVL;
                        sortPorteeMin = pminVL;
                        sortPorteeMax = pmaxVL;
                        distEnnemi = distApresMove;
                    }
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
            // SÉCURITÉ : si on a bougé sans cast (ou même si on n'a rien fait),
            // un GKK0 final ferme proprement l'action côté serveur Hystoria.
            // Sans ça, le Gt peut être ignoré jusqu'à 13s (forensic Athabiel 14:38:28).
            try { await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false); } catch { /* swallow */ }
            await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiApresDeplacement(150, 300)).ConfigureAwait(false);
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
        // ═══ GARDE-FOU ANTI-BAN fallback legacy ═══
        // Idem EnvoyerCastAsync : check dist réelle au moment du cast pour éviter
        // tout GA300 hors portée (signature anti-bot serveur Hystoria).
        int maCellFallback = perso.CellulePosition ?? maCell;
        int distFallback = DistanceDofus(maCellFallback, ennemi.CellulePosition);
        if (distFallback < sortPorteeMin || (sortPorteeMax > 0 && distFallback > sortPorteeMax))
        {
            Journaliseur.Avertir(
                $"[ANTI-BAN] REFUS cast legacy « {sort.Nom} » : dist réelle {distFallback} "
                + $"hors portée [{sortPorteeMin}-{sortPorteeMax}] (ma cell {maCellFallback}, "
                + $"cible cell {ennemi.CellulePosition}) → Gt direct.");
            await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
            return;
        }

        var paquetSort = $"GA300{sort.Identifiant};{ennemi.CellulePosition}";
        int nivChoisi = perso.SortsAppris.TryGetValue(sort.Identifiant, out var nv) ? nv : 0;
        Journaliseur.Info($"[ACTION] Sort « {sort.Nom} » (#{sort.Identifiant} niv{nivChoisi}) "
            + $"sur cell {ennemi.CellulePosition} (cible « {ennemi.Nom} », {sortCoutPA} PA, portée {sortPorteeMin}-{sortPorteeMax}, dist réelle={distFallback})");
        await _session.EnvoyerAuServeurAsync(paquetSort).ConfigureAwait(false);

        // GKK0 : capture user 16:22:00.130 → 376 ms après GA300. Random 300-500.
        await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiLancerSort(300, 500)).ConfigureAwait(false);
        await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);

        // Gt : capture user montre que le serveur termine le tour ~1.5 s après
        // GKK0 quand le client a vidé ses PA. Pour rester sûr, on envoie Gt
        // explicite après 1-1.5 s (humanisé). Si le serveur a déjà fermé le
        // tour (GTF reçu), Gt est inoffensif (le serveur l'ignore).
        await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiPasserTour(1000, 1500)).ConfigureAwait(false);
        Journaliseur.Info("[ACTION] Passe le tour (Gt)");
        await _session.EnvoyerAuServeurAsync("Gt").ConfigureAwait(false);
    }

    /// <summary>
    /// Distance « cases Dofus » entre 2 cell-id (grille iso) — utilise CHEBYSHEV
    /// `max(|dx|, |dy|)` qui est la métrique canonique des PORTÉES de sorts Dofus
    /// (cases diagonales = distance 1). Vérifié log 22:40 : bot rejette portée
    /// Chebyshev=9 puis bouge 1 case → dist=8 = portée max Ronce → cast accepté
    /// par le serveur (broadcast GA;0; reçu).
    /// </summary>
    /// <remarks>
    /// Pour le PATHFINDER 4-dir (estimation PM consommée) utiliser Manhattan
    /// `|dx|+|dy|` (cf. <c>TrouverApprocheCombat.dEstimee</c>). Mais la PORTÉE
    /// elle-même reste Chebyshev — sinon le bot rejette des cells valides
    /// (ex. dist Chebyshev=5 mais Manhattan=10 sur diagonale).
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
            // Portée de sort = CHEBYSHEV (= métrique canonique Dofus, cases diag = 1).
            // Rollback H.1 partiel : analyse DIAG224 a montré que Manhattan était
            // contre-productif sur la portée — Chebyshev est correct.
            int d = System.Math.Max(System.Math.Abs(c.X - xE), System.Math.Abs(c.Y - yE));
            if (d < porteeMin || d > porteeMax) continue;
            // Estimation MANHATTAN du déplacement (4-dir combat = chemin sans diag)
            // — borne basse PM réaliste sans payer le coût d'A*.
            int dEstimee = System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y);
            if (dEstimee > pmMax) continue;

            // combat:true → pathfinder utilise 4 dirs ortho strictes (dyshay
            // PeleasPathfinder). En 8-dir le serveur 1.29 rejette silencieusement
            // les GA001 contenant une diagonale → bot reste figé (bug identifié
            // par agent REFPLACE 22:30, cf. docs/REFERENCE-PLACEMENT-DEPLACEMENT-DYSHAY.md §2).
            var chemin = BotDofus.Divers.Cartes.Deplacement.Pathfinder.Trouver(
                carte, depart, c, interdites, combat: true);
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
    /// <summary>
    /// Pré-mouvement selon Mode (demande user 07:13). Avant la boucle multi-cast :
    /// - Agressif → se rapprocher de l'ennemi le plus proche (idéal CAC dist=1)
    /// - Eloigne / Fuyard → s'éloigner au max de l'ennemi le plus proche
    /// - Equilibre → ajuster à DistancePreferee (skip si déjà bien)
    /// Utilise le pathfinder combat 4-dir + le pipeline event-based ADR-002.
    /// </summary>
    private async Task PreMouvementSelonModeAsync(
        BotDofus.Divers.Jeu.Personnage.Personnage perso,
        BotDofus.Divers.Combats.Combat combat,
        System.Collections.Generic.List<BotDofus.Divers.Combats.Combattants.Combattant> ennemisVivants)
    {
        if (perso.PM <= 0 || perso.CellulePosition is not int maCellId) return;
        var carte = _etat.CarteCourante;
        if (carte == null) return;
        var depart = carte.Obtenir(maCellId);
        if (depart == null) return;
        var cfg = _compte.ConfigCombat;
        var mode = cfg?.Mode ?? BotDofus.Divers.Combats.IA.ModeCombat.Equilibre;
        int distPref = cfg?.DistancePreferee ?? 5;
        int distMinEloigne = cfg?.DistanceMinEloigne ?? 6;

        // Smart positioning multi-mobs : on score chaque cell candidate via la
        // SOMME des distances Chebyshev vers TOUS les ennemis vivants (style
        // dyshay Get_Total_Distancia_Enemigo). Plus précis que l'ancien
        // « distance vers l'ennemi le plus proche » quand il y a 2+ mobs.
        int mw = carte.Largeur > 0 ? carte.Largeur : BotDofus.Divers.Cartes.Carte.LargeurParDefaut;
        var ennemisXY = BotDofus.Divers.Combats.IA.ScorePositionCombat.CoordsEnnemis(ennemisVivants, mw);
        var (xMoi, yMoi) = BotDofus.Divers.Cartes.Cellule.CalculerCoordonnees(maCellId, mw);
        int distActuelle = BotDofus.Divers.Combats.IA.ScorePositionCombat.DistanceMin(xMoi, yMoi, ennemisXY);
        double scoreActuel = BotDofus.Divers.Combats.IA.ScorePositionCombat.ScoreCellule(
            xMoi, yMoi, ennemisXY, mode, distPref, distMinEloigne);

        // Gate par mode : skip si déjà optimal (distance min sans rien à gagner).
        switch (mode)
        {
            case BotDofus.Divers.Combats.IA.ModeCombat.Agressif when distActuelle <= 1: return;
            case BotDofus.Divers.Combats.IA.ModeCombat.Equilibre when System.Math.Abs(distActuelle - distPref) <= 1: return;
        }

        int pmMax = perso.PM;

        // === MOTEUR TACTIQUE AVANCÉ (ADR-008) : tente d'abord la fonction
        // CalculerMeilleureCellule qui prend en compte sort principal + LOS +
        // kite intelligent. Si pas de sort identifiable, fallback sur la
        // logique multi-mobs basique ci-dessous. ===
        var resultatTactique = TenterMoteurTactiqueMaster(perso, combat, carte, cfg, ennemisVivants, mw, ennemisXY, pmMax);
        if (resultatTactique.HasValue)
        {
            var (cellTac, cheminTac, pmTac, distFinTac) = resultatTactique.Value;
            Journaliseur.Info(
                $"[TACTIC] PRE-MOVE Mode={mode} | cell {maCellId} → {cellTac.Identifiant} | {pmTac} pas | distMin {distActuelle}→{distFinTac}");
            var paquetDepTac = BotDofus.Divers.Cartes.Deplacement.Pathfinder.PaquetDeplacement(cheminTac);
            await _session.EnvoyerAuServeurAsync(paquetDepTac).ConfigureAwait(false);

            int timeoutTac = System.Math.Max(3500, pmTac * 500 + 1500);
            var resTac = await Divers.Combats.IA.PipelineDeplacementCombat
                .AttendreMouvementOuTimeoutAsync(combat, perso.Identifiant, cellTac.Identifiant, timeoutTac, default)
                .ConfigureAwait(false);
            if (resTac == Divers.Combats.IA.ResultatDeplacementCombat.Confirme
                || resTac == Divers.Combats.IA.ResultatDeplacementCombat.ConfirmePartiel)
            {
                Journaliseur.Info($"[TACTIC] Mouvement {resTac} : cell réelle = {perso.CellulePosition}");
                await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiApresDeplacement(300, 500)).ConfigureAwait(false);
            }
            else
            {
                Journaliseur.Avertir($"[TACTIC] Timeout {timeoutTac}ms — serveur n'a pas confirmé.");
            }
            return;
        }
        // Fallback (legacy multi-mobs) — code ci-dessous.

        // Cells occupées = interdites (alliés vivants + ennemis vivants).
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

        // Énumère cells atteignables (4-dir, PM max), score via SOMME des
        // distances à TOUS les ennemis vivants (multi-mobs).
        BotDofus.Divers.Cartes.Cellule? meilleureCible = null;
        System.Collections.Generic.List<BotDofus.Divers.Cartes.Cellule>? meilleurChemin = null;
        double meilleurScore = scoreActuel; // ne bouge que si on FAIT MIEUX
        int meilleurNbPasTie = int.MaxValue;
        int meilleureDistAfter = distActuelle;

        foreach (var c in carte.Cellules)
        {
            if (c == null || !c.EstMarchable || c.IdInteractif >= 0 || interdites.Contains(c)) continue;
            int dEstimee = System.Math.Abs(c.X - depart.X) + System.Math.Abs(c.Y - depart.Y);
            if (dEstimee == 0 || dEstimee > pmMax) continue;

            double score = BotDofus.Divers.Combats.IA.ScorePositionCombat.ScoreCellule(
                c.X, c.Y, ennemisXY, mode, distPref, distMinEloigne);

            var chemin = BotDofus.Divers.Cartes.Deplacement.Pathfinder.Trouver(carte, depart, c, interdites, combat: true);
            if (chemin == null) continue;
            int nbPas = chemin.Count - 1;
            if (nbPas <= 0 || nbPas > pmMax) continue;
            if (score < meilleurScore || (score == meilleurScore && nbPas < meilleurNbPasTie))
            {
                meilleurScore = score;
                meilleurNbPasTie = nbPas;
                meilleureCible = c;
                meilleurChemin = chemin;
                meilleureDistAfter = BotDofus.Divers.Combats.IA.ScorePositionCombat.DistanceMin(c.X, c.Y, ennemisXY);
            }
        }

        if (meilleureCible == null || meilleurChemin == null)
        {
            Journaliseur.Info($"[PRE-MOVE] Mode={mode} : aucune cell d'amélioration trouvée (dist min actuelle={distActuelle}, scoreActuel={scoreActuel:F1}, PM={pmMax}, ennemis={ennemisXY.Length})");
            return;
        }

        // Skip si la cible est notre cell actuelle ou si le mouvement n'améliore RIEN
        // (pour Agressif → on veut réduire la dist ; pour Eloigne → augmenter).
        // Le score est déjà le critère absolu — meilleurScore est strictement
        // inférieur à scoreActuel sinon meilleurChemin serait null.
        bool ameliore = meilleurScore < scoreActuel;
        if (!ameliore)
        {
            Journaliseur.Info($"[PRE-MOVE] Mode={mode} : pas d'amélioration (dist {distActuelle} → {meilleureDistAfter})");
            return;
        }

        int sommeDistApres = BotDofus.Divers.Combats.IA.ScorePositionCombat.SommeDistances(
            meilleureCible.X, meilleureCible.Y, ennemisXY);
        var paquetDep = BotDofus.Divers.Cartes.Deplacement.Pathfinder.PaquetDeplacement(meilleurChemin);
        Journaliseur.Info($"[PRE-MOVE] Mode={mode} | départ cell {maCellId} → arrivée cell {meilleureCible.Identifiant} "
            + $"| {meilleurNbPasTie} pas | distMin {distActuelle}→{meilleureDistAfter}, ΣdistEnnemis→{sommeDistApres} ({ennemisXY.Length} ennemis)");
        Journaliseur.Info($"[ACTION-MV] Envoi GA001 (pré-mouvement) → '{paquetDep}'");
        await _session.EnvoyerAuServeurAsync(paquetDep).ConfigureAwait(false);

        int timeoutMs = System.Math.Max(3500, meilleurNbPasTie * 500 + 1500);
        var resultat = await Divers.Combats.IA.PipelineDeplacementCombat
            .AttendreMouvementOuTimeoutAsync(combat, perso.Identifiant, meilleureCible.Identifiant, timeoutMs, default)
            .ConfigureAwait(false);

        if (resultat == Divers.Combats.IA.ResultatDeplacementCombat.Confirme
            || resultat == Divers.Combats.IA.ResultatDeplacementCombat.ConfirmePartiel)
        {
            Journaliseur.Info($"[PRE-MOVE] Mouvement {resultat} : cell réelle = {perso.CellulePosition}");
            await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiApresDeplacement(300, 500)).ConfigureAwait(false);
        }
        else
        {
            Journaliseur.Avertir($"[PRE-MOVE] Timeout {timeoutMs}ms — serveur n'a pas confirmé. Le bot continue le tour sur sa position actuelle.");
        }
    }

    /// <summary>
    /// Tente le moteur tactique avancé (ADR-008) pour le master : identifie
    /// le sort principal de la rotation + la cible prioritaire, calcule la
    /// distance d'arrêt idéale (kite intelligent en Eloigne) et invoque
    /// <see cref="Divers.Combats.IA.MoteurTactique.CalculerMeilleureCellule"/>.
    /// Retourne <c>null</c> si pas de sort identifiable (fallback legacy).
    /// </summary>
    private (BotDofus.Divers.Cartes.Cellule, System.Collections.Generic.List<BotDofus.Divers.Cartes.Cellule>, int, int)?
        TenterMoteurTactiqueMaster(
            BotDofus.Divers.Jeu.Personnage.Personnage perso,
            BotDofus.Divers.Combats.Combat combat,
            BotDofus.Divers.Cartes.Carte carte,
            BotDofus.Divers.Combats.IA.ConfigCombat? cfg,
            System.Collections.Generic.List<BotDofus.Divers.Combats.Combattants.Combattant> ennemisVivants,
            int mapWidth,
            (int x, int y)[] ennemisXY,
            int pmMax)
    {
        if (cfg == null || cfg.Regles.Count == 0) return null;
        if (perso.CellulePosition is not int maCellId) return null;
        var depart = carte.Obtenir(maCellId);
        if (depart == null) return null;

        // Identifie le sort principal (1re règle valide : sort appris,
        // offensif, cible ennemi vivante).
        Divers.Jeu.Personnage.Spells.InfoSort? sortPrincipal = null;
        Divers.Combats.Combattants.Combattant? cible = null;
        int porteeMinSort = 0, porteeMaxSort = 0;
        bool sortLOS = false;
        foreach (var regle in cfg.Regles.OrderByDescending(r => r.Priorite))
        {
            if (regle.IdSort <= 0) continue;
            if (!perso.SortsAppris.TryGetValue(regle.IdSort, out var niveau) || niveau <= 0) continue;
            var sort = Divers.Jeu.Personnage.Spells.BaseSorts.Instance.Trouver(regle.IdSort);
            if (sort == null) continue;
            var stats = sort.Stats(niveau);
            int pmaxL = stats?.PorteeMax ?? sort.PorteeMax;
            if (pmaxL <= 0) continue;
            int pminL = stats?.PorteeMin ?? sort.PorteeMin;
            bool losL = stats?.NecessiteLOS ?? false;

            // Cible selon Focus (offensif seulement).
            Divers.Combats.Combattants.Combattant? cibleL = regle.Focus switch
            {
                Divers.Combats.IA.FocusSort.EnnemiLePlusProche => ennemisVivants
                    .OrderBy(e => DistanceDofus(maCellId, e.CellulePosition)).FirstOrDefault(),
                Divers.Combats.IA.FocusSort.EnnemiLePlusFaible => ennemisVivants
                    .Where(e => !e.EstInvocation).DefaultIfEmpty(ennemisVivants.FirstOrDefault())
                    .OrderBy(e => e?.PV ?? int.MaxValue).FirstOrDefault(),
                Divers.Combats.IA.FocusSort.EnnemiLePlusFort => ennemisVivants
                    .Where(e => !e.EstInvocation).DefaultIfEmpty(ennemisVivants.FirstOrDefault())
                    .OrderByDescending(e => e?.PV ?? -1).FirstOrDefault(),
                Divers.Combats.IA.FocusSort.EnnemiLePlusLoin => ennemisVivants
                    .OrderByDescending(e => DistanceDofus(maCellId, e.CellulePosition)).FirstOrDefault(),
                _ => null,
            };
            if (cibleL == null) continue;
            sortPrincipal = sort; cible = cibleL;
            porteeMinSort = pminL; porteeMaxSort = pmaxL; sortLOS = losL;
            break;
        }
        if (sortPrincipal == null || cible == null) return null;

        // Mode effectif : si bas PV et FuirSiPvBas activé, bascule en Fuyard
        // même si la config dit Agressif/Equilibre. Permet la "désengagement
        // d'urgence" automatique sans toucher au mode global.
        var modeEffectif = cfg.ModeEffectif(perso.Vie, perso.VieMax);

        var ctx = new Divers.Combats.IA.ScorePositionCombat.ContexteTactique(
            Mode: modeEffectif,
            PorteeMinSort: porteeMinSort,
            PorteeMaxSort: porteeMaxSort,
            SortNecessiteLOS: sortLOS,
            PmEnnemiCible: cible.PM,
            DistancePreferee: cfg.DistancePreferee,
            DistanceMinEloigne: cfg.DistanceMinEloigne);

        int distIdeale = Divers.Combats.IA.ScorePositionCombat.DistanceIdeale(ctx);
        var (xCible, yCible) = BotDofus.Divers.Cartes.Cellule.CalculerCoordonnees(cible.CellulePosition, mapWidth);
        string suffixMode = modeEffectif != cfg.Mode ? $" (effectif {modeEffectif} car bas PV)" : "";
        Journaliseur.Info(
            $"[TACTIC] Mode={cfg.Mode}{suffixMode}, sort=#{sortPrincipal.Identifiant} portée [{porteeMinSort}-{porteeMaxSort}] LOS={sortLOS} "
            + $"| cible #{cible.Identifiant} cell {cible.CellulePosition} pmEnnemi={cible.PM} → distIdéale={distIdeale}");

        // Interdites = combattants vivants sauf moi.
        var interdites = new System.Collections.Generic.HashSet<BotDofus.Divers.Cartes.Cellule>();
        var interdites_int = new System.Collections.Generic.HashSet<int>();
        foreach (var a in combat.Allies)
        {
            if (a.Identifiant == perso.Identifiant || a.EstMort) continue;
            var cellA = carte.Obtenir(a.CellulePosition);
            if (cellA != null) { interdites.Add(cellA); interdites_int.Add(cellA.Identifiant); }
        }
        foreach (var e in combat.Ennemis)
        {
            if (e.EstMort) continue;
            var cellE = carte.Obtenir(e.CellulePosition);
            if (cellE != null) { interdites.Add(cellE); interdites_int.Add(cellE.Identifiant); }
        }

        Divers.Combats.IA.MoteurTactique.TestLosDelegate testLos = (depuis, vers) =>
            !BotDofus.Divers.Cartes.LigneVisuelle.EstObstruee(carte, depuis, vers, interdites_int);

        var resultat = Divers.Combats.IA.MoteurTactique.CalculerMeilleureCellule(
            carte, depart, pmMax, interdites, ennemisXY, (xCible, yCible), ctx, testLos,
            exigeAmelioration: true);

        if (resultat == null) return null;
        return (resultat.Cible, new System.Collections.Generic.List<BotDofus.Divers.Cartes.Cellule>(resultat.Chemin), resultat.PmConsommes, resultat.DistanceFinaleCible);
    }

    /// <summary>
    /// N.1 — Envoie un seul cast (GA300 + GKK0) SANS pass turn. La boucle
    /// multi-cast dans <see cref="JouerTourCombatAsync"/> rappelle cette
    /// méthode tant qu'une règle SynFus est utilisable + PA dispo + cible
    /// en portée + NombreParTour pas atteint. Le Gt final est émis par
    /// l'appelant.
    /// </summary>
    private async Task EnvoyerCastAsync(Divers.Combats.IA.MoteurReglesCombat.ResultatRegle r)
    {
        Journaliseur.Info($"[DECIDEUR] Règle SynFus retenue : « {r.Sort.Nom} » "
            + $"(#{r.Sort.Identifiant} niv{r.NiveauAppris}) focus={r.Regle.Focus}, "
            + $"cible « {r.Cible.Nom} » cell {r.Cible.CellulePosition} "
            + $"(dist={r.Distance}, {r.CoutPA} PA, portée {r.PorteeMin}-{r.PorteeMax})");

        // ═══ GARDE-FOU ANTI-BAN (demande user 08:18) ═══
        // Recalcule la distance RÉELLE avec la position perso ACTUELLE au moment
        // du cast (peut avoir bougé après que MoteurReglesCombat l'a évaluée).
        // Si dist > porteeMax → REFUS catégorique d'envoyer GA300. Le serveur
        // Hystoria détecte les casts hors portée comme signature anti-bot → ban.
        int maCellMaintenant = _etat.Personnage.CellulePosition ?? 0;
        int distReelle = DistanceDofus(maCellMaintenant, r.Cible.CellulePosition);
        if (distReelle < r.PorteeMin || (r.PorteeMax > 0 && distReelle > r.PorteeMax))
        {
            Journaliseur.Avertir(
                $"[ANTI-BAN] REFUS cast « {r.Sort.Nom} » : dist réelle {distReelle} "
                + $"hors portée [{r.PorteeMin}-{r.PorteeMax}] (ma cell {maCellMaintenant}, "
                + $"cible cell {r.Cible.CellulePosition}). Le serveur aurait rejeté "
                + "le sort, signature anti-bot → annulation locale.");
            var combatGardeR = _etat.Combat;
            var cleGR = (_etat.Personnage.Identifiant, r.Sort.Identifiant);
            combatGardeR.CompteursRegleParTour[cleGR] =
                (combatGardeR.CompteursRegleParTour.TryGetValue(cleGR, out var cntGR) ? cntGR : 0)
                + System.Math.Max(1, r.Regle.NombreParTour);
            return;
        }

        // Check LOS si nécessaire (sorts à ligne droite obligatoire).
        var statsR = r.Sort.Stats(r.NiveauAppris);
        bool besoinLOS = statsR?.NecessiteLOS ?? false;
        if (besoinLOS && _etat.CarteCourante != null && distReelle > 1)
        {
            var celluleMoi = _etat.CarteCourante.Obtenir(maCellMaintenant);
            var celluleCible = _etat.CarteCourante.Obtenir(r.Cible.CellulePosition);
            if (celluleMoi != null && celluleCible != null)
            {
                var occupees = new System.Collections.Generic.HashSet<int>(
                    _etat.Combat.Allies.Where(a => !a.EstMort).Select(a => a.CellulePosition)
                        .Concat(_etat.Combat.Ennemis.Where(e => !e.EstMort).Select(e => e.CellulePosition)));
                if (BotDofus.Divers.Cartes.LigneVisuelle.EstObstruee(_etat.CarteCourante, celluleMoi, celluleCible, occupees))
                {
                    Journaliseur.Avertir(
                        $"[ANTI-BAN] REFUS cast « {r.Sort.Nom} » : LIGNE DE VUE OBSTRUÉE "
                        + $"entre cell {maCellMaintenant} et cible cell {r.Cible.CellulePosition} "
                        + "(combattant sur trajectoire). Le serveur aurait rejeté le sort.");
                    var combatGardeL = _etat.Combat;
                    var cleGL = (_etat.Personnage.Identifiant, r.Sort.Identifiant);
                    combatGardeL.CompteursRegleParTour[cleGL] =
                        (combatGardeL.CompteursRegleParTour.TryGetValue(cleGL, out var cntGL) ? cntGL : 0)
                        + System.Math.Max(1, r.Regle.NombreParTour);
                    return;
                }
            }
        }

        // Si c'est un sort d'invocation (Focus=CelluleVide ou CelluleAdjacenteEnnemi),
        // tracker la cell ciblée → le prochain combattant qui apparaît à cette
        // cell sera classé comme MON invocation (Allies + EstInvocation) au lieu
        // d'ennemi par l'heuristique id<0 (fix bug user 09:58 « il tape ses invocs »).
        if (r.Regle.Focus == Divers.Combats.IA.FocusSort.CelluleVide
            || r.Regle.Focus == Divers.Combats.IA.FocusSort.CelluleAdjacenteEnnemi)
        {
            _etat.Combat.CellsInvocationsAttendues.Add(r.Cible.CellulePosition);
            Journaliseur.Info($"[INVOC] Cell {r.Cible.CellulePosition} ajoutée aux invocations attendues");
        }

        var paquet = $"GA300{r.Sort.Identifiant};{r.Cible.CellulePosition}";
        Journaliseur.Info($"[ACTION] Sort « {r.Sort.Nom} » niv{r.NiveauAppris} "
            + $"sur cell {r.Cible.CellulePosition} (cible « {r.Cible.Nom} », "
            + $"{r.CoutPA} PA, portée {r.PorteeMin}-{r.PorteeMax}, dist réelle={distReelle})");
        // Trigger animation flash sur la cell cible dans MapViewer.
        _etat.Combat.DeclencherCast(r.Sort.Identifiant, r.Sort.Nom, r.Cible.CellulePosition);
        await _session.EnvoyerAuServeurAsync(paquet).ConfigureAwait(false);

        // Compteur NombreParTour + NombreParCible + DernierTour (cooldown).
        // Compteurs CLOISONNÉS par caster (idActif = master ici).
        var combat = _etat.Combat;
        int idActif = _etat.Personnage.Identifiant;
        var cleT = (idActif, r.Sort.Identifiant);
        combat.CompteursRegleParTour[cleT] =
            (combat.CompteursRegleParTour.TryGetValue(cleT, out var cnt) ? cnt : 0) + 1;
        var cleParCible = (idActif, r.Sort.Identifiant, r.Cible.Identifiant);
        combat.CompteursRegleParCible[cleParCible] =
            (combat.CompteursRegleParCible.TryGetValue(cleParCible, out var cntC) ? cntC : 0) + 1;
        combat.DernierTourLanceParSort[r.Sort.Identifiant] = combat.NumeroTour;

        // OPTIMISTIC : décrémenter les PA côté bot pour que le prochain Evaluer()
        // de la boucle multi-cast voie le bon budget restant. Le serveur sync
        // via GTS au tour suivant.
        if (_etat.Personnage.PA >= r.CoutPA)
            _etat.Personnage.PA -= r.CoutPA;

        // GKK0 ack + délai humanisé inter-cast (pas trop court pour éviter
        // signature anti-bot ; pas trop long pour laisser tourner la boucle).
        await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiLancerSort(300, 500)).ConfigureAwait(false);
        await _session.EnvoyerAuServeurAsync("GKK0").ConfigureAwait(false);
        await Task.Delay(Divers.Combats.IA.TimingsCombat.DelaiEntreDeuxSorts(500, 900)).ConfigureAwait(false);
    }
}
