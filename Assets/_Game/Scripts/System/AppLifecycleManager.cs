using SmashPuzzle.Core;
using UnityEngine;

namespace SmashPuzzle.Core
{
    // 앱 포커스/일시정지 이벤트 처리 — 에너지 타임스탬프 저장 및 오프라인 회복
    public class AppLifecycleManager : MonoBehaviour
    {
        public static AppLifecycleManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                OnBackground();
            else
                OnForeground();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                OnBackground();
            else
                OnForeground();
        }

        private void OnBackground()
        {
            EnergyManager.Instance?.SaveEnergy();
            PlayerPrefs.Save();
        }

        private void OnForeground()
        {
            EnergyManager.Instance?.RestoreOfflineEnergy();
        }

        private void OnApplicationQuit()
        {
            EnergyManager.Instance?.SaveEnergy();
            PlayerPrefs.Save();
        }
    }
}
