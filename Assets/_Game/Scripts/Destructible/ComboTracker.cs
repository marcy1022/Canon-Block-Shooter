using System;
using System.Collections;
using UnityEngine;

namespace SmashPuzzle.Destructible
{
    // 0.3초 윈도우 내 연속 파괴를 콤보로 집계.
    // LevelManager가 구독하여 콤보 보너스 코인/UI 처리.
    public class ComboTracker : MonoBehaviour
    {
        public static ComboTracker Instance { get; private set; }

        [SerializeField] private float comboWindowSeconds = 0.3f;

        public int  CurrentCombo { get; private set; }
        public static event Action<int> OnComboUpdated;  // 콤보 수 전달

        private Coroutine _resetCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterDestruction()
        {
            if (_resetCoroutine != null)
                StopCoroutine(_resetCoroutine);

            CurrentCombo++;
            OnComboUpdated?.Invoke(CurrentCombo);

            _resetCoroutine = StartCoroutine(ResetAfterWindow());
        }

        private IEnumerator ResetAfterWindow()
        {
            yield return new WaitForSeconds(comboWindowSeconds);
            CurrentCombo = 0;
            OnComboUpdated?.Invoke(0);
        }

        public void ResetCombo()
        {
            if (_resetCoroutine != null) StopCoroutine(_resetCoroutine);
            CurrentCombo = 0;
        }
    }
}
