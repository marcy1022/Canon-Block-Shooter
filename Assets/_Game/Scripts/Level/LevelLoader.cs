using System.Collections.Generic;
using SmashPuzzle.Data;
using SmashPuzzle.Destructible;
using UnityEngine;

namespace SmashPuzzle.Level
{
    // Resources/Levels/level_NNN.json 을 로드해 오브젝트를 코드로 생성.
    // Unity 6에서 수동 YAML 프리팹 임포트 불안정 문제를 우회.
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        [Header("Spawn Parent (optional — auto-finds 'Structures' GO)")]
        [SerializeField] private Transform spawnRoot;

        public LevelJsonData CurrentLevelData { get; private set; }

        private readonly Dictionary<int, StructureGroup> _groups = new();
        private readonly List<GameObject>                _spawnedObjects = new();

        // 레이어 안전 조회
        private static int Layer(string name, int fallback)
        {
            int n = LayerMask.NameToLayer(name);
            return n >= 0 ? n : fallback;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (spawnRoot == null)
            {
                var go = GameObject.Find("Structures");
                spawnRoot = go != null ? go.transform : transform;
            }
        }

        public bool LoadLevel(int levelId)
        {
            StopAllCoroutines();
            ClearLevel();

            string path = $"Levels/level_{levelId:D3}";
            TextAsset json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError($"[LevelLoader] JSON not found: Resources/{path}");
                return false;
            }

            CurrentLevelData = JsonUtility.FromJson<LevelJsonData>(json.text);
            if (CurrentLevelData == null || CurrentLevelData.objects == null)
            {
                Debug.LogError($"[LevelLoader] Failed to parse: {path}");
                return false;
            }

            // 블록을 Kinematic으로 스폰 → 물리 충돌 없이 정위치에 배치
            SpawnObjects(CurrentLevelData);
            // 2 FixedUpdate 후 물리 활성화 (이전 블록 Destroy 완료 + 스폰 안정)
            StartCoroutine(EnablePhysicsAfterDelay());
            return true;
        }

        private System.Collections.IEnumerator EnablePhysicsAfterDelay()
        {
            yield return new UnityEngine.WaitForFixedUpdate();
            yield return new UnityEngine.WaitForFixedUpdate();
            foreach (var go in _spawnedObjects)
            {
                if (go == null) continue;
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
            }
        }

        // JSON의 x/y(2D 지면 기준)를 3D 테이블 위 위치로 변환
        // Y 오프셋: 테이블 윗면(1.3) - 구 지면 상면(-5.65) = 6.95
        private const float TABLE_Z    = 4.0f;
        private const float Y_OFFSET   = 6.95f;

        private void SpawnObjects(LevelJsonData data)
        {
            _groups.Clear();

            foreach (var obj in data.objects)
            {
                Vector3    pos = new Vector3(obj.x, obj.y + Y_OFFSET, TABLE_Z + obj.z);
                Quaternion rot = Quaternion.Euler(0f, 0f, obj.rotation);
                GameObject go  = CreateDestructible(obj.type, pos, rot);
                if (go == null) continue;

                _spawnedObjects.Add(go);

                if (go.TryGetComponent<DestructibleObject>(out var dObj))
                {
                    dObj.SetTarget(obj.isTarget);
                    // 초기화 순서와 무관하게 항상 등록되도록 직접 호출
                    LevelManager.Instance?.RegisterDestructible(dObj);

                    if (obj.groupId >= 0)
                    {
                        if (!_groups.TryGetValue(obj.groupId, out var group))
                        {
                            var groupGo = new GameObject($"StructureGroup_{obj.groupId}");
                            groupGo.transform.SetParent(spawnRoot);
                            group = groupGo.AddComponent<StructureGroup>();
                            _groups[obj.groupId] = group;
                        }
                        group.RegisterMember(dObj);
                    }
                }
            }
        }

        // 오브젝트 타입별 코드 생성
        private GameObject CreateDestructible(string type, Vector3 pos, Quaternion rot)
        {
            GameObject go;
            switch (type)
            {
                case "Jar":
                    go = CreateBase(PrimitiveType.Sphere, pos, rot, new Vector3(0.5f, 0.5f, 0.5f), "Destructible", 7);
                    SetupDestructible(go, ObjectType.Jar, 1f, false, 0.4f, 0.8f);
                    ApplyColor(go, new Color(0.9f, 0.7f, 0.3f));
                    break;
                case "Stone":
                    go = CreateBase(PrimitiveType.Cube, pos, rot, new Vector3(0.5f, 0.5f, 0.5f), "Destructible", 7);
                    SetupDestructible(go, ObjectType.Stone, 3f, false, 2f, 0.8f);
                    ApplyColor(go, new Color(0.6f, 0.6f, 0.6f));
                    break;
                case "Pillar":
                    go = CreateBase(PrimitiveType.Cube, pos, rot, new Vector3(0.4f, 1.2f, 0.4f), "Destructible", 7);
                    SetupDestructible(go, ObjectType.Pillar, 5f, false, 4f, 0.8f);
                    ApplyColor(go, new Color(0.75f, 0.65f, 0.50f)); // 연한 돌 색
                    break;
                case "Wood":
                    // 얇은 직사각 도미노 형태 — 쓰러졌을 때 옆 블록을 연쇄로 맞춤
                    go = CreateBase(PrimitiveType.Cube, pos, rot, new Vector3(0.35f, 1.0f, 0.35f), "Destructible", 7);
                    SetupDestructible(go, ObjectType.Wood, 2f, false, 0.25f, 0.8f);
                    ApplyColor(go, new Color(0.72f, 0.50f, 0.28f)); // 나무 갈색
                    break;
                case "TNT":
                    go = CreateBase(PrimitiveType.Cube, pos, rot, new Vector3(0.6f, 0.6f, 0.5f), "Destructible", 7);
                    SetupDestructible(go, ObjectType.TNT, 1f, false, 0.3f, 0.05f);
                    ApplyColor(go, new Color(0.9f, 0.2f, 0.1f));
                    var tnt = go.AddComponent<TNTExplosion>();
                    tnt.SetDestructibleMask(1 << Layer("Destructible", 7));
                    break;
                default:
                    Debug.LogWarning($"[LevelLoader] Unknown type: {type}");
                    return null;
            }
            go.name = type;
            return go;
        }

        private static GameObject CreateBase(PrimitiveType shape, Vector3 pos, Quaternion rot,
                                             Vector3 scale, string layerName, int layerFallback)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            go.layer = Layer(layerName, layerFallback);
            return go;
        }

        private static void SetupDestructible(GameObject go, ObjectType type,
                                              float maxHealth, bool isKinematic,
                                              float mass, float drag)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass           = mass;
            rb.linearDamping  = 0.15f;
            rb.angularDamping = 0.30f;
            // 항상 Kinematic으로 시작 — EnablePhysicsAfterDelay 코루틴이 해제
            rb.isKinematic    = true;
            rb.constraints    = RigidbodyConstraints.None;

            // 물리 재질: 마찰·탄성으로 현실감 향상
            var col = go.GetComponent<Collider>();
            if (col != null) col.material = MakePhysicsMat(type);

            var d = go.AddComponent<DestructibleObject>();
            d.SetupRuntime(type, maxHealth);
        }

        private static PhysicsMaterial MakePhysicsMat(ObjectType type)
        {
            float friction, bounce;
            switch (type)
            {
                case ObjectType.Stone:
                    friction = 0.70f; bounce = 0.05f; break;
                case ObjectType.Pillar:
                    friction = 0.60f; bounce = 0.08f; break;
                case ObjectType.Wood:
                    // 블록 간 연쇄 쓰러짐이 잘 되도록 마찰 낮춤
                    friction = 0.30f; bounce = 0.15f; break;
                case ObjectType.Jar:
                    friction = 0.40f; bounce = 0.20f; break;
                default:
                    friction = 0.50f; bounce = 0.10f; break;
            }
            var mat = new PhysicsMaterial($"PM_{type}")
            {
                dynamicFriction = friction,
                staticFriction  = friction * 1.2f,
                bounciness      = bounce,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine   = PhysicsMaterialCombine.Minimum,
            };
            return mat;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            rend.material = mat;
        }

        public void ClearLevel()
        {
            // DestroyImmediate: 즉시 제거 → 새 블록 스폰 시 이전 블록과 물리 충돌 없음
            foreach (var go in _spawnedObjects)
                if (go != null) DestroyImmediate(go);
            _spawnedObjects.Clear();
            _groups.Clear();
            CurrentLevelData = null;
        }
    }
}
