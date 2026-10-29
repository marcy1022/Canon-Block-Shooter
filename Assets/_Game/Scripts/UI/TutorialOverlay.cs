using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmashPuzzle.TutorialUI
{
    // 손가락 가이드 이미지, 텍스트, 화살표, 하이라이트 표시
    public class TutorialOverlay : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject      panel;
        [SerializeField] private TextMeshProUGUI guideText;
        [SerializeField] private Image           fingerGuide;
        [SerializeField] private GameObject      arrowIndicator;

        [Header("Highlight")]
        [SerializeField] private SpriteRenderer  highlightSprite; // 3D 월드 스페이스 하이라이트
        [SerializeField] private Color           highlightColor = new Color(1f, 1f, 0f, 0.6f);
        [SerializeField] private float           blinkInterval  = 0.3f;

        private Camera _cam;
        private Coroutine _blinkCoroutine;

        private void Awake()
        {
            _cam = Camera.main;
            panel?.SetActive(false);
            if (highlightSprite) highlightSprite.gameObject.SetActive(false);
        }

        public void Show(string text, bool showFinger)
        {
            panel?.SetActive(true);
            if (guideText   != null) guideText.text = text;
            if (fingerGuide != null) fingerGuide.gameObject.SetActive(showFinger);
            if (arrowIndicator != null) arrowIndicator.SetActive(false);
        }

        public void ShowArrowAt(Vector3 worldPos, string text)
        {
            panel?.SetActive(true);
            if (guideText    != null) guideText.text = text;
            if (fingerGuide  != null) fingerGuide.gameObject.SetActive(false);

            if (arrowIndicator != null && _cam != null)
            {
                arrowIndicator.SetActive(true);
                Vector3 screen = _cam.WorldToScreenPoint(worldPos + Vector3.up * 1.2f);
                arrowIndicator.transform.position = screen;
            }
        }

        public void ShowHighlight(Transform target)
        {
            if (highlightSprite == null || target == null) return;
            highlightSprite.transform.position = target.position;
            highlightSprite.transform.localScale = target.lossyScale * 1.3f;
            highlightSprite.color = highlightColor;
            highlightSprite.gameObject.SetActive(true);

            if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = StartCoroutine(Blink());
        }

        public void Hide()
        {
            panel?.SetActive(false);
            if (highlightSprite) highlightSprite.gameObject.SetActive(false);
            if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        }

        private IEnumerator Blink()
        {
            while (true)
            {
                if (highlightSprite) highlightSprite.enabled = !highlightSprite.enabled;
                yield return new WaitForSeconds(blinkInterval);
            }
        }
    }
}
