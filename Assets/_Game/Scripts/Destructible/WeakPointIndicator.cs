using System.Collections;
using UnityEngine;

namespace SmashPuzzle.Destructible
{
    // 약점 위치를 2초간 빨간색으로 하이라이트하는 힌트 연출.
    // DestructibleObject에 자식으로 배치하고 SpriteRenderer가 필요.
    [RequireComponent(typeof(SpriteRenderer))]
    public class WeakPointIndicator : MonoBehaviour
    {
        [SerializeField] private float highlightDuration = 2f;
        [SerializeField] private Color highlightColor    = new Color(1f, 0.2f, 0.2f, 0.85f);

        private SpriteRenderer _sr;
        private Color          _originalColor;
        private Coroutine      _routine;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _originalColor = _sr.color;
            gameObject.SetActive(false);
        }

        public void ShowHint()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HintRoutine());
        }

        private IEnumerator HintRoutine()
        {
            gameObject.SetActive(true);
            _sr.color = highlightColor;
            yield return new WaitForSeconds(highlightDuration);
            _sr.color = _originalColor;
            gameObject.SetActive(false);
        }
    }
}
