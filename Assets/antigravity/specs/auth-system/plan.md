# Plan de Implementación - Sistema de Autenticación

## Orden de implementación

Paso 1 → Modelos y Repositorios de Base de Datos
Paso 2 → Controladores y Servicios de Backend (Registro)
Paso 3 → Controladores y Servicios de Backend (Login)
Paso 4 → Autenticación en Unity (UI + Peticiones HTTP)

---

## Paso 1 — Modelos y Repositorios

Ficheros: 
- `backend/src/repositories/mongo/UserRepository.js`
- `backend/src/models/User.js`

Tareas:
  1.1 Instalar Mongoose (si no está instalado) para la lógica de base de datos.
  1.2 Crear el modelo `User` en Mongoose con: `username` (string, único, requerido) y `passwordHash` (string, requerido).
  1.3 Implementar `UserRepository.js` con soporte para operaciones: `createUser()`, `findUserByUsername()`, `findUserById()`.

---

## Paso 2 — Endpoint de Registro

Ficheros: 
- `backend/src/controllers/UserController.js`
- `backend/src/services/UserService.js`

Tareas:
  2.1 En el servicio, recibir `username` y `password` en texto plano.
  2.2 Validar que el usuario no exista usando el repositorio.
  2.3 Encriptar el password usando `bcrypt` (salt rounds: 10).
  2.4 Guardar el usuario en la BD.
  2.5 Crear el endpoint `POST /api/auth/register` en el controlador que llame al servicio.

---

## Paso 3 — Endpoint de Login

Ficheros: 
- `backend/src/controllers/UserController.js`
- `backend/src/services/UserService.js`

Tareas:
  3.1 En el servicio, recibir `username` y `password`.
  3.2 Buscar el usuario por nombre en el repositorio.
  3.3 Comparar el password recibido con el `passwordHash` usando `bcrypt`.
  3.4 Si es correcto, generar un JWT (JSON Web Token) firmado que contenga el `userId` y `username`.
  3.5 Crear el endpoint `POST /api/auth/login` que devuelva un 200 OK con el token JWT.

---

## Paso 4 — Autenticación en Unity (Cliente)

Ficheros: 
- `unity-client/Assets/Scripts/Auth/AuthManager.cs`
- `unity-client/Assets/Scripts/UI/LoginUI.cs`

Tareas:
  4.1 Construir la Interfaz de Usuario: 2 inputs (Username, Password) y 2 botones (Login y Register).
  4.2 Implementar `UnityWebRequest` para hacer llamadas POST a `http://localhost:3000/api/auth/login` y `register`.
  4.3 Serializar datos en JSON e incluirlos en el body.
  4.4 Guardar el token JWT recibido en memoria (ej. variable estática `GameSession.Token`).
  4.5 Si el login es exitoso, cambiar la escena de Unity o activar paneles del **Lobby**.
