using BotDofus.Commun.Reseau;

namespace BotDofus.Commun.Messages;

/// <summary>
/// Message de repli pour tout paquet dont le préfixe n'a pas de classe typée
/// enregistrée dans la <see cref="FabriqueMessages"/>. Le contenu reste accessible
/// via <see cref="Charge"/> et <see cref="Source"/> pour inspection / journalisation.
/// </summary>
public sealed class MessageInconnu : MessageDofus
{
    public MessageInconnu(string prefixe, DirectionPaquet direction)
    {
        _prefixe = prefixe;
        _direction = direction;
    }

    private readonly string _prefixe;
    private readonly DirectionPaquet _direction;

    public override string Prefixe => _prefixe;
    public override DirectionPaquet Direction => _direction;

    public override void Desserialiser(string charge) { Charge = charge; }
}
