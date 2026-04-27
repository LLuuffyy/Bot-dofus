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
        Ecouter<MessageInfoMessage>(OnInfoMessage);
        Ecouter<MessageInfoVie>(OnInfoVie);
        Ecouter<MessageSelectionPersonnage>(OnSelectionPersonnage);
        Ecouter<MessageStats>(OnStats);
        Ecouter<MessageObjetAjout>(OnObjetAjout);
        Ecouter<MessageObjetRetrait>(OnObjetRetrait);
        Ecouter<MessageObjetQuantite>(OnObjetQuantite);
        Ecouter<MessageObjetPoids>(msg => _etat.Personnage.ActualiserPoids(msg.PoidsActuel, msg.PoidsMax));
        Ecouter<MessageMouvementCarte>(OnMouvementCarte);
        Ecouter<MessageChatMessage>(OnChatMessage);
        Ecouter<MessageChatServeur>(msg => Journaliseur.Info($"[SERVEUR] {msg.Texte}"));

        // Mises à jour de l'état combat (GS, GE, GT) — alimente Combat.Etat / NumeroTour pour la vue live.
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageDebutCombat>(_ =>
        {
            _etat.Combat.Demarrer();
            _etat.Combat.PassageEnCombat();
            _compte.ChangerEtat(EtatsCompte.EnCombat);
        });
        Ecouter<BotDofus.Commun.Messages.VersClient.Jeu.MessageFinCombat>(_ =>
        {
            _etat.Combat.Reinitialiser();
            _compte.ChangerEtat(EtatsCompte.EnJeu);
            Journaliseur.Info("[COMBAT] Combat terminé");
        });
        Ecouter<MessageTourCombat>(msg =>
        {
            _etat.Combat.NouveauTour(msg.IdentifiantCombattant);
            _compte.ChangerEtat(EtatsCompte.EnCombat);
        });

        Ecouter<MessagePingMoyen>(_ => { /* silence ping */ });
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
        perso.PA = msg.PA;
        perso.PM = msg.PM;
        perso.ActualiserVie(msg.Vie, msg.VieMax);
        perso.ActualiserEnergie(msg.Energie, msg.EnergieMax);
    }

    private void OnDonneesCarte(MessageDonneesCarte msg)
    {
        Journaliseur.Info($"Changement de carte : #{msg.IdentifiantCarte}");
        _etat.ChangerCarte(msg.IdentifiantCarte, msg.Clef);
    }

    private void OnMouvementCarte(MessageMouvementCarte msg)
    {
        var carte = _etat.CarteCourante;
        if (carte == null) return;

        foreach (var entree in msg.Entrees)
        {
            if (entree.Operation == OperationGM.Despawn)
            {
                carte.Entites.Remove(entree.IdentifiantEntite);
                continue;
            }

            var champs = entree.ContenuBrut.Length > 1
                ? entree.ContenuBrut[1..].Split(';')
                : Array.Empty<string>();
            if (champs.Length < 2 || !int.TryParse(champs[0], out var cellule))
            {
                continue;
            }

            var type = ParserInt(champs, 1);
            var idEntite = ParserInt(champs, 3);

            if (EstEntreeJoueur(type, champs))
            {
                var nom = champs.ElementAtOrDefault(4) ?? string.Empty;
                var niveau = ParserNiveau(champs.ElementAtOrDefault(6));

                if ((idEntite != 0 && idEntite == _etat.Personnage.Identifiant)
                    || (!string.IsNullOrWhiteSpace(nom) && nom == _etat.Personnage.Nom))
                {
                    _etat.Personnage.CellulePosition = cellule;
                    continue;
                }

                carte.Entites[idEntite] = new EntiteJoueur
                {
                    Identifiant = idEntite,
                    CellulePosition = cellule,
                    Nom = nom,
                    Niveau = niveau,
                    Sexe = ParserInt(champs, 5)
                };
                continue;
            }

            if (type == 1)
            {
                var gabarits = champs.ElementAtOrDefault(4)?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var niveaux = champs.ElementAtOrDefault(6)?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var idGabarit = ParserInt(gabarits.ElementAtOrDefault(0));
                var niveau = niveaux.Select(ParserNiveau).DefaultIfEmpty(0).Max();
                var id = idEntite != 0 ? idEntite : -Math.Abs(cellule + 1);

                carte.Entites[id] = new EntiteMonstre
                {
                    Identifiant = id,
                    CellulePosition = cellule,
                    IdGabarit = idGabarit,
                    NiveauGroupe = niveau,
                    TailleGroupe = Math.Max(1, gabarits.Length),
                    Nom = NomMonstre(idGabarit)
                };
                continue;
            }

            if (type == -3 || type == 2)
            {
                var id = idEntite != 0 ? idEntite : cellule;
                carte.Entites[id] = new EntitePNJ
                {
                    Identifiant = id,
                    CellulePosition = cellule,
                    IdGabarit = Math.Abs(idEntite),
                    Nom = champs.ElementAtOrDefault(4) ?? $"PNJ #{Math.Abs(idEntite)}"
                };
            }
        }
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
}
