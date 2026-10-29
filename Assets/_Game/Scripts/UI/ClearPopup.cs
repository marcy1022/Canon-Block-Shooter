using System.Collections;
using SmashPuzzle.Core;
using SmashPuzzle.Level;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    public class ClearPopup : MonoBehaviour
    {
        [Header("Stars")]
        [SerializeField] private GameObject star1;
        [SerializeField] private GameObject star2;
        [SerializeField] private GameObject star3;

        [Header("Reward")]
        [SerializeField] private TextMeshProUGUI coinRewardText;

        [Header("Buttons")]
        [SerializeField] private Button nextLevelBtn;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button adCoinsBtn;

        private int _currentStars;

        private void OnEnable()  => LevelManager.OnStarRating.AddListener(Show);
        private void OnDisable() => LevelManager.OnStarRating.RemoveListener(Show);

        private void Start()
        {
            gameObject.SetActive(false);
            nextLevelBtn?.onClick.AddListener(OnNextLevel);
            retryBtn?.onClick.AddListener(OnRetry);
            adCoinsBtn?.onClick.AddListener(OnWatchAdCoins);
        }

        private void Show(int stars)
        {
            _currentStars = stars;
            gameObject.SetActive(true);
            SetStarsInactive();
            StartCoroutine(AnimateStars(stars));
        }

        private void SetStarsInactive()
        {
            if (star1) star1.SetActive(false);
            if (star2) star2.SetActive(false);
            if (star3) star3.SetActive(false);
        }

        private IEnumerator AnimateStars(int stars)
        {
            // T+2.3 / 2.6 / 2.9s 순차 등장
            yield return new WaitForSecondsRealtime(2.3f);
            if (stars >= 1 && star1) star1.SetActive(true);

            yield return new WaitForSecondsRealtime(0.3f);
            if (stars >= 2 && star2) star2.SetActive(true);

            yield return new WaitForSecondsRealtime(0.3f);
            if (stars >= 3 && star3) star3.SetActive(true);

            // 코인 표시 갱신
            if (coinRewardText != null && LevelManager.Instance != null)
            {
                int coins = LevelManager.Instance.GetLastCoinReward();
                coinRewardText.text = $"+{coins}";
            }
        }

        private void OnNextLevel()
        {
            int next = (GameManager.Instance?.CurrentLevel ?? 1) + 1;
            PlayerPrefs.SetInt("CurrentLevel", next);
            SceneManager.LoadScene("GameScene");
        }

        private void OnRetry()
        {
            SceneManager.LoadScene("GameScene");
        }

        private void OnWatchAdCoins()
        {
            // Mock: 광고 없이 즉시 코인 +30
            CoinManager.Instance?.AddCoins(30);
            if (adCoinsBtn != null) adCoinsBtn.interactable = false;
        }
    }
}
