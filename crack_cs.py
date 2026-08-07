#!/usr/bin/env python3
# Crack OFFLINE de la calibration cipher '-' sens CLIENT -> SERVEUR (Abrak).
# Donnees : 16 cles AK (constantes) + 4 paquets C->S courts captures en clair.
# But : trouver (mapping index-cle, formule offset) tel que le checksum
# recalcule du clair == le char cks du frame, pour les 4 paquets a la fois.

HEX = "0123456789ABCDEF"

# AK brut (apres "AK"), tokens separes par '|'. CanalAbrak: si longueur
# impaire on retire le 1er char (marqueur d'index 0..F).
AK = ("014a68af34955cb816ace24fcae5ead72|13b7ec856f1594dd873ca716d3e5bca63|"
      "2e19cb42a8fe66accb8198ab9cbc47f23|3c5469f63865ea418b7f28d7e4a7bce7f|"
      "49b58ec512d74298d1e7d725585c9dc7f|5f48ee2b631aff4213c131dc375b2f7be|"
      "6b58cd13d4791bdf3e3e9f797e769ea29|77f983f1e82d5fe7dc923f3b7bc47c3ea|"
      "879f4ec323d73666ebf872d9857c4d39f|97a356db8c5812c2ddd8cbcb7fca5c68d|"
      "Aa3f1a9db1ea49246e8ee5985cb15c216|Bafccd6cf422a422f6d81464a26a34824|"
      "Ced5ec3c47a2f75be7a26d415a738f119|D4d78ae7ddefe76adac77666eea5e67d7|"
      "E9ee6c24aeca99a384bda71adebbde797|Fb6a26efc978ec3a2fc3bb61994bbd829")

# Paquets C->S COMPLETS (non tronques) captures : (idxFrame, cksChar, payloadHex)
PKTS = [
    (2, '6', "8980"),
    (3, '6', "083F"),
    (5, 'D', "5A883E82"),
    (7, 'D', "B4FCF777"),
]

def unescape_flash(s):
    out = []
    i = 0
    while i < len(s):
        c = s[i]
        if c == '%' and i + 2 < len(s):
            hi = HEX.find(s[i+1].upper())
            lo = HEX.find(s[i+2].upper())
            if hi >= 0 and lo >= 0:
                out.append(chr((hi << 4) | lo))
                i += 3
                continue
        out.append(c)
        i += 1
    return "".join(out)

def prepare_key(token_hex):
    if len(token_hex) % 2 != 0:
        return None
    b = bytes.fromhex(token_hex)
    s = "".join(chr(x) for x in b)          # latin-1
    return unescape_flash(s)

def checksum(s):
    return HEX[sum(ord(c) % 16 for c in s) % 16]

# Prepare les 16 cles (avec retrait du 1er char si longueur impaire)
tokens = AK.split('|')
keys = []
for t in tokens:
    if len(t) % 2 != 0 and len(t) > 1:
        t = t[1:]
    keys.append(prepare_key(t))

def decrypt(payload_hex, key, offset):
    data = bytes.fromhex(payload_hex)
    out = []
    L = len(key)
    for i, v in enumerate(data):
        k = ord(key[(i + offset) % L])
        out.append(chr(v ^ k))
    return unescape_flash("".join(out))

# Formules d'offset candidates (depend de cks et/ou idx)
def offsets(cks_val, idx):
    return {
        "cks*2": cks_val * 2,
        "0": 0,
        "cks": cks_val,
        "cks*2+1": cks_val * 2 + 1,
        "cks*2-1": (cks_val * 2 - 1) % 9999,
        "idx*2": idx * 2,
        "idx": idx,
        "cks*2+idx": cks_val * 2 + idx,
    }

# On cherche un (delta cle, nom offset) qui valide le checksum sur TOUS les paquets
solutions = []
for delta in range(16):
    for off_name in ["cks*2", "0", "cks", "cks*2+1", "cks*2-1", "idx*2", "idx", "cks*2+idx"]:
        ok = True
        decoded = []
        for (idx, cks, pay) in PKTS:
            ki = (idx + delta) % 16
            key = keys[ki]
            if not key:
                ok = False
                break
            cks_val = HEX.find(cks)
            off = offsets(cks_val, idx)[off_name]
            if off < 0:
                ok = False
                break
            clair = decrypt(pay, key, off)
            decoded.append(clair)
            if checksum(clair) != cks:
                ok = False
                break
        if ok:
            solutions.append((delta, off_name, decoded))

print("=== Cles preparees (longueurs) ===")
print([len(k) if k else None for k in keys])
print()
if solutions:
    print("=== SOLUTIONS (checksum valide sur les 4 paquets) ===")
    for d, o, dec in solutions:
        print(f"deltaCle={d}  offset={o}  clairs={dec!r}")
else:
    print("Aucune solution stricte. Dump par paquet (toutes cles, offset cks*2) :")
    for (idx, cks, pay) in PKTS:
        print(f"\n--- paquet idx={idx} cks={cks} pay={pay} ---")
        cks_val = HEX.find(cks)
        for ki in range(16):
            key = keys[ki]
            if not key:
                continue
            for off_name, off in offsets(cks_val, idx).items():
                if off < 0:
                    continue
                clair = decrypt(pay, key, off)
                printable = all(32 <= ord(c) <= 126 for c in clair)
                mark = "  <== checksum OK" if checksum(clair) == cks else ""
                if printable or mark:
                    print(f"  cle[{ki:2}] off={off_name:9} -> {clair!r}{mark}")
