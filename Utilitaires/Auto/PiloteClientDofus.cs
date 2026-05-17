using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BotDofus.Utilitaires.Journaux;

namespace BotDofus.Utilitaires.Auto;

/// <summary>
/// Calibration de la projection cellule Dofus → pixel écran (dans la fenêtre
/// du client). Sérialisée dans <c>config/clic-client.json</c> pour être ajustée
/// sans recompiler. Modèle isométrique Dofus Retro :
///   px = OrigineX + (X - Y) * PasX
///   py = OrigineY + (X + Y) * PasY
/// où (X, Y) sont les coordonnées Dofus de la cellule (cf. Cellule.X/Y).
///
/// OrigineX/Y = pixel ÉCRAN du centre de la cellule (X==0, Y==0). PasX/PasY =
/// demi-largeur / demi-hauteur d'une cellule à l'écran. Valeurs par défaut =
/// Dofus Retro 1.x non zoomé ; à affiner en 2 clics via la calibration UI.
/// </summary>
public sealed class CalibrationClic
{
    public double OrigineX { get; set; } = 470;
    public double OrigineY { get; set; } = 95;
    public double PasX { get; set; } = 43;
    public double PasY { get; set; } = 21.5;

    private static string Chemin => Path.Combine("config", "clic-client.json");

    public static CalibrationClic Charger()
    {
        try
        {
            if (File.Exists(Chemin))
                return JsonSerializer.Deserialize<CalibrationClic>(File.ReadAllText(Chemin))
                       ?? new CalibrationClic();
        }
        catch (Exception ex) { Journaliseur.Avertir($"[PILOTE] Calibration illisible : {ex.Message}"); }
        // Premier lancement : on écrit le fichier de défauts pour qu'il soit
        // trouvable et ajustable à la main (config/clic-client.json).
        var def = new CalibrationClic();
        def.Sauver();
        Journaliseur.Info("[PILOTE] Calibration par défaut écrite dans config/clic-client.json "
            + "(ajuste OrigineX/Y + PasX/PasY si le clic tombe à côté).");
        return def;
    }

    public void Sauver()
    {
        try
        {
            Directory.CreateDirectory("config");
            File.WriteAllText(Chemin, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { Journaliseur.Avertir($"[PILOTE] Sauvegarde calibration : {ex.Message}"); }
    }

    /// <summary>Calibre à partir de DEUX cellules dont on connaît le pixel écran réel.</summary>
    public void CalibrerDeuxPoints(int x1, int y1, double px1, double py1,
                                   int x2, int y2, double px2, double py2)
    {
        double du1 = x1 - y1, dv1 = x1 + y1;
        double du2 = x2 - y2, dv2 = x2 + y2;
        if (Math.Abs(du2 - du1) > 0.0001) PasX = (px2 - px1) / (du2 - du1);
        if (Math.Abs(dv2 - dv1) > 0.0001) PasY = (py2 - py1) / (dv2 - dv1);
        OrigineX = px1 - du1 * PasX;
        OrigineY = py1 - dv1 * PasY;
        Sauver();
    }
}

/// <summary>
/// Pilote le VRAI client Dofus par clic synthétique. Indispensable contre Abrak
/// dont les actions de jeu passent par un canal CHIFFRÉ anti-tamper (on ne peut
/// pas injecter le paquet — mais on peut faire cliquer le client, qui chiffre et
/// envoie lui-même). Trouve la fenêtre Dofus, la met au premier plan, déplace le
/// curseur sur la cellule cible et clique gauche.
/// </summary>
public static class PiloteClientDofus
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);
    [DllImport("user32.dll")] private static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string? c, string? n);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr h, StringBuilder s, int n);

    private delegate bool EnumProc(IntPtr h, IntPtr p);
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004,
                       MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;

    /// <summary>Cherche la fenêtre du client Dofus (titre contenant « Dofus »).</summary>
    public static IntPtr TrouverFenetre()
    {
        IntPtr trouve = IntPtr.Zero;
        EnumWindows((h, _) =>
        {
            if (!IsWindowVisible(h)) return true;
            var sb = new StringBuilder(256);
            GetWindowText(h, sb, sb.Capacity);
            var titre = sb.ToString();
            if (titre.IndexOf("Dofus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                trouve = h;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return trouve;
    }

    /// <summary>
    /// Clique (gauche par défaut) sur la cellule de coords Dofus (cellX, cellY)
    /// dans le vrai client. Retourne false si la fenêtre est introuvable.
    /// </summary>
    public static bool CliquerCellule(int cellX, int cellY, CalibrationClic cal, bool droit = false)
    {
        var hwnd = TrouverFenetre();
        if (hwnd == IntPtr.Zero)
        {
            Journaliseur.Avertir("[PILOTE] Fenêtre Dofus introuvable — le client est-il lancé ?");
            return false;
        }

        // Coin écran de la zone client (origine relative à la fenêtre).
        var coin = new POINT { X = 0, Y = 0 };
        ClientToScreen(hwnd, ref coin);

        int sx = coin.X + (int)Math.Round(cal.OrigineX + (cellX - cellY) * cal.PasX);
        int sy = coin.Y + (int)Math.Round(cal.OrigineY + (cellX + cellY) * cal.PasY);

        ShowWindow(hwnd, 9 /* SW_RESTORE */);
        SetForegroundWindow(hwnd);
        System.Threading.Thread.Sleep(60);
        SetCursorPos(sx, sy);
        System.Threading.Thread.Sleep(40);
        mouse_event(droit ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(25);
        mouse_event(droit ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);

        Journaliseur.Info($"[PILOTE] Clic {(droit ? "droit" : "gauche")} cellule ({cellX},{cellY}) → écran ({sx},{sy})");
        return true;
    }
}
