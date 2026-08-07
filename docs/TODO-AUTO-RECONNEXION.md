# TODO — Auto-reconnexion

**Statut** : Pas implémenté (P1, ~3-4h d'effort, risque moyen)

## Contexte

Quand le serveur kick le bot (déconnexion réseau, timeout, ban) ou que le
client Dofus crashe, le bot reste actif mais sans session jeu. L'user doit
relancer Dofus manuellement.

## Idée d'implémentation

1. Écouter `SessionProxy.SessionTerminee` (déjà fait pour Discord alerte)
2. Si l'Etat compte était `EnJeu`/`EnCombat` :
   - Attendre un délai (backoff exponentiel : 5s, 15s, 60s)
   - Vérifier si le process Dofus.exe est mort → si oui, relancer
   - Re-déclencher le flow d'auth via `aks_identity.txt` capturé
3. Limites :
   - Si ban → relancer Dofus ne va pas régler le problème, ça va juste log
     un autre kick
   - Si captcha → impossible de bypass
   - Si maintenance serveur → attendre

## Risques

- Boucle infinie de re-co si le ban est permanent
- Sur Hystoria, plusieurs reconnexions en rafale = signal anti-bot fort
- Captcha aléatoires (CAPTCHA Hystoria activé)

## Recommandation

V1 minimale = juste **détecter la déconnexion + alerte Discord**, laisser
l'user décider. C'est ce qui est fait actuellement (cf. commit `c0ffee`).
Pour aller plus loin, implémenter avec :
- Max 3 tentatives par session
- Backoff 30s / 2min / 5min
- Désactivation totale après 3 échecs

## Référence

`Divers/ContexteCompte.cs:OnSessionJeuDemarree` — handler SessionTerminee
déjà branché pour alerte Discord. Étendre ce handler pour orchestrer la
reconnexion.
