using System.Collections;
using SmashPuzzle.Cannon;
using SmashPuzzle.Destructible;
using SmashPuzzle.Level;
using UnityEngine;

namespace SmashPuzzle.Core
{
    // 레벨 1~3 인터랙티브 튜토리얼 시퀀스
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private TutorialUI.TutorialOverlay overlay;

        private int  _currentLevel;
        private bool _step1Done; // 드래그 감지
        private bool _step2Done; // 발사 감지
        private bool _step3Done; // 타겟 파괴 감지

        private static string FlagKey(int level) => $"TutorialCompleted_{level}";
        public static bool IsDone(int level)     => PlayerPrefs.GetInt(FlagKey(level), 0) == 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // GameSceneController.StartLevel 이후 호출
        public void TryStartTutorial(int levelId)
        {
            _currentLevel = levelId;

            if (levelId == 1 && !IsDone(1))
                StartCoroutine(TutorialLevel1());
            else if (levelId == 2 && !IsDone(2))
                StartCoroutine(HintLevel2());
            else if (levelId == 3 && !IsDone(3))
                StartCoroutine(HintLevel3());
        }

        // ─── Level 1: 3단계 인터랙티브 가이드 ───────────────────────

        private IEnumerator TutorialLevel1()
        {
            overlay?.Show("드래그해서 조준하세요!", true);
            CannonController.OnFireRequested += OnFireDetected;
            // 드래그 시작 감지: CannonController.IsDragging을 폴링
            var cannon = FindFirstObjectByType<CannonController>();
            while (!_step1Done)
            {
                if (cannon != null && cannon.IsDragging) _step1Done = true;
                yield return null;
            }

            overlay?.Show("손가락을 떼면 발사됩니다!", true);
            while (!_step2Done) yield return null; // OnFireDetected에서 세팅

            CannonController.OnFireRequested -= OnFireDetected;
            overlay?.Show("빨간 표시를 모두 파괴하세요!", false);
            LevelManager.OnStarRating.AddListener(OnLevel1Clear);

            while (!_step3Done) yield return null; // OnLevel1Clear에서 세팅

            overlay?.Hide();
            MarkDone(1);
        }

        private void OnFireDetected(float _, float __) => _step2Done = true;

        private void OnLevel1Clear(int stars)
        {
            _step3Done = true;
            LevelManager.OnStarRating.RemoveListener(OnLevel1Clear);
        }

        // ─── Level 2: Pillar 화살표 힌트 ─────────────────────────────

        private IEnumerator HintLevel2()
        {
            yield return new WaitForSeconds(1.5f);

            var pillar = FindFirstDestructibleOfType(ObjectType.Pillar);
            if (pillar != null)
                overlay?.ShowArrowAt(pillar.transform.position, "여기를 노려보세요!");

            yield return new WaitForSeconds(3f);
            overlay?.Hide();
            MarkDone(2);
        }

        // ─── Level 3: TNT 하이라이트 ─────────────────────────────────

        private IEnumerator HintLevel3()
        {
            yield return new WaitForSeconds(1.5f);

            var tnt = FindFirstDestructibleOfType(ObjectType.TNT);
            if (tnt != null)
                overlay?.ShowHighlight(tnt.transform);

            yield return new WaitForSeconds(4f);
            overlay?.Hide();
            MarkDone(3);
        }

        // ─── Utils ───────────────────────────────────────────────────

        private static void MarkDone(int level) =>
            PlayerPrefs.SetInt(FlagKey(level), 1);

        private static DestructibleObject FindFirstDestructibleOfType(ObjectType type)
        {
            foreach (var d in FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None))
                if (d.Type == type) return d;
            return null;
        }
    }
}
