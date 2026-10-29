using SmashPuzzle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    // MenuScene 전담 컨트롤러
    public class MenuSceneController : MonoBehaviour
    {
        [SerializeField] private Button playBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI coinsText;

        private const string KeyFirstRun = "FirstRunDone";

        private void Start()
        {
            playBtn?.onClick.AddListener(OnPlay);
            settingsBtn?.onClick.AddListener(OnSettings);
            Refresh();
        }

        private void OnEnable()
        {
            EnergyManager.OnEnergyChanged.AddListener(OnEnergyChanged);
            CoinManager.OnCoinsChanged.AddListener(OnCoinsChanged);
        }

        private void OnDisable()
        {
            EnergyManager.OnEnergyChanged.RemoveListener(OnEnergyChanged);
            CoinManager.OnCoinsChanged.RemoveListener(OnCoinsChanged);
        }

        private void OnEnergyChanged(int _) => Refresh();
        private void OnCoinsChanged(int _)  => Refresh();

        private void Refresh()
        {
            var em = EnergyManager.Instance;
            var cm = CoinManager.Instance;
            if (energyText != null && em != null)
                energyText.text = $"⚡{em.CurrentEnergy}/{em.GetMaxEnergy()}";
            if (coinsText != null && cm != null)
                coinsText.text = cm.Balance.ToString("N0");
        }

        private void OnPlay()
        {
            // 최초 실행: 레벨 1 직접 진입
            if (!PlayerPrefs.HasKey(KeyFirstRun))
            {
                PlayerPrefs.SetInt(KeyFirstRun, 1);
                PlayerPrefs.SetInt("CurrentLevel", 1);
                SceneManager.LoadScene("GameScene");
            }
            else
            {
                SceneManager.LoadScene("MapScene");
            }
        }

        private void OnSettings()
        {
            // 프로토타입: 설정 팝업 (음량 토글 등) — 추후 구현
            AudioListener.volume = AudioListener.volume > 0.5f ? 0f : 1f;
            Debug.Log($"[Menu] Volume: {AudioListener.volume}");
        }
    }
}
