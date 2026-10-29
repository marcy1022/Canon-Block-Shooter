using SmashPuzzle.Core;
using SmashPuzzle.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject energyEmptyPanel;

        [Header("Pause Button")]
        [SerializeField] private Button pauseButton;

        private void OnEnable()
        {
            GameManager.OnStateChanged.AddListener(HandleStateChange);
            EnergyManager.OnEnergyDepleted.AddListener(ShowEnergyEmptyPopup);
            LevelManager.OnStarRating.AddListener(ShowLevelComplete);
            LevelManager.OnLevelFail.AddListener(ShowGameOver);
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged.RemoveListener(HandleStateChange);
            EnergyManager.OnEnergyDepleted.RemoveListener(ShowEnergyEmptyPopup);
            LevelManager.OnStarRating.RemoveListener(ShowLevelComplete);
            LevelManager.OnLevelFail.RemoveListener(ShowGameOver);
        }

        private void Start()
        {
            pauseButton?.onClick.AddListener(() => GameManager.Instance?.PauseGame());
            SetPanel(hudPanel, true);
        }

        private void HandleStateChange(GameState state)
        {
            SetPanel(hudPanel,           state == GameState.Playing);
            SetPanel(pausePanel,         state == GameState.Paused);
            SetPanel(levelCompletePanel, state == GameState.LevelComplete);
            SetPanel(gameOverPanel,      state == GameState.GameOver);
        }

        private void ShowEnergyEmptyPopup() => SetPanel(energyEmptyPanel, true);
        private void ShowLevelComplete(int stars) { }
        private void ShowGameOver() { }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        public static void GoToMenu()   => SceneManager.LoadScene("MenuScene");
        public static void GoToMap()    => SceneManager.LoadScene("MapScene");
        public static void ReloadGame() => SceneManager.LoadScene("GameScene");
    }
}
