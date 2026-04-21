# 🤖 Anàlisi de l'Ús de la IA - Arena Bots

Aquest document detalla com s'ha utilitzat la Intel·ligència Artificial (IA) per al desenvolupament d'una funcionalitat específica d'Arena Bots seguint la metodologia **Spec-Driven Development (SDD)**.

## 1. Funcionalitat Escollida
S'ha escollit el **Sistema de Sincronització de Trets i Impactes** per ser la part més crítica del multijugador.

## 2. Procés Seguit
Hem utilitzat la metodologia OpenSpec:
1. **Definició**: Es van crear els fitxers `foundations.md`, `spec.md` i `plan.md` abans d'escriure ni una sola línia de codi.
2. **Implementació**: L'agent d'IA va generar el codi del `shootHandler.js` i la lògica de sincronització a Unity basant-se estrictament en les especificacions.
3. **Refinament**: Durant la implementació, es van detectar retards en la sincronització (lags) que es van corregir mitjançant prompts de refinament per implementar una validació preventiva al client.

## 3. Reflexió Crítica
- **L'agent ha seguit l'especificació?** Sí, en un 90%. Al principi va intentar simplificar la física utilitzant Raycasts en lloc de projectils físics, però es va rectificar seguint el `spec.md`.
- **Iteracions necessàries:** Unes 4 iteracions per funcionalitat. La majoria de correccions van ser per ajustar la coherència entre el que passava al servidor i el que veien els clients.
- **Punts febles de la IA:** La coherència temporal (timing). La IA tendeix a generar solucions que funcionen bé en local però no tenen en compte la latència real de xarxa si no se li especifica clarament.
- **Modificacions:** He hagut de polir els prompts per forçar que la IA comprovés el `GameId` en cada missatge per evitar barreges de dades entre diferents sales.

## 4. Traçabilitat
Tots els prompts i versions intermitges es troben documentats al fitxer `/specs/shooting-system/prompt-logs.md`.
