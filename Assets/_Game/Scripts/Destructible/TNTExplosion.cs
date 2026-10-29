using System.Collections;
using UnityEngine;

namespace SmashPuzzle.Destructible
{
    [RequireComponent(typeof(DestructibleObject))]
    public class TNTExplosion : MonoBehaviour
    {
        [SerializeField] private float     explosionRadius = 2f;
        [SerializeField] private float     explosionDamage = 3f;
        [SerializeField] private float     explosionForce  = 400f;  // 물리 폭발력
        [SerializeField] private float     explosionDelay  = 0.05f;
        [SerializeField] private LayerMask destructibleMask;

        public void SetDestructibleMask(LayerMask mask) => destructibleMask = mask;

        private void Start()
        {
            // destructibleMask 미설정 시 자동 지정
            if (destructibleMask == 0)
                destructibleMask = 1 << LayerMask.NameToLayer("Destructible");

            GetComponent<DestructibleObject>().OnDestroyed += OnSelfDestroyed;
        }

        private void OnSelfDestroyed(DestructibleObject _)
        {
            StartCoroutine(ExplodeDelayed());
        }

        private IEnumerator ExplodeDelayed()
        {
            yield return new WaitForSeconds(explosionDelay);

            Vector3 origin = transform.position;

            // 물리 폭발력: 범위 내 모든 Rigidbody에 적용 (레이어 무관)
            Collider[] allHits = Physics.OverlapSphere(origin, explosionRadius);
            foreach (var col in allHits)
            {
                if (col.TryGetComponent<Rigidbody>(out var rb))
                    rb.AddExplosionForce(explosionForce, origin, explosionRadius, 0.3f, ForceMode.Impulse);
            }

            // 데미지: Destructible 오브젝트에만 적용
            Collider[] destHits = Physics.OverlapSphere(origin, explosionRadius, destructibleMask);
            foreach (var hit in destHits)
            {
                if (hit.TryGetComponent<IDestructible>(out var target))
                    target.TakeDamage(explosionDamage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
