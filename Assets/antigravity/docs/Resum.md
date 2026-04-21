# Arena Bots - DAMTR3 HUGO CORDOBA (Hugo)

---
**Curs:** 2DAM 2025-2026  
**Projecte:** Arena Bots  
**Integrant:** Hugo (Grup X)  
**Arquitectura:** Microserveis en Node.js + Client Unity 2D  
**Vídeo:** [Enllaç al vídeo de Canva](https://canva.com/...)  
---

### Objectiu del Projecte
Arena Bots és un shooter 2D multijugador PvE on els jugadors han de sobreviure a onades d'enemics cibernètics en un domini desèrtic. L'objectiu és oferir una experiència de joc cooperativa amb sincronització en temps real i persistència d'estadístiques.

### Arquitectura Tècnica
El projecte s'ha dissenyat seguint una arquitectura de **microserveis** per garantir la separació de responsabilitats i facilitar l'escalabilitat:

1. **API Gateway**: Actua com a proxy invers (Port 3000), centralitzant totes les peticions de Unity i redirigint-les al servei intern corresponent.
2. **Auth Service**: Gestiona el registre, login i autenticació mitjançant JWT i bcrypt.
3. **Game Service**: S'encarrega de la lògica de les sales (lobby) i la comunicació en temps real via WebSockets (posicions, trets, impactes).
4. **Stats Service**: Gestiona la persistència de les millors puntuacions dels jugadors a MongoDB.

### Patró Repository
S'ha aplicat el patró **Repository** per aïllar la lògica de negoci de la persistència. Disposem de dues implementacions:
- `MongoRepository`: Per a producció amb MongoDB.
- `InMemoryRepository`: Per a l'execució de tests unitaris veloços.

### IA i ML-Agents
S'ha integrat un agent d'intel·ligència artificial (ML-Agents) que aprèn a moure's per l'escenari per perseguir els jugadors, augmentant el repte de la partida de forma dinàmica.
