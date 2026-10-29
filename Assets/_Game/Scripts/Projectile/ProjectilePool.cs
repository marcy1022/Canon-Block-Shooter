using System.Collections.Generic;
using UnityEngine;

namespace SmashPuzzle.Projectile
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [SerializeField] private int poolSize = 5;

        private Queue<Cannonball> _pool = new();

        // 레이어 이름→번호 안전 조회 (미등록 시 하드코딩 폴백)
        private static int Layer(string name, int fallback)
        {
            int n = LayerMask.NameToLayer(name);
            return n >= 0 ? n : fallback;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            for (int i = 0; i < poolSize; i++)
            {
                var ball = CreateCannonball();
                ball.gameObject.SetActive(false);
                _pool.Enqueue(ball);
            }
            Debug.Log($"[ProjectilePool] Initialized {poolSize} cannonballs.");
        }

        public Cannonball Get(Vector3 position)
        {
            Cannonball ball = _pool.Count > 0 ? _pool.Dequeue() : CreateCannonball();
            ball.transform.position = position;
            ball.gameObject.SetActive(true);
            return ball;
        }

        public void Return(Cannonball ball)
        {
            ball.gameObject.SetActive(false);
            _pool.Enqueue(ball);
        }

        private Cannonball CreateCannonball()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Cannonball";
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one * 0.3f;
            go.layer = Layer("Projectile", 6);

            var col = go.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.radius = 0.5f; // Cannonball.Awake에서 0.15로 재설정됨
                var mat = new PhysicsMaterial("PM_Cannonball")
                {
                    bounciness      = 0.6f,
                    dynamicFriction = 0.05f,
                    staticFriction  = 0.05f,
                    bounceCombine   = PhysicsMaterialCombine.Maximum,  // 두 물체 중 높은 쪽 사용
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                };
                col.material = mat;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass           = 3f; // 무거운 포탄 — 블록에 충분한 모멘텀 전달
            rb.linearDamping  = 0f;
            rb.angularDamping = 0.05f;
            rb.constraints    = RigidbodyConstraints.None; // 3D 자유 운동
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = true;

            return go.AddComponent<Cannonball>();
        }
    }
}
