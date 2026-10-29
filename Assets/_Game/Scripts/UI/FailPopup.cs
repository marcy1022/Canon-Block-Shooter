using SmashPuzzle.Core;
using SmashPuzzle.Level;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    public class FailPopup : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private TextMeshProUGUI reasonText;

        [Header("Buttons")]
        [SerializeField] private Button homeBtn;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button adBallBtn;  // "광고 보고 볼 +1"
        [SerializeField] private GameObject softIAPPanel; // 2회 이상 실패 시 노출

        private static int _consecutiveFails = 0;

        private void OnEnable()  => LevelManager.OnLevelFail.AddListener(Show);
        private void OnDisable() => LevelManager.OnLevelFail.RemoveListener(Show);

        private void Start()
        {
            gameObject.SetActive(false);
            homeBtn?.onClick.AddListener(OnHome);
            retryBtn?.onClick.AddListener(OnRetry);
            adBallBtn?.onClick.AddListener(OnWatchAdBall);
        }

        private void Show()
        {
            _consecutiveFails++;
            gameObject.SetActive(true);

            // 남은 타겟 수 표시
            if (reasonText != null && LevelManager.Instance != null)
            {
                int remaining = LevelManager.Instance.TargetCount - LevelManager.Instance.DestroyedCount;
                reasonText.text = $"타겟 {remaining}개 남음";
            }

            // 2회 이상 연속 실패 → Soft IAP 패널 노출
            if (softIAPPanel != null)
                softIAPPanel.SetActive(_consecutiveFails >= 2);
        }

        private void OnHome()
        {
            _consecutiveFails = 0;
            SceneManager.LoadScene("MapScene");
        }

        private void OnRetry()
        {
            // 에너지 소모 후 재시도
            if (EnergyManager.Instance != null && !EnergyManager.Instance.TrySpendEnergy(1))
            {
                EnergyManager.OnEnergyDepleted.Invoke();
                return;
            }
            SceneManager.LoadScene("GameScene");
        }

        private void OnWatchAdBall()
        {
            // Mock: 광고 없이 즉시 볼 +1
            LevelManager.Instance?.AddExtraProjectiles(1);
            gameObject.SetActive(false);

            if (adBallBtn != null) adBallBtn.interactable = false;
        }
    }
}
