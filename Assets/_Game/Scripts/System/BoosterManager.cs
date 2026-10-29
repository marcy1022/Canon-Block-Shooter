using SmashPuzzle.Level;
using UnityEngine;
using UnityEngine.Events;

namespace SmashPuzzle.Core
{
    // 볼 추가 (+2) 부스터 1종 — 150코인, 레벨당 최대 3회
    public class BoosterManager : MonoBehaviour
    {
        public static BoosterManager Instance { get; private set; }

        private const int BoosterCost       = 150;
        private const int BallsAdded        = 2;
        private const int MaxUsesPerLevel   = 3;
        private const float Cooldown        = 0.5f;

        public static readonly UnityEvent<int> OnBoosterUsed = new();

        private int   _usesThisLevel;
        private float _cooldownTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ResetForNewLevel() => _usesThisLevel = 0;

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        }

        public bool TryUseBallBooster()
        {
            if (_cooldownTimer > 0f)           return false;
            if (_usesThisLevel >= MaxUsesPerLevel) return false;

            var coins = CoinManager.Instance;
            if (coins == null || !coins.SpendCoins(BoosterCost)) return false;

            _usesThisLevel++;
            _cooldownTimer = Cooldown;

            LevelManager.Instance?.AddExtraProjectiles(BallsAdded);
            OnBoosterUsed.Invoke(_usesThisLevel);
            Debug.Log($"[Booster] Ball +{BallsAdded} used (use {_usesThisLevel}/{MaxUsesPerLevel})");
            return true;
        }

        private void OnDestroy() => OnBoosterUsed.RemoveAllListeners();
    }
}
