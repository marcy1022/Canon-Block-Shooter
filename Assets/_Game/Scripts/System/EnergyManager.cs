using SmashPuzzle.Data;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SmashPuzzle.Core
{
    public class EnergyManager : MonoBehaviour
    {
        public static EnergyManager Instance { get; private set; }

        [SerializeField] private GameData gameData;

        public int CurrentEnergy { get; private set; }
        public float RegenTimer { get; private set; }
        public bool HasGameData => gameData != null;
        public bool IsFull => gameData != null && CurrentEnergy >= gameData.MaxEnergy;

        public static readonly UnityEvent<int> OnEnergyChanged = new();
        // IAP 전환 트리거: 에너지 0 상태에서 플레이 시도 시
        public static readonly UnityEvent OnEnergyDepleted = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            RestoreOfflineEnergy();
        }

        // 앱 재진입 시 경과 시간으로 회복량 계산
        public void RestoreOfflineEnergy()
        {
            if (gameData == null) { CurrentEnergy = 5; return; }

            long savedTicks = long.Parse(PlayerPrefs.GetString("EnergyTimestamp", "0"));
            if (savedTicks > 0)
            {
                long nowTicks     = DateTime.UtcNow.Ticks;
                float elapsed     = (nowTicks - savedTicks) / (float)TimeSpan.TicksPerSecond;
                int recovered     = Mathf.FloorToInt(elapsed / gameData.EnergyRegenSeconds);
                int saved         = PlayerPrefs.GetInt("Energy", gameData.MaxEnergy);
                CurrentEnergy     = Mathf.Min(saved + recovered, gameData.MaxEnergy);
            }
            else
            {
                CurrentEnergy = gameData.MaxEnergy;
            }
            SaveEnergy();
        }

        private void Update()
        {
            if (gameData == null || IsFull) return;
            RegenTimer += Time.deltaTime;
            if (RegenTimer >= gameData.EnergyRegenSeconds)
            {
                RegenTimer = 0f;
                AddEnergy(1);
            }
        }

        public int   GetMaxEnergy()             => gameData != null ? gameData.MaxEnergy : 5;
        public float GetRegenSecondsRemaining() => gameData != null ? gameData.EnergyRegenSeconds - RegenTimer : 0f;

        public bool TrySpendEnergy(int amount = 1)
        {
            if (CurrentEnergy < amount)
            {
                OnEnergyDepleted.Invoke(); // → IAP 팝업 트리거
                return false;
            }
            CurrentEnergy -= amount;
            SaveEnergy();
            OnEnergyChanged.Invoke(CurrentEnergy);
            return true;
        }

        public void AddEnergy(int amount)
        {
            int max = gameData != null ? gameData.MaxEnergy : 5;
            CurrentEnergy = Mathf.Min(CurrentEnergy + amount, max);
            SaveEnergy();
            OnEnergyChanged.Invoke(CurrentEnergy);
        }

        // 광고 시청 (Mock): 하루 5회 상한
        public bool WatchAdForEnergy()
        {
            string key   = $"AdWatchCount_{DateTime.UtcNow:yyyyMMdd}";
            int    count = PlayerPrefs.GetInt(key, 0);
            if (count >= 5) return false;
            PlayerPrefs.SetInt(key, count + 1);
            AddEnergy(1);
            return true;
        }

        public void SaveEnergy()
        {
            PlayerPrefs.SetInt("Energy", CurrentEnergy);
            PlayerPrefs.SetString("EnergyTimestamp", DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        private int LoadEnergy() => PlayerPrefs.GetInt("Energy", gameData.MaxEnergy);

        private void OnDestroy()
        {
            OnEnergyChanged.RemoveAllListeners();
            OnEnergyDepleted.RemoveAllListeners();
        }
    }
}
