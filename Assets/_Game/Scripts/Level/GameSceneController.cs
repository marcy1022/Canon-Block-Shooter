using SmashPuzzle.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmashPuzzle.Level
{
    // GameScene 진입 시 레벨 로드 및 시작을 조율한다.
    // Managers GO에 붙인다.
    public class GameSceneController : MonoBehaviour
    {
        private const string KeyCurrentLevel = "CurrentLevel";

        private void OnEnable()
        {
            LevelManager.OnStarRating.AddListener(OnClear);
            LevelManager.OnLevelFail.AddListener(OnFail);
        }

        private void OnDisable()
        {
            LevelManager.OnStarRating.RemoveListener(OnClear);
            LevelManager.OnLevelFail.RemoveListener(OnFail);
        }

        private void Start()
        {
            // SceneBootstrap.Start()(order -100)이 이미 LoadLevel + InitLevel을 처리함
            // 여기서는 부가 시스템(에너지, 오디오, 튜토리얼)만 담당
            int levelId = PlayerPrefs.GetInt(KeyCurrentLevel, 1);
            if (levelId < 1 || levelId > 10) levelId = 1;

            var energy = EnergyManager.Instance;
            if (energy != null && energy.HasGameData && !energy.TrySpendEnergy(1))
            {
                Debug.Log("[GameScene] Not enough energy!");
                return;
            }

            GameManager.Instance?.SetState(GameState.Playing);
            SmashPuzzle.Core.BoosterManager.Instance?.ResetForNewLevel();
            SmashPuzzle.Core.TutorialManager.Instance?.TryStartTutorial(levelId);
            SmashPuzzle.Core.AudioManager.Instance?.PlayBGM();
        }

        private void OnClear(int stars) =>
            Debug.Log($"[GameScene] LEVEL CLEAR! Stars: {stars}");

        private void OnFail() =>
            Debug.Log("[GameScene] LEVEL FAIL!");
    }
}
