using System.Collections;
using SmashPuzzle.Core;
using SmashPuzzle.Level;
using TMPro;
using UnityEngine;

namespace SmashPuzzle.Core
{
    // 클리어 연출: 슬로우모션 → SMASH! → 컨페티 → 팝업 (역기획서 §7.1.4)
    public class ClearSequence : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI smashText;
        [SerializeField] private Vector3         confettiPosition = new Vector3(0f, 3f, 0f);

        private void OnEnable()  => LevelManager.OnStarRating.AddListener(OnClear);
        private void OnDisable() => LevelManager.OnStarRating.RemoveListener(OnClear);

        private void Start()
        {
            if (smashText != null) smashText.gameObject.SetActive(false);
        }

        private void OnClear(int stars) => StartCoroutine(RunSequence());

        private IEnumerator RunSequence()
        {
            // T+0.0s: 슬로우모션 시작
            Time.timeScale = 0.3f;
            AudioManager.Instance?.SetSlowMotionPitch(true);

            yield return new WaitForSecondsRealtime(0.8f); // 0.8초간 슬로우

            Time.timeScale = 1f;
            AudioManager.Instance?.SetSlowMotionPitch(false);

            // T+1.3s: SMASH! 텍스트 (스케일 3→1 바운스)
            if (smashText != null)
            {
                smashText.gameObject.SetActive(true);
                smashText.transform.localScale = Vector3.one * 3f;
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    smashText.transform.localScale = Vector3.Lerp(
                        Vector3.one * 3f, Vector3.one, t / 0.3f);
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // T+1.6s: 컨페티
            FXManager.Instance?.PlayConfetti(confettiPosition);

            yield return new WaitForSecondsRealtime(0.4f);

            // T+2.0s: 클리어 팝업은 UIManager가 GameState.LevelComplete를 받아 처리
        }
    }
}
