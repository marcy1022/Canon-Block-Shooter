using System;
using System.Collections.Generic;
using SmashPuzzle.Destructible;
using UnityEngine;

namespace SmashPuzzle.Projectile
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Cannonball : MonoBehaviour
    {
        [SerializeField] private float baseDamage      = 1f;
        [SerializeField] private float deactivateDelay = 2.0f; // 첫 충돌 후 사라질 때까지
        [SerializeField] private float minFlightTime   = 0.2f;

        public static event Action OnAnyBallReturned;

        private Rigidbody      _rb;
        private SphereCollider _col;
        private Camera         _cam;
        private bool           _active;
        private float          _flightTime;
        private bool           _hasHit;
        // 한 발사에서 이미 맞은 오브젝트 → 중복 대미지 방지
        private readonly HashSet<GameObject> _hitObjects = new();

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<SphereCollider>();
            _col.radius = 0.5f;  // 시각적 구체와 일치 → 더 넓은 히트박스
            int pL = LayerMask.NameToLayer("Projectile");
            gameObject.layer = pL >= 0 ? pL : 6;
            _cam = Camera.main;
        }

        public void Launch(Vector3 direction, float force)
        {
            _hasHit     = false;
            _active     = true;
            _flightTime = 0f;
            _hitObjects.Clear();
            _rb.isKinematic    = false;
            _rb.linearVelocity  = direction.normalized * force;
            _rb.angularVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (!_active) return;
            _flightTime += Time.deltaTime;
            if (_flightTime < minFlightTime) return;

            if (_cam != null)
            {
                Vector3 vp = _cam.WorldToViewportPoint(transform.position);
                if (vp.x < -0.2f || vp.x > 1.2f || vp.y < -0.5f)
                    ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision col)
        {
            if (!_active) return;
            if (_hitObjects.Contains(col.gameObject)) return;
            _hitObjects.Add(col.gameObject);

            // 맞은 오브젝트에 대미지 (여러 오브젝트 연쇄 가능)
            if (col.gameObject.TryGetComponent<IDestructible>(out var target))
                target.TakeDamage(baseDamage);

            // 첫 충돌 시 타이머 시작 — 그 후에도 물리는 계속 작동
            if (!_hasHit)
            {
                _hasHit = true;
                Invoke(nameof(ReturnToPool), deactivateDelay);
            }
        }

        public void ReturnToPool()
        {
            CancelInvoke(nameof(ReturnToPool));
            _active = false;
            // velocity 를 0 으로 만들지 않고 kinematic 으로 전환 → 갑작스러운 중단 없음
            _rb.isKinematic = true;
            gameObject.SetActive(false);
            OnAnyBallReturned?.Invoke();
        }
    }
}
