using System;
using System.IO;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Commun.Reseau;

/// <summary>
/// Enregistre tous les paquets qui traversent un <see cref="ProxyReseau"/>
/// dans un fichier plat (un paquet par ligne) pour analyse a posteriori.
/// Format : <c>HH:mm:ss.fff DIR PREFIXE CONTENU</c>.
///
/// Très utile pour construire des jeux de données de référence pour l'IA
/// de combat ou pour rétro-ingénierer des messages non encore typés.
/// </summary>
public sealed class EnregistreurPaquets : IDisposable
{
    private readonly string _chemin;
    private StreamWriter? _ecrivain;
    private readonly object _verrou = new();

    public EnregistreurPaquets(string dossier)
    {
        Directory.CreateDirectory(dossier);
        _chemin = Path.Combine(dossier, $"paquets-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    public string Chemin => _chemin;

    public void AttacherA(ProxyReseau proxy)
    {
        lock (_verrou)
        {
            _ecrivain ??= new StreamWriter(_chemin, append: true) { AutoFlush = true };
        }
        proxy.PaquetRecu += OnPaquet;
        Journaliseur.Info($"Enregistrement des paquets activé : {_chemin}");
    }

    public void Detacher(ProxyReseau proxy)
    {
        proxy.PaquetRecu -= OnPaquet;
    }

    private void OnPaquet(object? sender, EvenementPaquetRecu e)
    {
        var paquet = e.Paquet;
        var sens = paquet.Direction == DirectionPaquet.VersClient ? "SRV→CLI" : "CLI→SRV";
        var ligne = $"{paquet.Horodatage:HH:mm:ss.fff} {sens} {paquet.Prefixe} {paquet.Contenu}";

        lock (_verrou)
        {
            try { _ecrivain?.WriteLine(ligne); } catch { /* ignoré */ }
        }
    }

    public void Dispose()
    {
        lock (_verrou)
        {
            try { _ecrivain?.Dispose(); } catch { }
            _ecrivain = null;
        }
    }
}
