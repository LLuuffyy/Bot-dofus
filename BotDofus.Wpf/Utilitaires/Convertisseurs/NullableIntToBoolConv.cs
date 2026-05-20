using System;
using System.Globalization;
using System.Windows.Data;

namespace BotDofus.Wpf.Convertisseurs;

/// <summary>
/// Drive l'état coché d'une <see cref="System.Windows.Controls.CheckBox"/> depuis
/// une propriété <c>int?</c> de <see cref="BotDofus.Divers.Combats.IA.RegleSort"/> :
/// HasValue → coché, null → décoché. Utilisé en <c>OneWay</c> ; l'écriture est
/// pilotée par les handlers <c>Checked</c>/<c>Unchecked</c> côté code-behind
/// (set valeur par défaut quand coché, null quand décoché) — cf. blueprint
/// <c>docs/UI-SYNFUS-BLUEPRINT.md</c> §6.
/// </summary>
public sealed class NullableIntToBoolConv : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
