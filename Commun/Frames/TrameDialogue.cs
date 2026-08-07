using System;
using System.Threading.Tasks;
using BotDofus.Commun.Messages.VersServeur.Dialogue;
using BotDofus.Commun.Reseau;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Frames;

/// <summary>
/// Phase transitoire de dialogue avec un PNJ. Peut être empilée par-dessus
/// <see cref="TrameJeu"/> sans la désactiver.
/// </summary>
public sealed class TrameDialogue : TrameBase
{
    private readonly SessionProxy _session;

    public TrameDialogue(Repartiteur repartiteur, SessionProxy session) : base(repartiteur)
    {
        _session = session;
    }

    protected override void EnregistrerGestionnaires()
    {
        // TODO : écouter DC (création), DQ (question+choix), DV (fin).
    }

    public async Task ChoisirReponseAsync(int identifiantReponse)
    {
        try
        {
            await _session.EnvoyerAuServeurAsync(new MessageDialogueReponse
            {
                IdentifiantReponse = identifiantReponse
            }.Serialiser()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Journaliseur.Erreur("Échec envoi réponse dialogue", ex);
        }
    }

    public async Task QuitterAsync()
    {
        try { await _session.EnvoyerAuServeurAsync(new MessageDialogueQuitter().Serialiser()).ConfigureAwait(false); }
        catch (Exception ex) { Journaliseur.Erreur("Échec quitter dialogue", ex); }
    }
}
