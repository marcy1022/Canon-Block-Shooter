using UnityEngine;

namespace SmashPuzzle.Core
{
    [DefaultExecutionOrder(-100)]
    public class SceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            SetupPhysicsLayers();
            SetupCamera();
            RecreateGround();
            RecreateTable();
            RecreateWalls();
            RecreateCannon();
        }

        private void Start()
        {
            if (Object.FindFirstObjectByType<SmashPuzzle.UI.UIBootstrap>() == null)
            {
                var uiGo = new GameObject("UIBootstrap");
                uiGo.AddComponent<SmashPuzzle.UI.UIBootstrap>();
            }

            // SceneBootstrap이 레벨 로드를 책임짐 (GameSceneController 유무 무관)
            // GameSceneController가 있으면 이 코드 이후에 중복 호출을 막음
            var loader  = SmashPuzzle.Level.LevelLoader.Instance;
            var manager = SmashPuzzle.Level.LevelManager.Instance;
            if (loader != null && loader.CurrentLevelData == null)
            {
                int levelId = UnityEngine.PlayerPrefs.GetInt("CurrentLevel", 1);
                if (levelId < 1 || levelId > 10) levelId = 1;
                if (loader.LoadLevel(levelId) && manager != null)
                    manager.InitLevel(loader.CurrentLevelData);
            }
        }

        private static void SetupPhysicsLayers()
        {
            for (int i = 6; i <= 9; i++)
                for (int j = 6; j <= 9; j++)
                    Physics.IgnoreLayerCollision(i, j, false);
        }

        // ── Camera ────────────────────────────────────────────────────────────
        // 3D 1인칭 대포 시점:
        //   - 원근감(Perspective) 카메라
        //   - 대포 뒤 위에서 내려다봄 → 대포가 화면 하단에 크게, 테이블이 앞에 작게
        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic  = false;
            cam.fieldOfView   = 62f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 100f;
            cam.backgroundColor = new Color(0.40f, 0.65f, 0.90f);
            cam.clearFlags      = CameraClearFlags.SolidColor;
            // 대포(0,0.4,0) 뒤에서 테이블(0,1.5,4) 방향을 바라봄
            cam.transform.position = new Vector3(0f, 2.2f, -3.0f);
            cam.transform.LookAt(new Vector3(0f, 1.2f, 4f));
        }

        // ── Ground ────────────────────────────────────────────────────────────
        private static void RecreateGround()
        {
            DestroyNamed("Ground");
            DestroyNamed("Dirt");

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ground";
            go.transform.position   = new Vector3(0f, -0.4f, 5f); // 카메라 앞 넓게
            go.transform.localScale = new Vector3(20f, 0.5f, 25f);
            SetLayer(go, "Ground", 8);
            ApplyColor(go, new Color(0.33f, 0.55f, 0.22f)); // 잔디
        }

        // ── Table (받침대, 고정) ───────────────────────────────────────────────
        // 테이블은 Ground 레이어 → 파괴 불가, 블록들이 위에 놓임
        public const float TableZ    = 4.0f;  // LevelLoader에서 참조
        public const float TableTopY = 1.3f;  // 테이블 윗면 Y (블록 스택 기준)

        private static void RecreateTable()
        {
            DestroyNamed("Table");
            DestroyNamed("TableLeg_FL");
            DestroyNamed("TableLeg_FR");
            DestroyNamed("TableLeg_BL");
            DestroyNamed("TableLeg_BR");

            // 테이블 상판
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Table";
            top.transform.position   = new Vector3(0f, TableTopY - 0.1f, TableZ);
            top.transform.localScale = new Vector3(4.0f, 0.2f, 1.6f);
            SetLayer(top, "Ground", 8);
            ApplyColor(top, new Color(0.70f, 0.45f, 0.15f)); // 나무색

            // 다리 4개
            CreateLeg("TableLeg_FL", -1.8f,  TableZ - 0.6f);
            CreateLeg("TableLeg_FR",  1.8f,  TableZ - 0.6f);
            CreateLeg("TableLeg_BL", -1.8f,  TableZ + 0.6f);
            CreateLeg("TableLeg_BR",  1.8f,  TableZ + 0.6f);
        }

        private static void CreateLeg(string legName, float x, float z)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = legName;
            leg.transform.position   = new Vector3(x, (TableTopY - 0.2f) * 0.5f, z);
            leg.transform.localScale = new Vector3(0.12f, (TableTopY - 0.1f) * 0.5f, 0.12f);
            SetLayer(leg, "Ground", 8);
            ApplyColor(leg, new Color(0.55f, 0.33f, 0.10f));
        }

        // ── Walls ─────────────────────────────────────────────────────────────
        private static void RecreateWalls()
        {
            DestroyNamed("Wall_Left");
            DestroyNamed("Wall_Right");
            DestroyNamed("Wall_Back");

            CreateInvisibleWall("Wall_Left",  new Vector3(-6f, 3f, 4f),  new Vector3(0.3f, 10f, 20f));
            CreateInvisibleWall("Wall_Right", new Vector3( 6f, 3f, 4f),  new Vector3(0.3f, 10f, 20f));
            CreateInvisibleWall("Wall_Back",  new Vector3( 0f, 3f, 12f), new Vector3(20f, 10f, 0.3f));
        }

        private static void CreateInvisibleWall(string wName, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = wName;
            go.transform.position   = pos;
            go.transform.localScale = scale;
            SetLayer(go, "Wall", 9);
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
        }

        // ── Cannon ────────────────────────────────────────────────────────────
        // 캐주얼 스타일 3D 대포
        //   - 바퀴 2개 (좌우, X축)
        //   - 오렌지 마차 프레임
        //   - 금색 링이 달린 검은 포신 (+Z 방향)
        private static void RecreateCannon()
        {
            var cannon = GameObject.Find("Cannon");
            if (cannon == null) cannon = new GameObject("Cannon");
            cannon.transform.position = new Vector3(0f, 0.3f, 0f);
            cannon.transform.rotation = Quaternion.identity;

            // 기존 자식 전부 제거
            for (int i = cannon.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(cannon.transform.GetChild(i).gameObject);

            // ── 팔레트 ──────────────────────────────────────────────
            var cOrange  = new Color(0.95f, 0.45f, 0.05f); // 마차 바디
            var cDark    = new Color(0.92f, 0.35f, 0.02f); // 마차 어두운 면
            var cMetal   = new Color(0.10f, 0.10f, 0.13f); // 포신
            var cGold    = new Color(1.00f, 0.76f, 0.02f); // 금색 링/허브
            var cSilver  = new Color(0.65f, 0.65f, 0.70f); // 머즐
            var cWheel   = new Color(0.28f, 0.13f, 0.03f); // 바퀴
            var cWheelRim= new Color(0.48f, 0.26f, 0.06f); // 바퀴 테두리

            // ── 마차 프레임 ─────────────────────────────────────────
            // 메인 박스
            var frame = MakeChild(cannon, "Frame", PrimitiveType.Cube,
                new Vector3(0f, -0.04f, 0.18f),
                new Vector3(0.70f, 0.38f, 0.72f),
                Quaternion.identity, cOrange);
            // 앞면 경사 강조
            MakeChild(cannon, "FrameFront", PrimitiveType.Cube,
                new Vector3(0f, -0.04f, 0.52f),
                new Vector3(0.64f, 0.30f, 0.10f),
                Quaternion.identity, cDark);
            // 포신 받침대 (U자형 트러니언)
            MakeChild(cannon, "Trunnion", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 0.20f),
                new Vector3(0.40f, 0.12f, 0.40f),
                Quaternion.identity, cDark);

            // ── 바퀴 (좌/우) ─────────────────────────────────────────
            // 바퀴: cylinder 기본이 +Y → Euler(0,0,90)으로 +X 방향 축
            var wRot = Quaternion.Euler(0f, 0f, 90f);
            // 바퀴 본체
            MakeChild(cannon, "WheelL", PrimitiveType.Cylinder,
                new Vector3(-0.40f, -0.22f, 0.18f),
                new Vector3(0.09f, 0.30f, 0.30f), wRot, cWheel);
            MakeChild(cannon, "WheelR", PrimitiveType.Cylinder,
                new Vector3( 0.40f, -0.22f, 0.18f),
                new Vector3(0.09f, 0.30f, 0.30f), wRot, cWheel);
            // 바퀴 외곽 테두리 링
            MakeChild(cannon, "WheelRimL", PrimitiveType.Cylinder,
                new Vector3(-0.43f, -0.22f, 0.18f),
                new Vector3(0.04f, 0.29f, 0.29f), wRot, cWheelRim);
            MakeChild(cannon, "WheelRimR", PrimitiveType.Cylinder,
                new Vector3( 0.43f, -0.22f, 0.18f),
                new Vector3(0.04f, 0.29f, 0.29f), wRot, cWheelRim);
            // 바퀴 허브 (금색 중심)
            MakeChild(cannon, "WheelHubL", PrimitiveType.Cylinder,
                new Vector3(-0.46f, -0.22f, 0.18f),
                new Vector3(0.03f, 0.12f, 0.12f), wRot, cGold);
            MakeChild(cannon, "WheelHubR", PrimitiveType.Cylinder,
                new Vector3( 0.46f, -0.22f, 0.18f),
                new Vector3(0.03f, 0.12f, 0.12f), wRot, cGold);

            // ── 포신 (+Z 방향) ────────────────────────────────────────
            // Euler(90,0,0): cylinder +Y → +Z
            var bRot = Quaternion.Euler(90f, 0f, 0f);
            // 포신 본체 (검은 금속)
            MakeChild(cannon, "Barrel", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 0.80f),
                new Vector3(0.17f, 0.60f, 0.17f), bRot, cMetal);
            // 포신 뒤쪽 금색 링
            MakeChild(cannon, "BarrelRing1", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 0.28f),
                new Vector3(0.25f, 0.055f, 0.25f), bRot, cGold);
            // 포신 중간 금색 링
            MakeChild(cannon, "BarrelRing2", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 0.80f),
                new Vector3(0.23f, 0.055f, 0.23f), bRot, cGold);
            // 머즐 (포구) - 약간 넓고 은색
            MakeChild(cannon, "BarrelMuzzle", PrimitiveType.Cylinder,
                new Vector3(0f, 0.15f, 1.38f),
                new Vector3(0.21f, 0.045f, 0.21f), bRot, cSilver);

            // ── FirePoint: 포구 끝 ────────────────────────────────────
            var existingFP = cannon.transform.Find("FirePoint");
            if (existingFP != null) Object.Destroy(existingFP.gameObject);
            var oldFP = GameObject.Find("FirePoint");
            if (oldFP != null) Object.Destroy(oldFP);
            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(cannon.transform);
            fp.transform.localPosition = new Vector3(0f, 0.15f, 1.44f);
            fp.transform.localRotation = Quaternion.identity;
        }

        // 자식 GO 생성 헬퍼 (콜라이더 제거, 색 적용)
        private static GameObject MakeChild(GameObject parent, string childName,
            PrimitiveType shape, Vector3 localPos, Vector3 localScale,
            Quaternion localRot, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = childName;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.transform.localRotation = localRot;
            Object.Destroy(go.GetComponent<Collider>());
            ApplyColor(go, color);
            return go;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static void DestroyNamed(string n)
        {
            var go = GameObject.Find(n);
            if (go != null) Object.Destroy(go);
        }

        private static void SetLayer(GameObject go, string name, int fallback)
        {
            int n = LayerMask.NameToLayer(name);
            go.layer = n >= 0 ? n : fallback;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            rend.material = mat;
        }
    }
}
