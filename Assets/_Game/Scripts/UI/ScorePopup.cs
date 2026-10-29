using System.Collections;
using TMPro;
using UnityEngine;

namespace SmashPuzzle.UI
{
    // "+N" 텍스트가 위로 이동하며 사라지는 팝업
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float           riseSpeed    = 1.5f;
        [SerializeField] private float           duration     = 1.0f;

        public static ScorePopup Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            gameObject.SetActive(false);
        }

        public void Show(int score, Vector3 worldPos)
        {
            if (label != null) label.text = $"+{score}";
            transform.position = worldPos;
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            var startPos = transform.position;
            var color    = label != null ? label.color : Color.white;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float ratio = t / duration;
                transform.position = startPos + Vector3.up * (riseSpeed * ratio);
                if (label != null)
                    label.color = new Color(color.r, color.g, color.b, 1f - ratio);
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
