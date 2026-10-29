using SmashPuzzle.Level;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    // ScrollRect 안에 레벨 노드를 동적으로 생성한다.
    public class LevelMapUI : MonoBehaviour
    {
        [Header("Node Template")]
        [SerializeField] private GameObject levelNodePrefab; // 레벨 버튼 프리팹

        [Header("Layout")]
        [SerializeField] private Transform  nodeContainer;   // ScrollRect Content

        [Header("Navigation")]
        [SerializeField] private Button homeBtn;
        [SerializeField] private Button shopBtn;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI coinsText;

        private const int MaxLevel = 30; // 프로토타입: 레벨 30개

        private void Start()
        {
            homeBtn?.onClick.AddListener(() => SceneManager.LoadScene("MenuScene"));
            shopBtn?.onClick.AddListener(OnShop);

            BuildNodes();
            RefreshHeader();
        }

        private void OnEnable()
        {
            SmashPuzzle.Core.EnergyManager.OnEnergyChanged.AddListener(OnEnergyChanged);
            SmashPuzzle.Core.CoinManager.OnCoinsChanged.AddListener(OnCoinsChanged);
        }

        private void OnDisable()
        {
            SmashPuzzle.Core.EnergyManager.OnEnergyChanged.RemoveListener(OnEnergyChanged);
            SmashPuzzle.Core.CoinManager.OnCoinsChanged.RemoveListener(OnCoinsChanged);
        }

        private void OnEnergyChanged(int _) => RefreshHeader();
        private void OnCoinsChanged(int _)  => RefreshHeader();

        private void BuildNodes()
        {
            if (nodeContainer == null || levelNodePrefab == null) return;

            foreach (Transform child in nodeContainer)
                Destroy(child.gameObject);

            var progress = LevelProgressManager.Instance;

            for (int i = 1; i <= MaxLevel; i++)
            {
                int levelId = i;
                var node    = Instantiate(levelNodePrefab, nodeContainer);

                bool unlocked = progress == null || progress.IsLevelUnlocked(levelId);
                int  stars    = progress?.GetStars(levelId) ?? 0;

                // 레벨 번호 텍스트
                var label = node.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = unlocked ? levelId.ToString() : "🔒";

                // 스타 표시 (자식 오브젝트 3개 Star1/2/3 가정)
                SetStars(node.transform, stars);

                // 버튼 인터랙션
                var btn = node.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = unlocked;
                    if (unlocked)
                        btn.onClick.AddListener(() => LoadLevel(levelId));
                }
            }
        }

        private void SetStars(Transform node, int stars)
        {
            for (int i = 1; i <= 3; i++)
            {
                var star = node.Find($"Star{i}");
                if (star != null)
                    star.gameObject.SetActive(i <= stars);
            }
        }

        private void LoadLevel(int levelId)
        {
            PlayerPrefs.SetInt("CurrentLevel", levelId);
            SceneManager.LoadScene("GameScene");
        }

        private void RefreshHeader()
        {
            var em = SmashPuzzle.Core.EnergyManager.Instance;
            var cm = SmashPuzzle.Core.CoinManager.Instance;
            if (energyText != null && em != null)
                energyText.text = $"⚡{em.CurrentEnergy}/{em.GetMaxEnergy()}";
            if (coinsText != null && cm != null)
                coinsText.text = cm.Balance.ToString("N0");
        }

        private void OnShop()
        {
            // Mock: 코인 충전 팝업 (프로토타입은 Debug 패널 활용)
            SmashPuzzle.Core.CoinManager.Instance?.AddCoins(100);
            Debug.Log("[LevelMapUI] Shop Mock: +100 coins");
        }
    }
}
