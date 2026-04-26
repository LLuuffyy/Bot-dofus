# HystoriaCipher — Spécification complète

Reverse depuis `loader.swf` du client Hystoria, classe `dofus.aks.Aks`
(fichier décompilé : `swf-decompiled/scripts/__Packages/dofus/aks/Aks.as`).

---

## 1. Vue d'ensemble

Hystoria utilise un cipher custom **XOR-stream avec keystream dérivé** (compteur séquentiel + clé partagée 32 bytes). Activé via un handshake `CRYPTS\n` côté client. Tous les packets game sont encapsulés dans une enveloppe `CRYPTS<seq><base64>`.

- **Type** : XOR stream cipher (PAS AES, PAS ChaCha)
- **Force cryptographique** : faible (vulnérable à known-plaintext, predictable)
- **Force pratique** : suffisante pour bloquer un sniffer naïf
- **Clé** : pré-partagée, hardcodée dans le client (récupérable)

---

## 2. Clé pré-partagée (PSK)

Référence : `Aks.as:70`

```
PSK_HEX = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
```

→ **32 octets**, valeur en hex :
```
01 23 45 67 89 ab cd ef
01 23 45 67 89 ab cd ef
01 23 45 67 89 ab cd ef
01 23 45 67 89 ab cd ef
```

Codée en dur dans le client SWF. Le serveur Hystoria utilise la même.

⚠️ Si Hystoria patche cette valeur dans une mise à jour, il faudra re-décompiler `loader.swf` pour récupérer la nouvelle.

---

## 3. État machine du cipher

Référence : `Aks.as:71-76, 906-918`

| État | Valeur | Signification |
|---|---|---|
| OFF | 0 | Pas de chiffrement, paquets en clair |
| HANDSHAKING | 1 | Handshake en cours, paquets bufferisés |
| ACTIVE | 2 | Chiffrement actif, encrypt + decrypt |
| FAILED | 3 | Handshake refusé, mode clair forcé |

Variables d'instance :
- `_cryptoSessionKey` : la PSK (bytes[32])
- `_cryptoSendSeq` : compteur d'envoi (uint, incrémenté à chaque encrypt)
- `_cryptoRecvSeq` : compteur de réception (uint, mis à jour à chaque decrypt)
- `_cryptoState` : état machine ci-dessus

---

## 4. Handshake

Référence : `Aks.as:1010-1014, 583-625, 957-1001`

```
1. Client → Server : "CRYPTS\n"
2. Server → Client : "HG..." (paquet HG en clair)
3. À partir de ce moment, _cryptoState = 2 (ACTIVE)
4. Tous les sends/recvs passent par encrypt/decrypt
```

Variantes serveur acceptées :
- `CRYPTS` (8 chars max) → ack handshake (cas standard)
- `CRYPTOK` → ack alternatif
- `CRYPTFAIL` → refus, retour en clair (state = 3)

---

## 5. Format wire

```
"CRYPTS" + <8_chars_hex_seq> + <base64_du_ciphertext>
```

- 6 octets ASCII : préfixe `CRYPTS`
- 8 octets ASCII : sequence number en hex (uppercase = lowercase tolérés au decrypt)
- N octets ASCII : ciphertext encodé en base64

Taille minimum d'un paquet chiffré : 14 octets (6 + 8 + 0).
Si `len < 15` au decrypt → packet rejeté (`Aks.as:1072`).

---

## 6. Algorithme d'ENCRYPT

Référence : `Aks.as:1043-1069`

```pseudo
function encrypt(plaintext_string):
    seq      = sendSeq++              // entier 32 bits, monotone
    seq_low  = seq & 0xFF
    seq_high = (seq >> 8) & 0xFF

    pt_bytes = utf8_encode(plaintext_string)
    key      = sessionKey              // 32 bytes
    ct_bytes = []

    for i in [0 .. len(pt_bytes) - 1]:
        ks = key[(i + seq) % 32]
           ^ seq_low
           ^ seq_high
           ^ (i & 0xFF)
        ct_bytes.append( pt_bytes[i] ^ ks )

    seq_hex = format(seq, '08x')       // 8 chars hex, big-endian, lowercase
    return "CRYPTS" + seq_hex + base64_encode(ct_bytes)
```

**Points d'attention** :
- `seq` est un entier 32 bits qui ne wrappe que théoriquement (pratiquement jamais atteint)
- `seq_hex` utilise les chars `"0123456789abcdef"` (lowercase, big-endian, MSB first)
- L'UTF-8 encoding est manuel (`Aks.as:1128-1153`) — `< 128` = 1 byte, `< 2048` = 2 bytes (`0xC0|hi, 0x80|lo`), sinon 3 bytes

---

## 7. Algorithme de DECRYPT

Référence : `Aks.as:1070-1126`

```pseudo
function decrypt(message_string):
    if len(message) < 15: return null

    // Le préfixe "CRYPTS" est déjà strippé (Aks.as:999-1001 fait substr(6))
    // ou on considère que message commence par "CRYPTS"
    seq_hex_part = message[6:14]      // 8 chars hex
    base64_part  = message[14:]

    // Parser le seq depuis hex (Aks.as:1082-1099)
    seq = 0
    for c in seq_hex_part:
        if   '0' <= c <= '9': digit = ord(c) - 48
        elif 'a' <= c <= 'f': digit = ord(c) - 87
        elif 'A' <= c <= 'F': digit = ord(c) - 55
        else: digit = 0
        seq = seq * 16 + digit

    // Anti-replay
    if seq < recvSeq - 100: return null
    if seq >= recvSeq: recvSeq = seq + 1

    ct_bytes = base64_decode(base64_part)
    if ct_bytes == null or len(ct_bytes) == 0: return null

    seq_low  = seq & 0xFF
    seq_high = (seq >> 8) & 0xFF
    key      = sessionKey
    pt_bytes = []

    for i in [0 .. len(ct_bytes) - 1]:
        ks = key[(i + seq) % 32]
           ^ seq_low
           ^ seq_high
           ^ (i & 0xFF)
        pt_bytes.append( ct_bytes[i] ^ ks )

    return utf8_decode(pt_bytes)
```

L'algo est **strictement symétrique** car XOR (encrypt(decrypt(x)) == x).

---

## 8. Anti-replay

Référence : `Aks.as:1101-1108`

Fenêtre glissante de **100 packets** :
- Si `seq < recvSeq - 100` → packet rejeté (trop vieux)
- Si `seq >= recvSeq` → `recvSeq = seq + 1` (avancement monotone)
- Sinon : packet accepté mais `recvSeq` non mis à jour (autorise le réordonnancement local)

→ Pour un MITM, tant que tu **réémets dans l'ordre**, tu n'as pas besoin de gérer ça.

---

## 9. Cas particuliers à gérer

| Préfixe paquet | Traitement |
|---|---|
| `CRYPTS<seq><b64>` | Format chiffré standard, decrypt → game packet en clair |
| `CRYPTS\n` | Demande de handshake du client |
| `CRYPTOK` | Ack handshake (variante) |
| `CRYPTFAIL` | Échec handshake → mode clair |
| `CRYPTE<b64>` | Format alternatif sans seq (`Aks.as:999-1001`), `cryptoDecrypt(substr(6))` |
| Tout autre | Paquet en clair (état avant handshake ou packet HG/heartbeat) |

---

## 10. Dépendances helper

Référence : `Aks.as:1015-1041, 1128-1271`

- `cryptoHexToBytes(hex)` : "0123abcd" → [0x01, 0x23, 0xab, 0xcd]
- `cryptoBytesToHex(bytes)` : inverse
- `cryptoStringToBytes(s)` : UTF-8 manuel (cf. section 6)
- `cryptoBytesToString(bytes)` : UTF-8 decode
- `cryptoBase64Encode(bytes)` : base64 standard (alphabet `A-Za-z0-9+/`, padding `=`)
- `cryptoBase64Decode(str)` : inverse

⚠️ Vérifier que le base64 utilise bien `+/` et pas `-_` (URL-safe) lors de l'implémentation.

---

## 11. Test vector (à valider après implémentation)

Une fois ton encrypt() codé, tu peux le valider avec ce test :

```
PSK = répétition 4× de [0x01,0x23,0x45,0x67,0x89,0xAB,0xCD,0xEF]
seq = 0
plaintext = "AT7504"   ← le packet AT qu'on a vu dans ton log

Compute keystream pour chaque byte i = 0..5 :
   ks[i] = PSK[i % 32] ^ 0x00 ^ 0x00 ^ (i & 0xFF)
   ks[0] = 0x01 ^ 0 ^ 0 ^ 0 = 0x01
   ks[1] = 0x23 ^ 0 ^ 0 ^ 1 = 0x22
   ks[2] = 0x45 ^ 0 ^ 0 ^ 2 = 0x47
   ks[3] = 0x67 ^ 0 ^ 0 ^ 3 = 0x64
   ks[4] = 0x89 ^ 0 ^ 0 ^ 4 = 0x8D
   ks[5] = 0xAB ^ 0 ^ 0 ^ 5 = 0xAE

plaintext bytes = [0x41, 0x54, 0x37, 0x35, 0x30, 0x34]   ← "AT7504"
ciphertext     = [0x40, 0x76, 0x70, 0x51, 0xBD, 0x9A]   ← XOR
base64         = "QHZwUb2a"

Résultat final : "CRYPTS00000000QHZwUb2a"
```

Si ton encrypt() produit exactement ça, c'est bon.

---

## 12. Pièges connus

1. **L'opérateur `^` AS3 a précédence faible** — d'où les parenthèses subtiles dans `key[(i + seq) % 32] ^ seqLow ^ seqHigh ^ (i & 0xFF)`. En implémentant, **forcer la précédence** : `((((key[(i+seq)%32]) ^ seqLow) ^ seqHigh) ^ (i & 0xFF))` ou utiliser un langage où `^` est associatif gauche.
2. **`(i & 0xFF)` est crucial** — sans le mask, sur des messages > 256 bytes, `i` déborde et le keystream diverge.
3. **`seq` ne reset jamais** sauf via `cryptoReset()` (à chaque nouvelle session login).
4. **L'enveloppe `CRYPTS` est INTÉGRÉE** au flux — pas un cadre TCP séparé. Faut découper sur `\0` (terminator standard 1.29) AVANT de tester si le packet commence par `CRYPTS`.
