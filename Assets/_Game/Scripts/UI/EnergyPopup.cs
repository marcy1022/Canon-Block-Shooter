using SmashPuzzle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    public class EnergyPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button adBtn;       // 광고 시청 → +1 에너지
        [SerializeField] private Button iapBtn;      // Mock IAP → 에너지 가득

        private void Start()
        {
            gameObject.SetActive(false);
            closeBtn?.onClick.AddListener(() => gameObject.SetActive(false));
            adBtn?.onClick.AddListener(OnWatchAd);
            iapBtn?.onClick.AddListener(OnMockIAP);
        }

        private void OnEnable()
        {
            EnergyManager.OnEnergyDepleted.AddListener(Show);
            EnergyManager.OnEnergyChanged.AddListener(UpdateEnergyDisplay);
        }

        private void OnDisable()
        {
            EnergyManager.OnEnergyDepleted.RemoveListener(Show);
            EnergyManager.OnEnergyChanged.RemoveListener(UpdateEnergyDisplay);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            var em = EnergyManager.Instance;
            if (em == null || timerText == null) return;
            if (em.IsFull) { timerText.text = "FULL"; return; }
            float remaining = em.GetRegenSecondsRemaining();
            int   min = Mathf.FloorToInt(remaining / 60f);
            int   sec = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"다음 에너지: {min:00}:{sec:00}";
        }

        private void Show()
        {
            gameObject.SetActive(true);
            UpdateEnergyDisplay(EnergyManager.Instance?.CurrentEnergy ?? 0);
        }

        private void UpdateEnergyDisplay(int energy)
        {
            var em = EnergyManager.Instance;
            if (energyText != null && em != null)
                energyText.text = $"{energy}/{em.GetMaxEnergy()}";
        }

        private void OnWatchAd()
        {
            bool ok = EnergyManager.Instance?.WatchAdForEnergy() ?? false;
            if (!ok) Debug.Log("[EnergyPopup] Ad daily limit reached");
            if (EnergyManager.Instance?.CurrentEnergy > 0)
                gameObject.SetActive(false);
        }

        private void OnMockIAP()
        {
            EnergyManager.Instance?.AddEnergy(99);
            gameObject.SetActive(false);
        }
    }
}
