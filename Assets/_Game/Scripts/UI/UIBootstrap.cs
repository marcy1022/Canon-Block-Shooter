using System.Collections;
using SmashPuzzle.Core;
using SmashPuzzle.Level;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmashPuzzle.UI
{
    // 런타임에 Canvas + HUD + 클리어/실패 팝업을 코드로 생성
    [DefaultExecutionOrder(-50)]
    public class UIBootstrap : MonoBehaviour
    {
        private Text _levelText;
        private Text _shotText;
        private GameObject _clearPanel;
        private Text _clearStarText;
        private Text _clearCoinText;
        private GameObject _failPanel;

        private bool _clearShown;
        private bool _failShown;

        private void Awake()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            BuildHUD(canvas);
            _clearPanel = BuildClearPanel(canvas);
            _failPanel  = BuildFailPanel(canvas);
        }

        private void Start()
        {
            // Start() 시점에 이미 InitLevel이 완료된 경우(씬 재로드 후 등) 즉시 반영
            if (LevelManager.Instance != null && LevelManager.Instance.IsInitialized)
            {
                UpdateShots(LevelManager.Instance.ShotsRemaining);
                UpdateLevelDisplay();
            }
        }

        private void Update()
        {
            // LevelManager 이벤트 놓쳤을 때 폴백 감지
            var lm = LevelManager.Instance;
            if (lm == null) return;

            if (!_clearShown && lm.LevelCleared)
            {
                // LastStars는 ClearSequence에서 설정됨 (0이면 아직 계산 전 → 1 star 기본)
                int stars = lm.LastStars > 0 ? lm.LastStars : 1;
                ShowClear(stars);
            }
            else if (!_failShown && !lm.LevelCleared
                     && lm.IsInitialized           // InitLevel 완료 전에는 체크하지 않음
                     && lm.ShotFired               // 한 발도 안 쐈으면 실패 판정 없음
                     && lm.ShotsRemaining <= 0
                     && !_clearShown)
            {
                // 발사 소진 + 미클리어: 잠시 대기 후 실패 처리
                StartCoroutine(DelayedFail());
            }
        }

        private System.Collections.IEnumerator DelayedFail()
        {
            _failShown = true;
            yield return new WaitForSeconds(1.5f);
            var lm = LevelManager.Instance;
            if (lm != null && !lm.LevelCleared && lm.IsInitialized && lm.ShotFired && lm.ShotsRemaining <= 0)
                ShowFail();
            else
                _failShown = false; // 조건 불충족 → 나중에 다시 트리거 가능하도록 리셋
        }

        private void OnEnable()
        {
            LevelManager.OnLevelInitialized.AddListener(OnLevelInit);
            LevelManager.OnStarRating.AddListener(ShowClear);
            LevelManager.OnShotFired.AddListener(UpdateShots);
            LevelManager.OnLevelFail.AddListener(ShowFail);
            SmashPuzzle.Cannon.CannonController.OnFireRequested += OnFire;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelInitialized.RemoveListener(OnLevelInit);
            LevelManager.OnStarRating.RemoveListener(ShowClear);
            LevelManager.OnShotFired.RemoveListener(UpdateShots);
            LevelManager.OnLevelFail.RemoveListener(ShowFail);
            SmashPuzzle.Cannon.CannonController.OnFireRequested -= OnFire;
        }

        private void OnLevelInit(int remaining)
        {
            UpdateShots(remaining);
            UpdateLevelDisplay();
        }

        private void UpdateLevelDisplay()
        {
            if (_levelText == null) return;
            int levelId = PlayerPrefs.GetInt("CurrentLevel", 1);
            _levelText.text = $"Level {levelId}";
        }

        private void OnFire(float a, float f)
        {
            if (LevelManager.Instance != null)
                UpdateShots(LevelManager.Instance.ShotsRemaining);
        }

        private void UpdateShots(int remaining)
        {
            if (_shotText != null)
                _shotText.text = $"Remain : {remaining}";
        }

        private void ShowClear(int stars)
        {
            if (_clearPanel == null || _clearShown) return;
            _clearShown = true;
            _clearPanel.SetActive(true);

            if (_clearStarText != null)
                _clearStarText.text = new string('★', stars) + new string('☆', 3 - stars);

            if (_clearCoinText != null && LevelManager.Instance != null)
                _clearCoinText.text = $"+{LevelManager.Instance.GetLastCoinReward()} 코인";

            StartCoroutine(AnimateClear(stars));
        }

        private IEnumerator AnimateClear(int stars)
        {
            // 슬로우모션
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(0.6f);
            Time.timeScale = 1f;

            // SMASH! 타이틀 바운스 (ClearPanel 안에 있는 타이틀 Text)
            var title = _clearPanel.transform.Find("Title")?.GetComponent<Text>();
            if (title != null)
            {
                title.gameObject.SetActive(true);
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    float scale = Mathf.Lerp(2f, 1f, t / 0.3f);
                    title.transform.localScale = Vector3.one * scale;
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // 별 순차 등장
            if (_clearStarText != null)
            {
                string full  = new string('★', stars);
                string empty = new string('☆', 3 - stars);
                for (int i = 1; i <= stars; i++)
                {
                    _clearStarText.text = new string('★', i)
                                        + new string('☆', stars - i)
                                        + empty;
                    yield return new WaitForSecondsRealtime(0.3f);
                }
                _clearStarText.text = full + empty;
            }
        }

        private void ShowFail()
        {
            // 실패 패널이 열릴 때 대포 잠금 → 버튼 클릭이 발사로 이어지지 않음
            var cannon = Object.FindFirstObjectByType<SmashPuzzle.Cannon.CannonController>();
            cannon?.SetLocked(true);
            if (_failPanel != null) _failPanel.SetActive(true);
        }

        // ── Canvas ──────────────────────────────────────────────────
        private static Canvas CreateCanvas()
        {
            var go = new GameObject("GameCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();

            // ENABLE_INPUT_SYSTEM: New Input System 패키지 설치 시 자동으로 정의되는 심볼
            // Type.GetType() 런타임 어셈블리 탐색은 불안정 — 컴파일 타임 조건부 컴파일이 신뢰성 높음
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // ── HUD ─────────────────────────────────────────────────────
        private void BuildHUD(Canvas canvas)
        {
            var hud = MakePanel(canvas.transform, "HUD",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10),
                new Vector2(320, 110), Color.clear);
            hud.pivot = new Vector2(0, 1);

            // 현재 레벨 표시 (상단)
            _levelText = MakeText(hud.transform, "LevelDisplay", "Level -",
                TextAnchor.MiddleLeft, 38, new Color(1f, 0.95f, 0.6f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -22));
            _levelText.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 44);
            var lo = _levelText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            lo.effectColor    = Color.black;
            lo.effectDistance = new Vector2(2f, -2f);

            // 잔탄 표시 (하단)
            _shotText = MakeText(hud.transform, "ShotCount", "Remain : -",
                TextAnchor.MiddleLeft, 40, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -68));
            _shotText.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 48);
            var outline = _shotText.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor    = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        // ── Clear Panel ─────────────────────────────────────────────
        private GameObject BuildClearPanel(Canvas canvas)
        {
            var panel = MakePanel(canvas.transform, "ClearPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(700, 900), new Color(0, 0, 0, 0.75f));
            panel.gameObject.SetActive(false);

            // 타이틀
            var title = MakeText(panel.transform, "Title", "SMASH CLEAR!",
                TextAnchor.MiddleCenter, 72, new Color(1f, 0.9f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200));
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(660, 120);
            title.fontStyle = FontStyle.Bold;

            // 별
            _clearStarText = MakeText(panel.transform, "Stars", "☆☆☆",
                TextAnchor.MiddleCenter, 100, new Color(1f, 0.85f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60));
            _clearStarText.GetComponent<RectTransform>().sizeDelta = new Vector2(660, 120);

            // 코인
            _clearCoinText = MakeText(panel.transform, "Coins", "",
                TextAnchor.MiddleCenter, 44, new Color(0.9f, 0.8f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40));
            _clearCoinText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 60);

            // 다음 레벨 버튼 — 블록 재스폰 방식으로 다음 레벨 진입
            MakeButton(panel.transform, "NextLevel", "다음 레벨",
                new Vector2(0, -200), new Vector2(280, 80),
                new Color(0.2f, 0.7f, 0.3f), () =>
                {
                    int cur  = PlayerPrefs.GetInt("CurrentLevel", 1);
                    int next = Mathf.Min(cur + 1, 10);
                    PlayerPrefs.SetInt("CurrentLevel", next);
                    DoRestartLevel(next);
                });

            // 다시하기 버튼 — 현재 레벨 원상복구 (씬 리로드 없이 블록 재스폰)
            MakeButton(panel.transform, "Retry", "다시하기",
                new Vector2(0, -300), new Vector2(280, 80),
                new Color(0.6f, 0.3f, 0.1f), () =>
                {
                    int cur = PlayerPrefs.GetInt("CurrentLevel", 1);
                    if (cur < 1 || cur > 10) cur = 1;
                    DoRestartLevel(cur);
                });

            return panel.gameObject;
        }

        // ── Fail Panel ───────────────────────────────────────────────
        private GameObject BuildFailPanel(Canvas canvas)
        {
            var panel = MakePanel(canvas.transform, "FailPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(700, 700), new Color(0, 0, 0, 0.75f));
            panel.gameObject.SetActive(false);

            MakeText(panel.transform, "Title", "실패...",
                TextAnchor.MiddleCenter, 80, new Color(1f, 0.3f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150))
                .GetComponent<RectTransform>().sizeDelta = new Vector2(600, 120);

            MakeText(panel.transform, "Sub", "발사 횟수를 모두 사용했습니다",
                TextAnchor.MiddleCenter, 36, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40))
                .GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);

            MakeButton(panel.transform, "Retry", "다시하기",
                new Vector2(0, -150), new Vector2(280, 80),
                new Color(0.6f, 0.3f, 0.1f), () =>
                {
                    int cur = PlayerPrefs.GetInt("CurrentLevel", 1);
                    if (cur < 1 || cur > 10) cur = 1;
                    DoRestartLevel(cur);
                });

            return panel.gameObject;
        }

        // 현재 레벨 재시작 — 씬 리로드 없이 블록만 재스폰하고 상태 초기화
        private void DoRestartLevel(int levelId)
        {
            StopAllCoroutines();
            Time.timeScale = 1f;

            _clearShown = false;
            _failShown  = false;
            if (_clearPanel != null) _clearPanel.SetActive(false);
            if (_failPanel  != null) _failPanel.SetActive(false);

            // 대포는 잠금 상태로 시작 — 버튼 클릭이 발사로 이어지는 버그 방지
            var cannon = Object.FindFirstObjectByType<SmashPuzzle.Cannon.CannonController>();
            if (cannon != null)
            {
                cannon.SetLocked(true);
                cannon.transform.rotation = Quaternion.identity;
            }

            var loader  = SmashPuzzle.Level.LevelLoader.Instance;
            var manager = SmashPuzzle.Level.LevelManager.Instance;

            if (loader == null || manager == null)
            {
                ReloadScene();
                return;
            }

            if (loader.LoadLevel(levelId) && loader.CurrentLevelData != null)
                manager.InitLevel(loader.CurrentLevelData);

            // 버튼 입력이 완전히 소화된 뒤 잠금 해제 (0.4초 딜레이)
            StartCoroutine(UnlockCannonAfterDelay(cannon, 0.4f));
        }

        private static IEnumerator UnlockCannonAfterDelay(
            SmashPuzzle.Cannon.CannonController cannon, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            cannon?.SetLocked(false);
        }

        // 씬 리로드: buildIndex 우선, 미등록 씬이면 이름으로 폴백 (다음 레벨용)
        private static void ReloadScene()
        {
            Time.timeScale = 1f;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int idx = scene.buildIndex;
            if (idx >= 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene(idx);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        // ── UI Helpers ───────────────────────────────────────────────
        private static RectTransform MakePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            Vector2 size, Color bgColor)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = size;

            if (bgColor.a > 0)
            {
                var img = go.AddComponent<Image>();
                img.color = bgColor;
            }
            return rt;
        }

        private static Text MakeText(Transform parent, string name, string content,
            TextAnchor anchor, int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.anchoredPosition = anchoredPos;

            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.alignment = anchor;
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        private static void MakeButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            // 라벨
            var labelGo = new GameObject("Label");
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.SetParent(go.transform, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;

            var txt = labelGo.AddComponent<Text>();
            txt.text      = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize  = 40;
            txt.color     = Color.white;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
