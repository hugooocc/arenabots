# 🗄️ Inicialització de la Base de Dades (NoSQL)

Tot i que utilitzem MongoDB (una base de dades no relacional), aquí tens l'estructura i els passos per inicialitzar el sistema amb algunes dades de prova.

## 1. Esquema de Col·leccions

### Col·lecció: `users`
Conté la informació dels jugadors i les seves estadístiques globals.
```json
{
  "_id": "ObjectId",
  "username": "nom_usuari",
  "passwordHash": "contrasenya_encriptada",
  "maxMobsKilled": 0,
  "maxTimeSurvived": 0,
  "createdAt": "ISODate"
}
```

### Col·lecció: `games`
Conté l'estat de les sales de joc.
```json
{
  "_id": "ObjectId",
  "name": "Nom de la Sala",
  "status": "WAITING | IN_PROGRESS | FINISHED",
  "players": ["ObjectId(User)"],
  "maxPlayers": 4,
  "password": "codi_privat_opcional",
  "createdAt": "ISODate"
}
```

## 2. Script d'Inicialització (Opcional)
Si vols carregar un usuari de prova manualment des de la terminal de MongoDB:

```javascript
use arena_bots;

db.users.insertOne({
  username: "player_test",
  passwordHash: "$2b$10$...", // bcrypt hash
  maxMobsKilled: 10,
  maxTimeSurvived: 300,
  createdAt: new Date()
});

db.games.updateMany({}, { $set: { status: "FINISHED" } });
```

> [!NOTE]
> El sistema està configurat per "netejar" les sales actives en iniciar-se, marcant totes les partides antigues com a `FINISHED` per evitar conflictes al lobby.
