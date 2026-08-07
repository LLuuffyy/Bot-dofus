namespace BotDofus.Divers.Combats.IA;

/// <summary>
/// Mode de combat = profil de POSITIONNEMENT pendant le combat. Distinct de
/// <see cref="StrategieCombat"/> (qui décrit un style de jeu, ex. Soutien) et
/// de <see cref="PositionnementCombat"/> (qui ne pilote que le placement
/// initial avant le combat).
/// </summary>
/// <remarks>
/// Algo de choix de cellule cible (cf. dyshay <c>FightExtensions.get_Mover</c>
/// + ADR-001 §3) :
/// <list type="bullet">
/// <item><b>Agressif</b> : minimise <c>distance(cell, ennemi) - 1</c> → engage en CAC.</item>
/// <item><b>Eloigne</b> : maximise <c>distance(cell, ennemi)</c>, dans la portée
/// max du sort → distance maximale qui reste castable.</item>
/// <item><b>Fuyard</b> : si <c>PV % &lt; SeuilFuitePv</c>, maximise distance et
/// skip cast (pass turn) ; sinon comportement Eloigne.</item>
/// <item><b>Equilibre</b> (défaut) : minimise <c>|distance - cfg.DistancePreferee|</c>
/// → tient la distance préférée, fuit si PV bas.</item>
/// </list>
/// Compat : <c>Equilibre</c> par défaut → comportement quasi identique à
/// l'ancien <c>TrouverApprocheCombat</c> (qui visait juste « en portée »).
/// </remarks>
public enum ModeCombat
{
    /// <summary>Engage corps-à-corps (dist=1), ignore les PV.</summary>
    Agressif,
    /// <summary>Reste à distance max de la portée du sort, kite si possible.</summary>
    Eloigne,
    /// <summary>Maximise la distance, fuit si PV bas (skip cast).</summary>
    Fuyard,
    /// <summary>Tient <c>cfg.DistancePreferee</c>, comportement par défaut.</summary>
    Equilibre
}
