using System.Collections;
using System.Collections.Generic;
using SmashPuzzle.Cannon;
using UnityEngine;

namespace SmashPuzzle.Destructible
{
    // 하나의 구조물 단위(예: 기둥+위에 올려진 블록들)를 묶어 관리.
    // 그룹 내 Pillar가 파괴되면 나머지 오브젝트도 Dynamic으로 전환해 자연스러운 붕괴를 만든다.
    public class StructureGroup : MonoBehaviour
    {
        [SerializeField] private List<DestructibleObject> members = new();

        // Stuck 감지: 발사 이후 3초 이상 정지 상태이면서 미파괴 오브젝트를 강제 파괴
        private const float StuckCheckInterval    = 3f;
        private const float StuckVelocityThreshold = 0.05f;

        // 발사가 한 번이라도 있었는지 추적 — 스폰 직후 정지 오브젝트를 잘못 파괴하는 것을 방지
        private bool _gameStarted;

        private void Start()
        {
            foreach (var member in members)
            {
                if (member != null)
                    member.OnDestroyed += OnMemberDestroyed;
            }
            StartCoroutine(StuckDetectionLoop());
        }

        private void OnEnable()  => CannonController.OnFireRequested += OnFirstFire;
        private void OnDisable() => CannonController.OnFireRequested -= OnFirstFire;

        private void OnFirstFire(float _, float __)
        {
            _gameStarted = true;
            CannonController.OnFireRequested -= OnFirstFire;
        }

        private IEnumerator StuckDetectionLoop()
        {
            // Stuck 감지 비활성화 — 정지한 오브젝트가 즉사하는 버그 방지
            // 나무/기둥이 테이블 위에서 쓰러져 정지해도 사라지지 않아야 함
            yield break;
        }

        private void OnMemberDestroyed(DestructibleObject destroyed)
        {
            if (destroyed.Type != ObjectType.Pillar) return;

            // 기둥 파괴 시 그룹 내 모든 Kinematic 해제 → 물리 붕괴
            foreach (var member in members)
            {
                if (member != null && !member.IsDestroyed)
                    member.ReleaseKinematic();
            }
        }

        private void OnDestroy()
        {
            foreach (var member in members)
            {
                if (member != null)
                    member.OnDestroyed -= OnMemberDestroyed;
            }
        }

        public void RegisterMember(DestructibleObject obj)
        {
            if (!members.Contains(obj))
            {
                members.Add(obj);
                obj.OnDestroyed += OnMemberDestroyed;
            }
        }
    }
}
