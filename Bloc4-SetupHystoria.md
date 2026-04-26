# Bloc 4 — Setup Hystoria pour le bot

## Vue d'ensemble

Le client Flash d'Hystoria (`loader.swf`) a un mécanisme **natif** d'override d'IP serveur via le `config.xml`. On exploite ça pour le faire pointer sur notre proxy local au lieu des vrais serveurs Hystoria.

Aucune modification du binaire, aucune injection de DLL, pas besoin de Proxifier.

## Fichiers concernés

- **Source** : `C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml`
- **Backup à faire** : `config.xml.original` (sécurité)
- **Notre exe** : `C:\Users\touki\Desktop\Bot-dofus\.claude\worktrees\vigorous-murdock\temp-clone\bin\Debug\net8.0-windows\BotDofus.exe`

## Topologie cible

```
                                   ┌─────────────────────────────┐
                                   │ Client Hystoria (Flash SWF) │
                                   └──────────────┬──────────────┘
                                                  │
                                                  │ Login.as utilise
                                                  │ _global.CONFIG.connexionServer
                                                  ▼
                                          127.0.0.1:5555
                                                  │
                  ┌───────────────────────────────┴────────────────────────┐
                  │ ProxyReseau auth (notre BotDofus.exe)                   │
                  │ → forward vers 162.19.127.156:450                       │
                  │ → intercepte AYK, réécrit en AYK127.0.0.1:5556;ticket   │
                  └───────────────────────────────┬────────────────────────┘
                                                  │ AYK reçu, le client lit
                                                  │ _loc16_ = getCustomIP(601)
                                                  │ → 127.0.0.1:5556 (override)
                                                  ▼
                                          127.0.0.1:5556
                                                  │
                  ┌───────────────────────────────┴────────────────────────┐
                  │ ProxyReseau jeu (notre BotDofus.exe, démarré dynamiquement)
                  │ → forward vers 162.19.127.155:5555                      │
                  │ → gère cipher CRYPTS/HG via SessionProxy                │
                  └─────────────────────────────────────────────────────────┘
```

## Étapes

### 1. Backup du config.xml original

```cmd
copy "C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml" "C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml.backup"
```

### 2. Remplacer config.xml par la version patchée

Le contenu à mettre :

```xml
<config>

    <delay value="500"/>
    <rdelay value="3000"/>
    <rcount value="10"/>

    <conf name="En ligne (Bot Local)" zaapconnectport="25000">
        <databank id="0">
            <dataserver url="data/" type="local" priority="4"/>
            <dataserver url="http://51.178.36.51/dofus/" priority="3"/>
            <dataserver url="http://51.178.36.51/dofus/" priority="0"/>
        </databank>

        <!-- Override AUTH server : pointe le client sur notre proxy local au lieu de 162.19.127.156:450 -->
        <connserver name="Hystoria-Bot" ip="127.0.0.1" port="5555"/>

        <!-- Override GAME server : quand le serveur 601 (Hystoria) est sélectionné,
             remplace l'IP/port du AYK par notre proxy game local -->
        <servers>
            <server id="601" ip="127.0.0.1" port="5556"/>
        </servers>
    </conf>

    <cacheasbitmap>
        <cache element="ExternalContainer/InteractionCell" value="true"/>
        <cache element="ExternalContainer/Ground" value="true"/>
        <cache element="ExternalContainer/Object1" value="true"/>
        <cache element="ExternalContainer/Object2" value="true"/>
        <cache element="ExternalContainer/Zone" value="true"/>
        <cache element="ExternalContainer/Select" value="false"/>
        <cache element="ExternalContainer/Grid" value="true"/>
        <cache element="ExternalContainer/Pointer" value="true"/>
        <cache element="GAPI/UI" value="false"/>
        <cache element="GAPI/UITop" value="false"/>
        <cache element="GAPI/Popup" value="false"/>
        <cache element="GAPI/UIUltimate" value="false"/>
        <cache element="GAPI/Cursor" value="true"/>
        <cache element="mapHandler/BACKGROUND" value="false"/>
        <cache element="mapHandler/Cell/Ground" value="false"/>
        <cache element="mapHandler/Cell/Object1" value="false"/>
        <cache element="mapHandler/Cell/Object2" value="false"/>
        <cache element="mapHandler/Cell/ObjectExternal" value="false"/>
        <cache element="Zone/Zone" value="true"/>
        <cache element="Zone/Pointers" value="true"/>
        <cache element="Controls/Loader" value="false" />
    </cacheasbitmap>

</config>
```

### 3. Procédure de lancement

**Ordre IMPORTANT** :

1. **Lancer BotDofus.exe en premier** (sinon le client va tomber sur connexion refused)
   ```
   C:\Users\touki\Desktop\Bot-dofus\.claude\worktrees\vigorous-murdock\temp-clone\bin\Debug\net8.0-windows\BotDofus.exe
   ```
   Démarrer un compte. Cocher "ModePassif" pour la première fois (juste observer le trafic, pas tenter de jouer la trame d'auth toi-même).

2. **Lancer le launcher Hystoria normalement**
   - Le launcher démarre `Dofus Retro.exe` (Electron) qui charge le SWF
   - Le SWF lit ton config.xml patché → se connecte sur 127.0.0.1:5555 au lieu de 162.19.127.156:450
   - Notre proxy auth intercepte, forward vers le vrai serveur, voit toute la séquence
   - Quand le AYK arrive, getCustomIP(601) override → client se reconnecte sur 127.0.0.1:5556
   - Notre proxy game démarre dynamiquement, intercepte le handshake CRYPTS/HG, active le cipher
   - Tous les packets in-game sont déchiffrés et émis dans l'événement PaquetRecu

3. **Tu joues**, le bot **observe et log**. Vérifie le contenu de `logs/paquets-*.log` après la session.

### 4. Pour revenir à l'utilisation normale (sans bot)

Restaure le backup :
```cmd
copy "C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml.backup" "C:\Users\touki\AppData\Local\Hystoria\Dofus\resources\app\retroclient\config.xml"
```

## Points à connaître

- **Le launcher Hystoria peut écraser config.xml** lors d'une mise à jour. Garde toujours ton backup.
- **Le mode Modern 64 bits du launcher** lance `Dofus Retro.exe` (Electron wrapper) qui consume le `main.jsc`. Le mode Legacy 32 bits lance directement `Dofus.exe` (Flash standalone). Les deux modes utilisent le même `loader.swf` et le même `config.xml`, donc l'override marche dans les deux cas.
- **Si le launcher refuse de démarrer** parce qu'il détecte le proxy : essaie de désactiver le `zaapconnectport` (mettre une valeur invalide ou supprimer l'attribut). Le client tentera de se connecter à `localhost:25000` pour récupérer les credentials autoconnect mais comme rien ne répond, il continuera sans.
- **Pour valider que ça marche** : démarre BotDofus.exe en mode passif, lance Hystoria. Tu devrais voir dans le log de BotDofus :
  ```
  [ORCH] Proxy auth démarré (127.0.0.1:5555 → 162.19.127.156:450)
  Client Dofus connecté depuis 127.0.0.1:xxxxx
  ...
  [ORCH] AYK intercepté : serveur jeu réel = 162.19.127.155:5555, ticket=...
  [ORCH] Proxy jeu démarré (127.0.0.1:5556 → 162.19.127.155:5555)
  [ORCH] AYK réécrit → AYK127.0.0.1:5556;...
  Client Dofus connecté depuis 127.0.0.1:xxxxx (sur proxy jeu)
  [CIPHER] Demande de handshake CRYPTS détectée (C→S)
  [CIPHER] Handshake confirmé par HG du serveur → cipher ACTIVE
  ```
  Et après le HG, tous les packets seront déchiffrés et lisibles dans `logs/`.

## Si ça plante

Cas typiques :

| Symptôme | Cause probable | Fix |
|---|---|---|
| Client reste bloqué sur "Connecting..." | BotDofus.exe pas lancé ou pas en écoute sur 5555 | Vérifier que BotDofus tourne, regarder `netstat -an \| findstr 5555` |
| `[CIPHER] Decrypt échoué` répété | PSK différente sur Hystoria depuis update | Re-décompiler `loader.swf` (cf. HystoriaCipher.md §2) |
| Client se déconnecte juste après HG | Cipher pas re-encrypté correctement après modification | Vérifier la logique `etaitChiffre` dans SessionProxy.cs |
| `[ORCH] AYK reçu mais format inattendu` | Hystoria a changé le format AYK | Adapter la regex dans OrchestrateurHystoria.cs |
| Port 5555 ou 5556 déjà utilisé | Autre process | Changer dans ConfigReseau.cs et config.xml |
