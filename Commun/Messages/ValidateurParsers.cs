using System;
using System.Collections.Generic;
using System.Text;
using BotDofus.Commun.Reseau;
using BotDofus.Commun.Messages.VersClient.Authentification;

namespace BotDofus.Commun.Messages;

/// <summary>
/// Banc de validation des parsers contre les VRAIS paquets capturés sur Aqua 1.39
/// (extraits du log Synfus fourni). Permet de prouver que le décodage protocolaire
/// fonctionne sans avoir besoin d'une session de jeu live.
///
/// Lancé via <c>Luffy-bot.exe --testparsers</c> (voir App.OnStartup).
/// </summary>
public static class ValidateurParsers
{
    private sealed record Cas(string Nom, string PaquetBrut, Func<MessageDofus, (bool ok, string detail)> Verif);

    public static string ExecuterTout()
    {
        FabriqueMessages.EnregistrerMessagesStandards();

        var cas = new List<Cas>
        {
            new("HC challenge",
                "HCQBKDRVVSGTJMCRHDOUUVYNRWXGLSEHJI",
                m => m is MessageHelloConnexion hc
                    ? (hc.Cle == "QBKDRVVSGTJMCRHDOUUVYNRWXGLSEHJI",
                       $"Cle='{hc.Cle}' (len={hc.Cle.Length})")
                    : (false, $"type={m.GetType().Name}")),

            new("Af file d'attente",
                "Af2|0|2|0||-1",
                m => (m is MessageQueuePosition, $"type={m.GetType().Name} charge='{m.Charge}'")),

            new("Ad pseudo",
                "AdRatcoon",
                m => m is MessagePseudo p
                    ? (p.Pseudo == "Ratcoon", $"Pseudo='{p.Pseudo}'")
                    : (false, $"type={m.GetType().Name}")),

            new("AH liste serveurs Aqua",
                "AH2;1;10;1|4;0;10;0|100;0;10;0|102;0;10;0|101;0;10;0",
                m =>
                {
                    if (m is not MessageServeursDisponibles s) return (false, $"type={m.GetType().Name}");
                    var sb = new StringBuilder();
                    foreach (var srv in s.Serveurs) sb.Append($"[{srv.Identifiant}:{(srv.EnLigne ? "ON" : "off")}]");
                    var serveur2EnLigne = false;
                    foreach (var srv in s.Serveurs)
                        if (srv.Identifiant == 2 && srv.EnLigne) serveur2EnLigne = true;
                    return (s.Serveurs.Count == 5 && serveur2EnLigne,
                            $"{s.Serveurs.Count} serveurs {sb} (Aqua id=2 attendu ON)");
                }),

            new("AYK redirect jeu (hostname Aqua)",
                "AYKaqua.play-astra.net:5562;MODYPMVDRBZVEOAB|506291",
                m =>
                {
                    if (m is not MessageHoteChiffre y) return (false, $"type={m.GetType().Name}");
                    var ok = y.Hote == "aqua.play-astra.net"
                          && y.Port == 5562
                          && y.Ticket == "MODYPMVDRBZVEOAB|506291";
                    return (ok, $"Hote='{y.Hote}' Port={y.Port} Ticket='{y.Ticket}'");
                }),

            new("ALK liste personnages",
                "ALK593033945|9|457817;Peyal;108;101;-1;-1;-1;195b,1f49,69f,1e22,null,1,;0;2;0;;200|457819;Ralou;114;101;-1;-1;-1;30e,98f,1b0f,null,null,1,;0;2;0;;200",
                m => (m is MessageListePersonnages,
                      $"type={m.GetType().Name} charge.len={m.Charge.Length}")),

            new("ATK ticket OK",
                "ATK0",
                m => (m is MessageTicket, $"type={m.GetType().Name}")),
        };

        var sortie = new StringBuilder();
        sortie.AppendLine("=== Validation parsers contre paquets Aqua 1.39 réels ===");
        int ok = 0;
        foreach (var c in cas)
        {
            MessageDofus msg;
            try
            {
                msg = FabriqueMessages.FabriquerDepuis(new PaquetBrut(DirectionPaquet.VersClient, c.PaquetBrut));
            }
            catch (Exception ex)
            {
                sortie.AppendLine($"  [CRASH] {c.Nom} : {ex.GetType().Name} {ex.Message}");
                continue;
            }

            var (reussi, detail) = c.Verif(msg);
            sortie.AppendLine($"  [{(reussi ? "OK  " : "FAIL")}] {c.Nom} → {detail}");
            if (reussi) ok++;
        }
        sortie.AppendLine($"=== {ok}/{cas.Count} parsers validés ===");
        return sortie.ToString();
    }
}
