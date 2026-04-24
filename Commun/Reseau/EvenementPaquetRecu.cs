using System;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// EventArgs porté par l'événement PaquetRecu du proxy. Contient le paquet brut
/// avec sa direction et permet aux couches supérieures (Frames, UI Debug) de
/// réagir ou de journaliser.
/// </summary>
public sealed class EvenementPaquetRecu : EventArgs
{
    public EvenementPaquetRecu(PaquetBrut paquet)
    {
        Paquet = paquet;
    }

    public PaquetBrut Paquet { get; }
}
