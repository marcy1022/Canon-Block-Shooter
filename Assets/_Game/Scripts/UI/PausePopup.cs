using SmashPuzzle.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    public class PausePopup : MonoBehaviour
    {
        [SerializeField] private Button resumeBtn;
        [SerializeField] private Button mapBtn;
        [SerializeField] private Slider volumeSlider;

        private void Start()
        {
            gameObject.SetActive(false);
            resumeBtn?.onClick.AddListener(OnResume);
            mapBtn?.onClick.AddListener(OnGoToMap);
            volumeSlider?.onValueChanged.AddListener(OnVolumeChanged);

            if (volumeSlider != null)
                volumeSlider.value = AudioListener.volume;
        }

        private void OnEnable()  => GameManager.OnStateChanged.AddListener(OnStateChanged);
        private void OnDisable() => GameManager.OnStateChanged.RemoveListener(OnStateChanged);

        private void OnStateChanged(GameState state)
        {
            bool paused = state == GameState.Paused;
            gameObject.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
        }

        private void OnResume()  => GameManager.Instance?.ResumeGame();

        private void OnGoToMap()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MapScene");
        }

        private void OnVolumeChanged(float v) => AudioListener.volume = v;
    }
}
