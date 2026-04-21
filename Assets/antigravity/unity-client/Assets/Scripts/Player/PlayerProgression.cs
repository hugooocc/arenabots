using UnityEngine;
using System;

namespace Antigravity.Player
{
    public class PlayerProgression : MonoBehaviour
    {
        [Header("Progression Status")]
        public int currentLevel = 1;
        public int currentXP = 0;
        public int xpToNextLevel = 100;

        public event Action<int, int> OnLevelUp; // Level, MaxXP
        public event Action<int, int> OnXPChanged; // CurrentXP, MaxXP

        public static PlayerProgression Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddExperience(int xpAmount)
        {
            currentXP += xpAmount;
            
            while (currentXP >= xpToNextLevel)
            {
                LevelUp();
            }

            OnXPChanged?.Invoke(currentXP, xpToNextLevel);
            Debug.Log($"[Progression] XP Gained: {xpAmount}. Current XP: {currentXP}/{xpToNextLevel} (Level {currentLevel})");
        }

        private void LevelUp()
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f);

            OnLevelUp?.Invoke(currentLevel, xpToNextLevel);
            Debug.Log($"[Progression] ¡LEVEL UP! Ahora eres nivel {currentLevel}!");
            
            // Aquí puedes añadir llamadas a curar al jugador o mejorar atributos
        }
    }
}
