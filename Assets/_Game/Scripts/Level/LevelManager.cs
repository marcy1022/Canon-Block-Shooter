using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SmashPuzzle.Data;
using SmashPuzzle.Core;
using SmashPuzzle.Destructible;
using SmashPuzzle.Cannon;

namespace SmashPuzzle.Level
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private float clearDelay = 0.8f;

        // 테이블 상판 Y=1.3 기준 — 이 값 미만이면 테이블 아래로 떨어진 것
        // (테이블 위 오브젝트 최소 Y ≈ 1.55, 바닥 오브젝트 최대 Y ≈ 0.6)
        private static float TableFallY => SceneBootstrap.TableTopY;

        public int  ShotsRemaining  { get; private set; }
        public int  TargetCount     { get; private set; }
        public int  DestroyedCount  { get; private set; }
        public bool LevelCleared    { get; private set; }
        public bool IsInitialized   { get; private set; }
        public int  LastCoinReward  { get; private set; }
        public int  LastStars       { get; private set; }

        public  bool ShotFired    { get; private set; }
        private bool _shotFired;
        private bool _clearPending;
        private bool _emptyWarned;   // 빈 리스트 경고 1회만

        public int GetLastCoinReward() => LastCoinReward;

        public static readonly UnityEvent<int> OnLevelInitialized = new();
        public static readonly UnityEvent<int> OnShotFired        = new();
        public static readonly UnityEvent<int> OnStarRating       = new();
        public static readonly UnityEvent<int> OnCoinsRewarded    = new();
        public static readonly UnityEvent      OnLevelFail        = new();

        private LevelJsonData                    _levelData;
        private readonly List<DestructibleObject> _registeredObjects = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()  => CannonController.OnFireRequested += HandleShot;
        private void OnDisable() => CannonController.OnFireRequested -= HandleShot;

        private void Update()
        {
            if (LevelCleared || _clearPending) return;

            // 등록 누락 시 자동 복구 (1회)
            if (_registeredObjects.Count == 0 && !_emptyWarned)
            {
                _emptyWarned = true;
                foreach (var obj in FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None))
                    RegisterDestructible(obj);
                Debug.LogWarning($"[LevelManager] 자동 복구: {_registeredObjects.Count}개 오브젝트 재등록");
            }

            if (_registeredObjects.Count > 0 && AllObjectsOffTable())
                TriggerClear();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (LevelCleared) return;
            GUILayout.BeginArea(new Rect(10, 40, 340, 200));
            GUILayout.Label($"<b>[LevelMgr]</b> 등록:{_registeredObjects.Count}  클리어:{LevelCleared}  pending:{_clearPending}");
            float thr = TableFallY;
            foreach (var obj in _registeredObjects)
            {
                if (obj == null) { GUILayout.Label("  - (null)"); continue; }
                float y = obj.transform.position.y;
                bool off = !obj.gameObject.activeInHierarchy || obj.IsDestroyed || y < thr;
                GUILayout.Label($"  {obj.name} target={obj.IsTarget} Y={y:F2} off={off}");
            }
            GUILayout.EndArea();
        }
#endif

        // 테이블 위 모든 오브젝트가 상판(Y=1.3) 아래로 떨어졌는지 확인
        private bool AllObjectsOffTable()
        {
            float threshold = TableFallY;
            int   counted   = 0;
            foreach (var obj in _registeredObjects)
            {
                if (obj == null) continue;
                counted++;
                bool offTable = !obj.gameObject.activeInHierarchy
                                || obj.IsDestroyed
                                || obj.transform.position.y < threshold;
                if (!offTable) return false;
            }
            return counted > 0; // 등록된 오브젝트가 하나도 없으면 false
        }

        private void TriggerClear()
        {
            if (_clearPending || LevelCleared) return;
            _clearPending = true;
            StartCoroutine(ClearSequence());
        }

        // LevelLoader가 레벨 로드 완료 후 호출
        public void InitLevel(LevelJsonData data)
        {
            _levelData     = data;
            ShotsRemaining = data.projectileCount;
            DestroyedCount = 0;
            LevelCleared   = false;
            IsInitialized  = false;  // 등록 완료 전에는 false
            ShotFired      = false;
            _shotFired     = false;
            _clearPending  = false;
            _emptyWarned   = false;
            _registeredObjects.Clear();

            foreach (var obj in FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None))
                RegisterDestructible(obj);

            TargetCount   = _registeredObjects.FindAll(o => o.IsTarget).Count;
            IsInitialized = true;
            OnLevelInitialized.Invoke(ShotsRemaining);
            Debug.Log($"[LevelManager] InitLevel — {_registeredObjects.Count}개 오브젝트 등록, 포탄 {ShotsRemaining}발, 임계Y={TableFallY:F2}");
        }

        public void RegisterDestructible(DestructibleObject obj)
        {
            if (_registeredObjects.Contains(obj)) return;
            _registeredObjects.Add(obj);
            obj.OnDestroyed += HandleDestroyed;
        }

        private void HandleShot(float angle, float force)
        {
            if (LevelCleared || ShotsRemaining <= 0) return;
            ShotsRemaining--;
            ShotFired  = true;
            _shotFired = true;
            OnShotFired.Invoke(ShotsRemaining);

            // 발사 후 2초 뒤 강제 클리어 체크 (물리 안정 후)
            StartCoroutine(DelayedClearCheck(2f));

            if (ShotsRemaining == 0)
                StartCoroutine(CheckFailDelayed());
        }

        // 발사 후 물리가 안정되는 시간을 기다렸다가 클리어 조건 확인
        private IEnumerator DelayedClearCheck(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!LevelCleared && !_clearPending && _registeredObjects.Count > 0)
            {
                // 상태 로그 (1회)
                foreach (var obj in _registeredObjects)
                {
                    if (obj == null) continue;
                    float y = obj.transform.position.y;
                    bool off = !obj.gameObject.activeInHierarchy || obj.IsDestroyed || y < TableFallY;
                    Debug.Log($"[LevelManager] 체크: {obj.name} Y={y:F2} active={obj.gameObject.activeInHierarchy} destroyed={obj.IsDestroyed} off={off}");
                }
                if (AllObjectsOffTable())
                    TriggerClear();
            }
        }

        public void AddExtraProjectiles(int count)
        {
            ShotsRemaining += count;
            OnShotFired.Invoke(ShotsRemaining);
        }

        private void HandleDestroyed(DestructibleObject obj)
        {
            if (!obj.IsTarget) return;
            DestroyedCount++;
            ComboTracker.Instance?.RegisterDestruction();
            // 타겟 파괴 직후 즉시 체크 (Update() 폴링보다 빠름)
            if (_registeredObjects.Count > 0 && AllObjectsOffTable())
                TriggerClear();
        }

        private IEnumerator ClearSequence()
        {
            if (LevelCleared) yield break;
            yield return new WaitForSeconds(clearDelay);

            int stars = CalculateStars();
            LastStars  = stars;           // Update() 폴백이 읽기 전에 설정
            int coins  = CalculateCoinReward(stars);
            LevelCleared = true;          // 마지막에 true → Update() 폴백이 올바른 LastStars 값을 읽음

            LastCoinReward = coins;
            LevelProgressManager.Instance?.SaveProgress(_levelData.levelId, stars, coins);
            SmashPuzzle.Core.CoinManager.Instance?.AddCoins(coins);
            GameManager.Instance?.SetState(GameState.LevelComplete);
            OnStarRating.Invoke(stars);
            OnCoinsRewarded.Invoke(coins);
            Debug.Log($"[LevelManager] LEVEL CLEAR — 별 {stars}개, 코인 {coins}");
        }

        private IEnumerator CheckFailDelayed()
        {
            yield return new WaitForSeconds(clearDelay + 2f);
            if (!LevelCleared)
            {
                GameManager.Instance?.SetState(GameState.GameOver);
                OnLevelFail.Invoke();
            }
        }

        private int CalculateStars()
        {
            if (_levelData == null) return 1;
            if (ShotsRemaining >= _levelData.star3Threshold) return 3;
            if (ShotsRemaining >= _levelData.star2Threshold) return 2;
            return 1;
        }

        private int CalculateCoinReward(int stars)
        {
            if (_levelData == null) return 0;
            return _levelData.coinRewardBase * stars;
        }

        private void OnDestroy()
        {
            foreach (var obj in _registeredObjects)
                if (obj != null) obj.OnDestroyed -= HandleDestroyed;
        }
    }
}
