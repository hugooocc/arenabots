using UnityEngine;
using System;
using System.Collections.Generic;

namespace Antigravity.Network
{
    [Serializable]
    public class Vector2Payload
    {
        public float x;
        public float y;

        public Vector2Payload(Vector2 v)
        {
            x = (float)Math.Round(v.x, 2);
            y = (float)Math.Round(v.y, 2);
        }
    }

    [Serializable]
    public class PlayerData
    {
        public string userId;
        public string username;
    }

    [Serializable]
    public class PlayerListMessage
    {
        public string tipo;
        public PlayerData[] jugadores;
    }

    [Serializable]
    public class MoveMessage
    {
        public string tipo;
        public string userId;
        public Vector2Payload posicion;
        public Vector2Payload velocidad;
        public Vector2Payload mirando;
    }

    [Serializable]
    public class ImpactPayload
    {
        public string tipo = "impacto_proyectil";
        public string partidaId;
        public string enemigoId;
        public int dano;
    }

    [Serializable]
    public class RetransmissionPayload
    {
        public string tipo;
        public string proyectilId;
        public string jugadorId;
        public Vector2Payload posicion;
        public Vector2Payload direccion;
        public long timestamp;
    }

    [Serializable]
    public class PlayerStatsData
    {
        public string userId;
        public string username;
        public int kills;
        public int time;
    }

    [Serializable]
    public class GameOverMessage
    {
        public string tipo;
        public List<PlayerStatsData> stats;
    }
}
