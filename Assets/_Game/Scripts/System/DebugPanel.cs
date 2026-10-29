using UnityEngine;
using UnityEngine.InputSystem;

namespace SmashPuzzle.Core
{
    // 에디터/개발 빌드 전용 Mock 버튼 패널.
    // 실제 UI 없이 키보드 단축키 및 OnGUI 버튼으로 구현.
    public class DebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEBUG
        private bool _visible = false;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            GUILayout.BeginArea(new Rect(10, 10, 220, 200), GUI.skin.box);
            GUILayout.Label("[ Debug Panel ]");

            if (GUILayout.Button("Energy MAX"))
                EnergyManager.Instance?.AddEnergy(99);

            if (GUILayout.Button("Coins +1000"))
                CoinManager.Instance?.AddCoins(1000);

            if (GUILayout.Button("Ad: +1 Energy (Mock)"))
                EnergyManager.Instance?.WatchAdForEnergy();

            if (GUILayout.Button("IAP: Energy ∞ (Mock)"))
                EnergyManager.Instance?.AddEnergy(99);

            if (GUILayout.Button("Ball Booster"))
                BoosterManager.Instance?.TryUseBallBooster();

            GUILayout.EndArea();
        }
#endif
    }
}
