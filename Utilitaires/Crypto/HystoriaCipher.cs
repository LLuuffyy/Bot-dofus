using System;
using System.Text;

namespace BotDofus.Utilitaires.Crypto;

/// <summary>
/// Implements Hystoria's custom CRYPTS envelope.
/// </summary>
public sealed class HystoriaCipher
{
    public const string PskHex = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private readonly byte[] _key;

    public uint SendSeq { get; private set; }

    public uint RecvSeq { get; private set; }

    public HystoriaCipher()
    {
        _key = HexToBytes(PskHex);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException($"La PSK Hystoria doit faire 32 octets, pas {_key.Length}.");
        }
    }

    public void Reset()
    {
        SendSeq = 0;
        RecvSeq = 0;
    }

    public static bool IsCryptsPacket(string packet)
    {
        if (packet.Length < 15 || !packet.StartsWith("CRYPTS", StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = 6; i < 14; i++)
        {
            if (HexValue(packet[i]) < 0)
            {
                return false;
            }
        }

        return true;
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var seq = SendSeq++;
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = Transform(plaintextBytes, seq);

        return "CRYPTS" + seq.ToString("x8") + Convert.ToBase64String(ciphertext);
    }

    public string? Decrypt(string envelope)
    {
        if (!IsCryptsPacket(envelope))
        {
            return null;
        }

        var seqHex = envelope.Substring(6, 8);
        var base64 = envelope[14..];

        var seq = 0u;
        for (var i = 0; i < seqHex.Length; i++)
        {
            var digit = HexValue(seqHex[i]);
            if (digit < 0)
            {
                return null;
            }

            seq = (seq * 16u) + (uint)digit;
        }

        if (RecvSeq > 100 && seq < RecvSeq - 100)
        {
            return null;
        }

        if (seq >= RecvSeq)
        {
            RecvSeq = seq + 1;
        }

        byte[] ciphertext;
        try
        {
            ciphertext = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }

        if (ciphertext.Length == 0)
        {
            return null;
        }

        var plaintext = Transform(ciphertext, seq);
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] Transform(byte[] input, uint seq)
    {
        var seqLow = (byte)(seq & 0xFF);
        var seqHigh = (byte)((seq >> 8) & 0xFF);
        var output = new byte[input.Length];

        for (var i = 0; i < input.Length; i++)
        {
            var keyIndex = (int)((i + seq) % (uint)_key.Length);
            var keyStream = (byte)(_key[keyIndex] ^ seqLow ^ seqHigh ^ (i & 0xFF));
            output[i] = (byte)(input[i] ^ keyStream);
        }

        return output;
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException("Longueur hex impaire.", nameof(hex));
        }

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var hi = HexValue(hex[i * 2]);
            var lo = HexValue(hex[(i * 2) + 1]);
            if (hi < 0 || lo < 0)
            {
                throw new ArgumentException($"Caractere hex invalide a l'offset {i * 2}.", nameof(hex));
            }

            bytes[i] = (byte)((hi << 4) | lo);
        }

        return bytes;
    }

    private static int HexValue(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'a' && c <= 'f')
        {
            return c - 'a' + 10;
        }

        if (c >= 'A' && c <= 'F')
        {
            return c - 'A' + 10;
        }

        return -1;
    }
}
