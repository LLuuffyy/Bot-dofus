using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotDofus.Divers.Combats.Enums;
using BotDofus.Divers.Combats.IA;
using BotDofus.Divers.Donnees;
using BotDofus.Divers.Jeu;
using BotDofus.Divers.Jeu.Personnage.Spells;
using BotDofus.Utilitaires.Journaux;
using MoonSharp.Interpreter;

namespace BotDofus.Divers.Scripts.Api;

/// <summary>
/// API exposée aux scripts Lua. Wrappe <see cref="ApiBot"/> et <see cref="EtatJeu"/>
/// avec une surface adaptée au scripting (méthodes synchrones bloquantes,
/// noms simples, accès direct à l'état).
///
/// Usage Lua :
/// <code>
///   bot.dire("$", "Hello world")
///   pos = bot.position()
///   if not bot.est_en_combat() then bot.deplacer(123) end
///   while bot.vie() > 0 do
///     bot.attendre(1000)
///   end
///
///   -- Configuration sorts en combat (l'IA les utilise automatiquement)
///   bot.config_combat_ajouter_sort{ id = 413, priorite = 10, cout_pa = 4, portee_max = 1, cible = "ennemi_proche" }
/// </code>
/// </summary>
[MoonSharpUserData]
public sealed class ApiLua
{
    private readonly ApiBot _api;
    private readonly EtatJeu _etat;
    private readonly ConfigCombat _configCombat;
    private readonly BotDofus.Divers.Interception.GestionnaireInterception? _interception;

    // Jeton d'annulation du script (posé par MoteurLuaInteractif.Demarrer).
    // Permet aux trajets en boucle de s'arrêter proprement : bot.attendre()
    // se débloque et bot.actif() renvoie false quand on clique « Arrêter ».
    private CancellationToken _ct = CancellationToken.None;
    public void DefinirAnnulation(CancellationToken ct) => _ct = ct;

    public ApiLua(ApiBot api, EtatJeu etat, ConfigCombat configCombat,
        BotDofus.Divers.Interception.GestionnaireInterception? interception = null)
    {
        _api = api;
        _etat = etat;
        _configCombat = configCombat;
        _interception = interception;
        Anka = new ApiAnka(api, etat, () => _ct);
    }

    /// <summary>Couche compatible AnkaBot (modules character/map/inventory/npc/…).</summary>
    public ApiAnka Anka { get; }

    // ---------------------------------------------------------------
    // Logging
    // ---------------------------------------------------------------

    /// <summary>Log un message dans la console du bot.</summary>
    public void log(string message) => Journaliseur.Info($"[LUA] {message}");

    /// <summary>Log un avertissement.</summary>
    public void avertir(string message) => Journaliseur.Avertir($"[LUA] {message}");

    // ---------------------------------------------------------------
    // État perso
    // ---------------------------------------------------------------

    public string nom() => _etat.Personnage.Nom;
    public int niveau() => _etat.Personnage.Niveau;
    public int vie() => _etat.Personnage.Vie;
    public int vie_max() => _etat.Personnage.VieMax;
    public double vie_pct() => _etat.Personnage.PourcentageVie;
    public long kamas() => _etat.Personnage.Kamas;
    public int pa() => _etat.Personnage.PA;
    public int pm() => _etat.Personnage.PM;
    public int? carte() => _etat.Personnage.CarteCourante;
    public int? position() => _etat.Personnage.CellulePosition;
    public int poids() => _etat.Personnage.PoidsActuel;
    public int poids_max() => _etat.Personnage.PoidsMax;
    public double poids_pct() => _etat.Personnage.PourcentagePoids;

    // ---------------------------------------------------------------
    // État monde
    // ---------------------------------------------------------------

    public bool est_en_combat() => _etat.Combat.Etat != EtatCombat.Inactif;

    /// <summary>Renvoie le nombre de monstres sur la carte (excluant nos joueurs).</summary>
    public int nb_monstres()
    {
        if (_etat.CarteCourante == null) return 0;
        int n = 0;
        foreach (var e in _etat.CarteCourante.Entites.Values)
        {
            if (e is BotDofus.Divers.Cartes.Entites.EntiteMonstre) n++;
        }
        return n;
    }

    // ---------------------------------------------------------------
    // Actions
    // ---------------------------------------------------------------

    /// <summary>Déplace le perso vers une cellule. Bloque jusqu'à envoi du paquet.</summary>
    public bool deplacer(int celluleCible)
    {
        return _api.SeDeplacerVersCelluleAsync(celluleCible, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Envoie un message dans un canal de chat (* = général, % = guilde, $ = équipe...).</summary>
    public void dire(string canal, string texte)
    {
        _api.EnvoyerMessageAsync(canal, texte, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Envoie la commande serveur ".travel x,y".</summary>
    public void travel(int x, int y)
    {
        _api.EnvoyerTravelAsync(x, y, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Attend N millisecondes. Interrompu net si on arrête le script.</summary>
    public void attendre(int millisecondes)
    {
        try { Task.Delay(millisecondes, _ct).GetAwaiter().GetResult(); }
        catch (System.OperationCanceledException) { }
    }

    /// <summary>Termine le tour en combat.</summary>
    public void finir_tour()
    {
        _api.FinirTourAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    // ---------------------------------------------------------------
    // Trajets / routes (scripting de déplacement & farm)
    // ---------------------------------------------------------------

    /// <summary>true tant que le script n'a pas été arrêté. Pour les boucles :
    /// <c>while bot.actif() do ... end</c>.</summary>
    public bool actif() => !_ct.IsCancellationRequested;

    /// <summary>Va sur la cellule (x,y) de la carte courante (pathfinding).</summary>
    public bool aller_xy(int x, int y)
    {
        var c = _etat.CarteCourante?.ObtenirParCoords(x, y);
        if (c == null) { Journaliseur.Avertir($"[LUA] aller_xy : cellule ({x},{y}) introuvable"); return false; }
        return _api.SeDeplacerVersCelluleAsync(c.Identifiant, _ct).GetAwaiter().GetResult();
    }

    /// <summary>Change de map par une sortie : "nord","sud","est","ouest".</summary>
    public bool changer_map(string direction)
        => _api.ChangerMapDirectionAsync(direction, _ct).GetAwaiter().GetResult();

    /// <summary>Récolte la ressource de la cellule donnée (skill auto via la BDD).</summary>
    public bool recolter(int cellule)
    {
        var c = _etat.CarteCourante?.Obtenir(cellule);
        if (c == null || c.IdInteractif < 0)
        { Journaliseur.Avertir($"[LUA] recolter : cell {cellule} non interactive"); return false; }
        var io = BaseDonnees.Instance.Interactif(c.IdInteractif);
        int skill = io?.IdSkill ?? 45;
        _api.RecolterAsync(cellule, c.IdInteractif, skill, _ct).GetAwaiter().GetResult();
        return true;
    }

    /// <summary>Récolte TOUTES les ressources exploitables de la carte. Renvoie le nombre.</summary>
    public int recolter_tout()
        => _api.RecolterToutAsync(_ct).GetAwaiter().GetResult();

    /// <summary>Nombre de ressources récoltables par ce perso ici.</summary>
    public int nb_recoltables() => _api.NbRecoltables();

    /// <summary>Engage le groupe de monstres le plus proche (combat direct).</summary>
    public bool engager_proche()
        => _api.EngagerCombatAsync(_ct).GetAwaiter().GetResult();

    /// <summary>Bloque tant qu'un combat est en cours (ou script arrêté).</summary>
    public void attendre_fin_combat()
    {
        while (_etat.Combat.Etat != EtatCombat.Inactif && !_ct.IsCancellationRequested)
        {
            try { Task.Delay(1000, _ct).GetAwaiter().GetResult(); }
            catch (System.OperationCanceledException) { break; }
        }
    }

    /// <summary>Parle à un PNJ (id négatif du sprite).</summary>
    public void parler_pnj(int idPnj)
        => _api.ParlerPnjAsync(0, idPnj, _ct).GetAwaiter().GetResult();

    /// <summary>Répond dans le dialogue PNJ : question + réponse.</summary>
    public void repondre(int question, int reponse)
        => _api.RepondreDialogueAsync(question, reponse, _ct).GetAwaiter().GetResult();

    /// <summary>Quitte le dialogue PNJ courant.</summary>
    public void quitter_dialogue()
        => _api.QuitterDialogueAsync(_ct).GetAwaiter().GetResult();

    /// <summary>Coordonnée X du perso sur la carte (-1 si inconnue).</summary>
    public int pos_x()
    {
        var c = _etat.Personnage.CellulePosition is int p ? _etat.CarteCourante?.Obtenir(p) : null;
        return c?.X ?? -1;
    }

    /// <summary>Coordonnée Y du perso sur la carte (-1 si inconnue).</summary>
    public int pos_y()
    {
        var c = _etat.Personnage.CellulePosition is int p ? _etat.CarteCourante?.Obtenir(p) : null;
        return c?.Y ?? -1;
    }

    // ---------------------------------------------------------------
    // Config combat (utilisée par BoucleIaCombat)
    // ---------------------------------------------------------------

    /// <summary>Vide la liste actuelle de sorts configurés.</summary>
    public void config_combat_vider() => _configCombat.Regles.Clear();

    /// <summary>
    /// Ajoute un sort à la rotation IA combat.
    /// Lua: bot.config_combat_ajouter_sort{ id = 413, priorite = 10, cout_pa = 4, portee_max = 1, cible = "ennemi_proche" }
    /// </summary>
    public void config_combat_ajouter_sort(Table args)
    {
        var regle = new RegleSort
        {
            IdSort = (int)(args.Get("id").CastToNumber() ?? 0),
            Priorite = (int)(args.Get("priorite").CastToNumber() ?? 5),
            CoutPA = (int)(args.Get("cout_pa").CastToNumber() ?? 4),
            PorteeMin = (int)(args.Get("portee_min").CastToNumber() ?? 1),
            PorteeMax = (int)(args.Get("portee_max").CastToNumber() ?? 6),
        };

        var cible = args.Get("cible").CastToString();
        regle.Cible = cible switch
        {
            "ennemi_proche" => CibleSort.EnnemiPlusProche,
            "ennemi_faible" => CibleSort.EnnemiPlusFaible,
            "ennemi_fort" => CibleSort.EnnemiPlusFort,
            "soi" => CibleSort.Soi,
            "allie_blesse" => CibleSort.AlliePlusBlesse,
            _ => CibleSort.EnnemiPlusProche,
        };

        _configCombat.Regles.Add(regle);
        Journaliseur.Info($"[LUA] Sort ajouté : id={regle.IdSort} cible={regle.Cible} priorite={regle.Priorite}");
    }

    // ---------------------------------------------------------------
    // Base de données des sorts (BaseSorts)
    // ---------------------------------------------------------------

    /// <summary>Retourne l'ID d'un sort à partir de son nom (recherche insensitive case, partial match).</summary>
    public int sort_id(string nom)
    {
        var s = BaseSorts.Instance.Chercher(nom).FirstOrDefault();
        if (s == null) { Journaliseur.Avertir($"[LUA] Sort '{nom}' introuvable"); return 0; }
        return s.Identifiant;
    }

    /// <summary>Retourne le nom d'un sort à partir de son ID.</summary>
    public string sort_nom(int id) => BaseSorts.Instance.Trouver(id)?.Nom ?? $"Sort #{id}";

    /// <summary>Retourne le coût en PA d'un sort.</summary>
    public int sort_cout_pa(int id) => BaseSorts.Instance.Trouver(id)?.CoutPA ?? 0;

    /// <summary>Retourne la portée min/max d'un sort.</summary>
    public Table sort_portee(int id)
    {
        var info = BaseSorts.Instance.Trouver(id);
        var t = new Table(null);
        t["min"] = info?.PorteeMin ?? 0;
        t["max"] = info?.PorteeMax ?? 0;
        return t;
    }

    /// <summary>Liste tous les sorts dont le nom contient le motCle (max 50 résultats).</summary>
    public Table sorts_chercher(string motCle)
    {
        var t = new Table(null);
        int i = 1;
        foreach (var s in BaseSorts.Instance.Chercher(motCle).Take(50))
        {
            t[i++] = $"#{s.Identifiant} {s.Nom}";
        }
        return t;
    }

    /// <summary>
    /// Ajoute un sort par NOM (recherche dans BaseSorts) — bien plus pratique que par ID.
    /// Lua: bot.config_combat_ajouter_sort_nom("Pied du Sacrieur", 10, "ennemi_proche")
    /// </summary>
    public void config_combat_ajouter_sort_nom(string nom, int priorite, string cible)
    {
        var info = BaseSorts.Instance.Chercher(nom).FirstOrDefault();
        if (info == null)
        {
            Journaliseur.Avertir($"[LUA] Sort '{nom}' introuvable, ignoré");
            return;
        }
        var regle = new RegleSort
        {
            IdSort = info.Identifiant,
            Priorite = priorite,
            CoutPA = info.CoutPA,
            PorteeMin = info.PorteeMin,
            PorteeMax = info.PorteeMax,
            Cible = cible switch
            {
                "ennemi_proche" => CibleSort.EnnemiPlusProche,
                "ennemi_faible" => CibleSort.EnnemiPlusFaible,
                "ennemi_fort" => CibleSort.EnnemiPlusFort,
                "soi" => CibleSort.Soi,
                "allie_blesse" => CibleSort.AlliePlusBlesse,
                _ => CibleSort.EnnemiPlusProche,
            },
        };
        _configCombat.Regles.Add(regle);
        Journaliseur.Info($"[LUA] Sort ajouté : '{info.Nom}' (#{info.Identifiant}, PA={info.CoutPA}, range={info.PorteeMin}-{info.PorteeMax}) priorité={priorite}");
    }

    /// <summary>Définit la stratégie globale ("agressif", "tactique", "defensif", "soutien", "passif").</summary>
    public void config_combat_strategie(string strategie)
    {
        _configCombat.Strategie = strategie switch
        {
            "agressif" => StrategieCombat.Agressif,
            "tactique" => StrategieCombat.Tactique,
            "defensif" => StrategieCombat.Defensif,
            "soutien" => StrategieCombat.Soutien,
            "passif" => StrategieCombat.Passif,
            _ => StrategieCombat.Agressif,
        };
        Journaliseur.Info($"[LUA] Stratégie combat : {_configCombat.Strategie}");
    }

    // ---------------------------------------------------------------
    // Base de données générale (items, monstres, NPCs, maps, skills)
    // ---------------------------------------------------------------

    public string item_nom(int id) => BaseDonnees.Instance.Item(id)?.Nom ?? $"Item #{id}";
    public int item_id(string nom)
    {
        var i = BaseDonnees.Instance.ChercherItem(nom).FirstOrDefault();
        return i?.Identifiant ?? 0;
    }

    public string monstre_nom(int id) => BaseDonnees.Instance.Monstre(id)?.Nom ?? $"Mob #{id}";
    public int monstre_niveau(int id) => BaseDonnees.Instance.Monstre(id)?.Niveau ?? 0;
    public int monstre_pv(int id) => BaseDonnees.Instance.Monstre(id)?.PointsVie ?? 0;

    public string npc_nom(int id) => BaseDonnees.Instance.Npc(id)?.Nom ?? $"NPC #{id}";

    public string skill_nom(int id) => BaseDonnees.Instance.Skill(id) ?? $"Skill #{id}";

    /// <summary>Coordonnées (x, y) d'une carte par son ID.</summary>
    public Table map_coords(int id)
    {
        var m = BaseDonnees.Instance.Map(id);
        var t = new Table(null);
        t["x"] = m?.X ?? 0;
        t["y"] = m?.Y ?? 0;
        return t;
    }

    // ---------------------------------------------------------------
    // Inventaire (live) — itère via l'EtatJeu
    // ---------------------------------------------------------------

    /// <summary>Retourne la liste des objets en inventaire (id, nom, quantité, position).</summary>
    public Table inventaire()
    {
        var t = new Table(null);
        int i = 1;
        foreach (var obj in _etat.Personnage.Inventaire)
        {
            var sub = new Table(null);
            sub["id"] = obj.Identifiant;
            sub["template"] = obj.IdTemplate;
            sub["nom"] = BaseDonnees.Instance.Item(obj.IdTemplate)?.Nom ?? $"Item #{obj.IdTemplate}";
            sub["quantite"] = obj.Quantite;
            sub["position"] = obj.Position;
            t[i++] = sub;
        }
        return t;
    }

    /// <summary>Compte un item par template ID dans l'inventaire (somme les piles).</summary>
    public int inventaire_compter(int templateId)
    {
        int total = 0;
        foreach (var obj in _etat.Personnage.Inventaire)
        {
            if (obj.IdTemplate == templateId) total += obj.Quantite;
        }
        return total;
    }

    // ---------------------------------------------------------------
    // Interception furtive (Phase 2) — modif inline du flux client réel
    // ---------------------------------------------------------------

    /// <summary>
    /// Ajoute une règle d'interception. La fonction Lua reçoit (contenu, sens) où
    /// sens = "C2S" (client→serveur) ou "S2C" (serveur→client), et retourne :
    ///   nil / le contenu inchangé → laisser passer
    ///   ""                         → supprimer le paquet
    ///   une autre string           → remplacer le paquet
    ///
    /// Lua :
    ///   bot.intercepter("test", "GA", function(p, sens)
    ///     bot.log("vu "..sens.." "..p)
    ///     return nil
    ///   end)
    /// </summary>
    public void intercepter(string nom, string prefixe, MoonSharp.Interpreter.Closure fn)
    {
        if (_interception == null) { avertir("Interception indisponible (pas de session)"); return; }

        _interception.Ajouter(new BotDofus.Divers.Interception.RegleInterception
        {
            Nom = nom,
            Prefixe = prefixe ?? string.Empty,
            Transformateur = (contenu, dir) =>
            {
                var sens = dir == BotDofus.Commun.Reseau.DirectionPaquet.VersServeur ? "C2S" : "S2C";
                var ret = fn.Call(contenu, sens);
                if (ret == null || ret.IsNil() || ret.Type == MoonSharp.Interpreter.DataType.Void)
                    return BotDofus.Divers.Interception.ResultatInterception.Laisser;
                var s = ret.CastToString();
                if (s == null) return BotDofus.Divers.Interception.ResultatInterception.Laisser;
                if (s.Length == 0) return BotDofus.Divers.Interception.ResultatInterception.Supprimer;
                if (s == contenu) return BotDofus.Divers.Interception.ResultatInterception.Laisser;
                return BotDofus.Divers.Interception.ResultatInterception.Remplacer(s);
            }
        });
    }

    /// <summary>Retire une règle d'interception par son nom.</summary>
    public void intercepter_retirer(string nom) => _interception?.Retirer(nom);

    /// <summary>Retire toutes les règles d'interception.</summary>
    public void intercepter_vider() => _interception?.Vider();

    /// <summary>Active/désactive globalement l'interception (kill-switch).</summary>
    public void intercepter_actif(bool actif)
    {
        if (_interception != null) _interception.Active = actif;
    }

    // ---------------------------------------------------------------
    // Humaniseur anti-burst (Phase 3) — cadence des actions autonomes
    // ---------------------------------------------------------------

    /// <summary>Active/désactive la garde anti-burst (espacement humain des envois bot).</summary>
    public void humaniseur_actif(bool actif) => _api.Humaniseur.Actif = actif;

    /// <summary>Règle la plage de cadence humaine (ms) entre deux actions bot.</summary>
    public void humaniseur_cadence(int minMs, int maxMs)
        => _api.Humaniseur.Cadence = new BotDofus.Divers.Securite.Plage(
            Math.Max(0, minMs), Math.Max(minMs, maxMs));

    /// <summary>Attend explicitement le respect de la cadence (à appeler avant une action critique).</summary>
    public void humaniseur_attendre()
        => _api.Humaniseur.RespecterCadenceAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Nombre de délais anti-burst imposés depuis le début (métrique).</summary>
    public long humaniseur_delais() => _api.Humaniseur.DelaisImposes;
}
