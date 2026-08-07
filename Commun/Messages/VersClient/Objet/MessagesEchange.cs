using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages.VersClient.Objet;

// =====================================================================
// VersClient / Échange (banque, marchand, échange joueur…)
// =====================================================================

/// <summary>
/// EV : l'échange en cours (banque, marchand, troc) est fermé. Peut être
/// émis par le serveur (S→C, après EV C→S) ou observé sur le canal C→S
/// si l'user ferme la fenêtre manuellement. Le pilote banque utilise ce
/// signal pour stopper ses dépôts en cours.
/// </summary>
public sealed class MessageEchangeFin : MessageDofus, IMessageVersClient
{
    public override string Prefixe => "EV";
    public override DirectionPaquet Direction => DirectionPaquet.VersClient;
    public override void Desserialiser(string charge) { Charge = charge; }
}
