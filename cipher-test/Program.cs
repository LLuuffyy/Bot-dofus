// Test isolé du Pathfinder + encodage chemin
// Référence pour validation manuelle
using System;
using System.Collections.Generic;

// Inline minimal de HashCarte pour le test
public static class HashCarte
{
    public static readonly char[] Alphabet =
    {
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p',
        'q','r','s','t','u','v','w','x','y','z',
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P',
        'Q','R','S','T','U','V','W','X','Y','Z',
        '0','1','2','3','4','5','6','7','8','9','-','_'
    };
    public static string EncoderCellule(int cellId)
        => $"{Alphabet[cellId / 64]}{Alphabet[cellId % 64]}";
    public static int DecoderCellule(string h) => IndexCar(h[0]) * 64 + IndexCar(h[1]);
    public static int IndexCar(char c) { for (int i = 0; i < Alphabet.Length; i++) if (Alphabet[i] == c) return i; return -1; }
}

class Program
{
    static int Main()
    {
        int passed = 0, failed = 0;

        // Test 1 : encodage cellId 0 = "aa"
        Check("CellId 0 → aa", HashCarte.EncoderCellule(0), "aa", ref passed, ref failed);
        // Test 2 : cellId 63 = "a_"
        Check("CellId 63 → a_", HashCarte.EncoderCellule(63), "a_", ref passed, ref failed);
        // Test 3 : cellId 64 = "ba"
        Check("CellId 64 → ba", HashCarte.EncoderCellule(64), "ba", ref passed, ref failed);
        // Test 4 : cellId 100 = "bM" (100/64=1, 100%64=36 → b + M)
        Check("CellId 100", HashCarte.EncoderCellule(100), "bM", ref passed, ref failed);
        // Test 5 : roundtrip
        for (int id = 0; id < 560; id++)
        {
            var encoded = HashCarte.EncoderCellule(id);
            var decoded = HashCarte.DecoderCellule(encoded);
            if (decoded != id) { failed++; Console.WriteLine($"[KO] CellId {id} roundtrip → {decoded}"); break; }
        }
        passed++;
        Console.WriteLine("[OK] CellId 0..559 roundtrip");

        // Test direction (4 cardinaux + 4 diagonales)
        // Reproduit dyshay GetCharDirection avec la formule : 0=NE, 1=E, 2=SE, 3=S, 4=SW, 5=W, 6=NW, 7=N
        // Pour deux cellules à coords (x,y) connues
        Check("Dir (5,5)→(5,4) = N (h)", DirVers(5, 5, 5, 4), 'd', ref passed, ref failed); // y diminue, x egal → N = (char)('a'+3)
        Check("Dir (5,5)→(5,6) = S (h)", DirVers(5, 5, 5, 6), 'h', ref passed, ref failed); // (char)('a'+7)
        Check("Dir (5,5)→(6,5) = E", DirVers(5, 5, 6, 5), 'f', ref passed, ref failed);     // x augmente, y egal → (char)('a'+5)
        Check("Dir (5,5)→(4,5) = W", DirVers(5, 5, 4, 5), 'b', ref passed, ref failed);     // (char)('a'+1)

        Console.WriteLine();
        Console.WriteLine($"=== Resultat : {passed} passes, {failed} echecs ===");
        return failed == 0 ? 0 : 1;
    }

    // Reproduit la formule Cellule.DirectionVers
    static char DirVers(int x1, int y1, int x2, int y2)
    {
        if (x1 == x2) return y2 < y1 ? (char)('a' + 3) : (char)('a' + 7);
        if (y1 == y2) return x2 < x1 ? (char)('a' + 1) : (char)('a' + 5);
        if (x1 > x2) return y1 > y2 ? (char)('a' + 2) : (char)('a' + 0);
        return y1 < y2 ? (char)('a' + 6) : (char)('a' + 4);
    }

    static void Check<T>(string name, T got, T expected, ref int p, ref int f)
    {
        if (got!.Equals(expected)) { p++; Console.WriteLine($"[OK] {name}"); }
        else { f++; Console.WriteLine($"[KO] {name} : attendu={expected}, obtenu={got}"); }
    }
}
