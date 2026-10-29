using UnityEngine;
using UnityEngine.Events;
using SmashPuzzle.Data;

namespace SmashPuzzle.Projectile
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class ProjectileBase : MonoBehaviour
    {
        [SerializeField] private GameData gameData;

        public static readonly UnityEvent<ProjectileBase, Collision> OnHit = new();

        private Rigidbody _rb;
        private float _lifetime;
        private bool  _launched;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            { int _pL = LayerMask.NameToLayer("Projectile"); gameObject.layer = _pL >= 0 ? _pL : 6; }
        }

        public void Launch(Vector3 direction, float power)
        {
            if (_launched) return;
            _launched = true;
            _lifetime = 0f;
            _rb.isKinematic = false;
            _rb.AddForce(direction.normalized * (gameData.MaxLaunchForce * power), ForceMode.Impulse);
        }

        private void Update()
        {
            if (!_launched) return;
            _lifetime += Time.deltaTime;
            if (_lifetime >= gameData.ProjectileLifetime)
                ReturnToPool();
        }

        private void OnCollisionEnter(Collision col)
        {
            if (!_launched) return;
            OnHit.Invoke(this, col);
        }

        public void ReturnToPool()
        {
            _launched = false;
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            gameObject.SetActive(false);
        }

        private void OnDestroy() => OnHit.RemoveAllListeners();
    }
}
