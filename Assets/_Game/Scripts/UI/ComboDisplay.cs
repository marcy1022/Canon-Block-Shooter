using System.Collections;
using SmashPuzzle.Destructible;
using TMPro;
using UnityEngine;

namespace SmashPuzzle.UI
{
    // 콤보 텍스트 팝업 + 카메라 쉐이크
    public class ComboDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI comboLabel;
        [SerializeField] private float           displayDuration  = 1.5f;
        [SerializeField] private float           shakeAmplitude   = 0.15f;
        [SerializeField] private float           shakeDuration    = 0.3f;

        private Transform _camTransform;
        private Vector3   _camOrigin;

        private void Awake()
        {
            _camTransform = Camera.main?.transform;
            if (_camTransform != null) _camOrigin = _camTransform.localPosition;
            if (comboLabel != null) comboLabel.gameObject.SetActive(false);
        }

        private void OnEnable()  => ComboTracker.OnComboUpdated += OnCombo;
        private void OnDisable() => ComboTracker.OnComboUpdated -= OnCombo;

        private void OnCombo(int combo)
        {
            if (combo < 2) return;

            string text;
            Color  color;

            if (combo == 2)      { text = "COMBO!";       color = Color.yellow; }
            else if (combo == 3) { text = "TRIPLE!";      color = new Color(1f, 0.5f, 0f); }
            else if (combo >= 4) { text = "SMASH!";       color = Color.red; }
            else                 { text = "TOTAL SMASH!"; color = Color.red; }

            StopAllCoroutines();
            StartCoroutine(ShowText(text, color));
            if (combo >= 3) StartCoroutine(Shake());
        }

        private IEnumerator ShowText(string text, Color color)
        {
            if (comboLabel == null) yield break;

            comboLabel.text  = text;
            comboLabel.color = color;
            comboLabel.gameObject.SetActive(true);

            yield return new WaitForSeconds(displayDuration);
            comboLabel.gameObject.SetActive(false);
        }

        private IEnumerator Shake()
        {
            if (_camTransform == null) yield break;

            float t = 0f;
            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float ratio = 1f - (t / shakeDuration);
                _camTransform.localPosition = _camOrigin + (Vector3)Random.insideUnitCircle * shakeAmplitude * ratio;
                yield return null;
            }
            _camTransform.localPosition = _camOrigin;
        }
    }
}
