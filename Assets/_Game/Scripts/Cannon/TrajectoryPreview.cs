using UnityEngine;

namespace SmashPuzzle.Cannon
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private int       dotCount      = 18;  // 점 개수
        [SerializeField] private float     timeStep      = 0.10f;
        [SerializeField] private float     gapStep       = 0.04f; // 점 사이 간격용 미세 진행
        [SerializeField] private float     dotRadius     = 0.08f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private Transform firePoint;

        private const float LaunchPitchMin = 18f;
        private const float LaunchPitchMax = 42f;

        private LineRenderer     _lr;
        private CannonController _cannon;

        private void Awake()
        {
            _lr    = GetComponent<LineRenderer>();
            _cannon = GetComponentInParent<CannonController>();
            if (_cannon == null)
                _cannon = FindFirstObjectByType<CannonController>();

            SetupLineRenderer();
        }

        private void Start()
        {
            if (firePoint == null)
            {
                var fp = GameObject.Find("FirePoint");
                if (fp != null) firePoint = fp.transform;
            }
        }

        private void SetupLineRenderer()
        {
            // Sprites/Default는 vertex color를 지원 → 그라디언트가 그대로 렌더링됨
            var mat = new Material(Shader.Find("Sprites/Default") ??
                                   Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = Color.white;
            _lr.material      = mat;
            _lr.useWorldSpace = true;
            _lr.numCapVertices    = 4;
            _lr.numCornerVertices = 4;

            // 너비: 시작 두껍게 → 끝으로 갈수록 가늘게
            _lr.startWidth = 0.10f;
            _lr.endWidth   = 0.03f;

            // 색상 그라디언트: 노란색(밝음) → 주황 → 빨간색(투명)
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1.00f, 0.95f, 0.10f), 0.00f), // 밝은 노랑
                    new GradientColorKey(new Color(1.00f, 0.55f, 0.05f), 0.50f), // 주황
                    new GradientColorKey(new Color(1.00f, 0.15f, 0.05f), 1.00f), // 빨강
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.00f),
                    new GradientAlphaKey(0.8f, 0.60f),
                    new GradientAlphaKey(0.0f, 1.00f), // 끝은 투명
                }
            );
            _lr.colorGradient = grad;
            _lr.positionCount = 0;
        }

        private void Update()
        {
            if (_cannon == null) return;
            if (_cannon.IsDragging) ShowTrajectory();
            else                    _lr.positionCount = 0;
        }

        private void ShowTrajectory()
        {
            Vector3 origin = firePoint != null
                ? firePoint.position
                : (_cannon != null
                    ? _cannon.transform.position + Vector3.up * 0.15f
                    : transform.position);

            // CannonShooter와 동일한 발사 방향
            Vector3 forward = _cannon != null ? _cannon.transform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            float forceT   = (_cannon != null)
                ? Mathf.InverseLerp(_cannon.minForce, _cannon.maxForce, _cannon.CurrentForce)
                : 0f;
            float pitchDeg = Mathf.Lerp(LaunchPitchMin, LaunchPitchMax, forceT);
            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            Vector3 dir    = (forward * Mathf.Cos(pitchRad) + Vector3.up * Mathf.Sin(pitchRad)).normalized;
            Vector3 vel    = dir * _cannon.CurrentForce;
            Vector3 gravity = Physics.gravity;

            // 점선 효과: 점(2개 가까운 포인트) + 간격(gapStep 만큼 진행 후 다음 점)
            // positionCount = dotCount * 2
            int maxPts = dotCount * 2;
            _lr.positionCount = maxPts;

            Vector3 pos = origin;
            int idx = 0;

            for (int i = 0; i < dotCount; i++)
            {
                if (idx + 1 >= maxPts) break;

                // 점 앞부분
                _lr.SetPosition(idx, pos);

                // 점 한 개의 길이만큼 미세 진행 (dot 너비)
                Vector3 velTmp = vel;
                velTmp += gravity * gapStep;
                Vector3 dotEnd = pos + velTmp * gapStep;
                _lr.SetPosition(idx + 1, dotEnd);
                idx += 2;

                // 충돌 감지
                if (i > 0 && Physics.CheckSphere(pos, dotRadius, collisionMask))
                {
                    _lr.positionCount = idx;
                    return;
                }

                // 다음 점까지 간격 진행 (timeStep)
                vel += gravity * timeStep;
                pos += vel    * timeStep;
            }

            _lr.positionCount = idx;
        }
    }
}
