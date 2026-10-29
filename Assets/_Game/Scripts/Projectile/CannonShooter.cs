using SmashPuzzle.Cannon;
using UnityEngine;

namespace SmashPuzzle.Projectile
{
    public class CannonShooter : MonoBehaviour
    {
        [SerializeField] private Transform    firePoint;
        [SerializeField] private ProjectilePool pool;

        // 수직 발사각 — 파워 게이지에 따라 가변 (낮은 힘=낮은 각, 높은 힘=높은 각)
        private const float LaunchPitchMin = 18f;
        private const float LaunchPitchMax = 42f;

        private CannonController _cannon;

        private void Awake()
        {
            _cannon = GetComponentInParent<CannonController>();
            if (_cannon == null)
                _cannon = FindFirstObjectByType<CannonController>();
        }

        private void Start()
        {
            if (firePoint == null)
            {
                var fp = GameObject.Find("FirePoint");
                if (fp != null) firePoint = fp.transform;
            }
            if (pool == null)
                pool = FindFirstObjectByType<ProjectilePool>();
        }

        private void OnEnable()
        {
            CannonController.OnFireRequested += Fire;
            Cannonball.OnAnyBallReturned     += UnlockCannon;
        }

        private void OnDisable()
        {
            CannonController.OnFireRequested -= Fire;
            Cannonball.OnAnyBallReturned     -= UnlockCannon;
        }

        private void Fire(float yawDeg, float force)
        {
            if (pool == null) return;

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;

            var ball = pool.Get(origin);

            // 수평: 대포 Y축 회전 방향, 수직: 고정 25° 상향
            Vector3 forward;
            if (_cannon != null)
            {
                forward = _cannon.transform.forward; // 3D 수평 방향
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                forward.Normalize();
            }
            else
            {
                float rad = yawDeg * Mathf.Deg2Rad;
                forward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            }

            // 파워 게이지 0→1 비율에 따라 발사각 보간
            float forceT   = (_cannon != null)
                ? Mathf.InverseLerp(_cannon.minForce, _cannon.maxForce, force)
                : 0f;
            float pitchDeg = Mathf.Lerp(LaunchPitchMin, LaunchPitchMax, forceT);
            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            Vector3 dir = (forward * Mathf.Cos(pitchRad) + Vector3.up * Mathf.Sin(pitchRad)).normalized;

            ball.Launch(dir, force);
            _cannon?.SetLocked(true);
        }

        private void UnlockCannon() => _cannon?.SetLocked(false);
    }
}
