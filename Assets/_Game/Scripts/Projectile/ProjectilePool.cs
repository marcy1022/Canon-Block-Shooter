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
            // 큰 대포알: 블록 모서리도 확실히 히트
            go.transform.localScale = Vector3.one * 0.45f;
            go.layer = Layer("Projectile", 6);

            var col = go.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.radius = 0.5f;
                var mat = new PhysicsMaterial("PM_Cannonball")
                {
                    bounciness      = 0.75f,  // 탄성 높여서 튕김 지속
                    dynamicFriction = 0.25f,  // 마찰 높여서 모서리 타격 시 반사각 커짐
                    staticFriction  = 0.25f,
                    bounceCombine   = PhysicsMaterialCombine.Maximum,
                    frictionCombine = PhysicsMaterialCombine.Average, // 블록 마찰과 평균
                };
                col.material = mat;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass           = 4f; // 크기 커진 만큼 무게도 증가 — 블록 연쇄 타격
            rb.linearDamping  = 0f;
            rb.angularDamping = 0.05f;
            rb.constraints    = RigidbodyConstraints.None; // 3D 자유 운동
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.isKinematic = true;

            return go.AddComponent<Cannonball>();
        }
    }
}
