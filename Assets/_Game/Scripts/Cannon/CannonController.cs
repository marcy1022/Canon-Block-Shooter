using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SmashPuzzle.Level;

namespace SmashPuzzle.Cannon
{
    public class CannonController : MonoBehaviour
    {
        [Header("Horizontal Aim")]
        [SerializeField] private float minYaw = -40f;
        [SerializeField] private float maxYaw =  40f;

        [Header("Power")]
        [SerializeField] public float minForce  = 12f;
        [SerializeField] public float maxForce  = 22f;
        [SerializeField] private float cycleTime = 1.0f; // 최소→최대→최소 한 사이클(초)

        // CurrentAngle = horizontal yaw (0=forward, negative=left, positive=right)
        public float CurrentAngle { get; private set; } = 0f;
        public float CurrentForce { get; private set; }
        public bool  IsDragging   { get; private set; }

        public static event Action<float, float> OnFireRequested;

        private Camera _cam;
        private bool   _locked;
        private float  _holdTime;

        private void Awake()
        {
            _cam = Camera.main;
            CurrentForce = minForce;
            CurrentAngle = 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        public void SetLocked(bool locked) => _locked = locked;

        private void Update()
        {
            // 레벨 클리어 후 모든 입력 차단
            if (LevelManager.Instance != null && LevelManager.Instance.LevelCleared)
            {
                IsDragging = false;
                return;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count > 0)
            {
                var touch = touchscreen.touches[0];
                var phase = touch.phase.ReadValue();
                var pos   = touch.position.ReadValue();
                switch (phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        BeginHold(pos); break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        UpdateAim(pos); break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        DoFire(pos); break;
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        IsDragging = false; break;
                }
            }
            else
            {
                HandleMouse();
            }
        }

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();

            // 항상 커서 방향으로 수평 조준
            if (!_locked) AimAtScreen(pos);

            if (mouse.leftButton.wasPressedThisFrame)
                BeginHold(pos);
            else if (mouse.leftButton.isPressed && IsDragging)
            {
                _holdTime += Time.deltaTime;
                // PingPong: 최소→최대→최소→최대... 반복 (골프 파워미터 방식)
                float t = Mathf.PingPong(_holdTime / cycleTime, 1f);
                CurrentForce = Mathf.Lerp(minForce, maxForce, t);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && IsDragging)
                DoFire(pos);
        }

        private void BeginHold(Vector2 pos)
        {
            if (_locked) return;
            IsDragging   = true;
            _holdTime    = 0f;
            CurrentForce = minForce;
            AimAtScreen(pos);
        }

        private void UpdateAim(Vector2 pos)
        {
            if (!_locked) AimAtScreen(pos);
        }

        private void DoFire(Vector2 pos)
        {
            if (!IsDragging || _locked) return;
            IsDragging = false;
            AimAtScreen(pos);
            if (_holdTime >= 0.05f)
                OnFireRequested?.Invoke(CurrentAngle, CurrentForce);
        }

        // 마우스 X 위치 → 수평 회전 (Y축), 앵글 범위: minYaw~maxYaw
        private void AimAtScreen(Vector2 screenPos)
        {
            float nx = (screenPos.x / Screen.width) * 2f - 1f; // -1 to +1
            CurrentAngle = Mathf.Clamp(nx * maxYaw, minYaw, maxYaw);
            transform.rotation = Quaternion.Euler(0f, CurrentAngle, 0f);
        }
    }
}
