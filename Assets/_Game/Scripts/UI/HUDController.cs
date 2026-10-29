using SmashPuzzle.Core;
using SmashPuzzle.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Energy")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI energyTimerText;

        [Header("Level Info")]
        [SerializeField] private TextMeshProUGUI levelNumberText;

        [Header("Target Progress")]
        [SerializeField] private Slider   targetProgressBar;
        [SerializeField] private TextMeshProUGUI targetText;

        [Header("Shots")]
        [SerializeField] private TextMeshProUGUI shotsText;

        [Header("Coins")]
        [SerializeField] private TextMeshProUGUI coinsText;

        private int _totalTargets;
        private int _destroyedTargets;

        private void OnEnable()
        {
            LevelManager.OnShotFired.AddListener(UpdateShots);
            EnergyManager.OnEnergyChanged.AddListener(UpdateEnergy);
            CoinManager.OnCoinsChanged.AddListener(UpdateCoins);
            GameManager.OnLevelLoaded.AddListener(OnLevelLoaded);
        }

        private void OnDisable()
        {
            LevelManager.OnShotFired.RemoveListener(UpdateShots);
            EnergyManager.OnEnergyChanged.RemoveListener(UpdateEnergy);
            CoinManager.OnCoinsChanged.RemoveListener(UpdateCoins);
            GameManager.OnLevelLoaded.RemoveListener(OnLevelLoaded);
        }

        private void Start()
        {
            RefreshAll();
        }

        private void Update()
        {
            UpdateEnergyTimer();
        }

        private void RefreshAll()
        {
            if (EnergyManager.Instance != null)
                UpdateEnergy(EnergyManager.Instance.CurrentEnergy);

            if (CoinManager.Instance != null)
                UpdateCoins(CoinManager.Instance.Balance);

            if (LevelManager.Instance != null)
                UpdateShots(LevelManager.Instance.ShotsRemaining);

            if (GameManager.Instance != null && levelNumberText != null)
                levelNumberText.text = $"LEVEL {GameManager.Instance.CurrentLevel}";
        }

        private void OnLevelLoaded(int level)
        {
            if (levelNumberText != null)
                levelNumberText.text = $"LEVEL {level}";

            _destroyedTargets = 0;
            if (LevelManager.Instance != null)
            {
                _totalTargets = LevelManager.Instance.TargetCount;
                UpdateTargetProgress();
            }
        }

        private void UpdateShots(int remaining)
        {
            if (shotsText != null)
                shotsText.text = $"x{remaining}";

            // 타겟 파괴 수 업데이트
            if (LevelManager.Instance != null)
            {
                _destroyedTargets = LevelManager.Instance.DestroyedCount;
                UpdateTargetProgress();
            }
        }

        private void UpdateTargetProgress()
        {
            if (targetText != null)
                targetText.text = $"{_destroyedTargets}/{_totalTargets}";

            if (targetProgressBar != null && _totalTargets > 0)
                targetProgressBar.value = (float)_destroyedTargets / _totalTargets;
        }

        private void UpdateEnergy(int energy)
        {
            if (energyText != null && EnergyManager.Instance != null)
                energyText.text = $"{energy}/{EnergyManager.Instance.GetMaxEnergy()}";
        }

        private void UpdateEnergyTimer()
        {
            var em = EnergyManager.Instance;
            if (em == null || energyTimerText == null) return;
            if (em.IsFull) { energyTimerText.text = "FULL"; return; }

            float remaining = em.GetRegenSecondsRemaining();
            int   min = Mathf.FloorToInt(remaining / 60f);
            int   sec = Mathf.FloorToInt(remaining % 60f);
            energyTimerText.text = $"{min:00}:{sec:00}";
        }

        private void UpdateCoins(int coins)
        {
            if (coinsText != null) coinsText.text = coins.ToString("N0");
        }
    }
}
