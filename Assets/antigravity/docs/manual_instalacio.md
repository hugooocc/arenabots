# 📖 Manual d'Instal·lació i Configuració - Arena Bots

Hola! En aquesta guia t'explico com posar en marxa tot el sistema d'Arena Bots. He intentat que sigui el més senzill possible per no tenir problemes amb dependències.

## 1. Requisits Previs
Abans de començar, assegura't de tenir instal·lat:
- **Docker** i **Docker Compose** (imprescindible per al backend).
- **Unity Hub** i versió de Unity 2022.3 o superior.
- **Git** per gestionar el codi.

## 2. Preparació del Backend (Microserveis)
Hem passat d'un sistema monolític a un de microserveis per fer-lo més escalable i robust. No et preocupis, que posar-ho en marxa és igual de fàcil:

1. Obre una terminal a la carpeta `Assets/antigravity/backend`.
2. Executa la següent comanda per aixecar tots els serveis (Gateway, Auth, Game, Stats i Base de dades):
   ```bash
   docker compose up --build
   ```
3. El sistema estarà llest quan vegis que el `arena-gateway` està escoltant al port **3000**. Tot el trànsit de Unity anirà centralitzat per aquí.

## 3. Configuració del Client Unity
No cal que canviïs res de codi si ja el tens configurat per defecte, però aquí tens els punts clau:

1. Obre el projecte amb Unity.
2. Ves a la carpeta `Assets/antigravity/unity-client`.
3. Obre la escena `Login` (és la porta d'entrada al joc).
4. Revisa que el `NetworkManager` apunti a `http://localhost:3000`. Com que hem fet un Gateway, no cal que sàpigues els ports interns dels microserveis.

## 4. Estructura de la Base de Dades
Utilitzem **MongoDB**. En aixecar el Docker, s'inicialitza automàticament. Si vols fer neteja total i començar de zero, pots esborrar el volum de dades amb:
```bash
docker compose down -v
```

## 5. Resolució de Problemes
- **No es connecta al servidor?** Revisa que el Docker estigui corrent correctament i que no tinguis un altre servei ocupant el port 3000.
- **Problemes amb el login?** Recorda que per jugar en multijugador cal estar registrat i haver fet login primer per obtenir el token de seguretat.

Espero que et serveixi de gran ajuda per l'entrega! Si tens qualsevol dubte, el codi està comentat per facilitar la comprensió.
