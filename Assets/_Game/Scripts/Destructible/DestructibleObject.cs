using System;
using UnityEngine;

namespace SmashPuzzle.Destructible
{
    public enum ObjectType { Jar, Stone, Pillar, Wood, TNT }

    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class DestructibleObject : MonoBehaviour, IDestructible
    {
        [SerializeField] private ObjectType   objectType    = ObjectType.Jar;
        [SerializeField] private float        maxHealth     = 1f;
        [SerializeField] private bool         isTarget      = false;
        [SerializeField] private ParticleSystem destroyParticle;
        [SerializeField] private AudioClip    destroySound;

        public ObjectType Type        => objectType;
        public bool       IsTarget    { get; private set; }
        public bool       IsDestroyed { get; private set; }

        // LevelLoader가 코드로 오브젝트 생성 시 AddComponent 직후 호출.
        // Awake가 이미 실행됐으므로 내부 상태도 함께 재설정.
        public void SetupRuntime(ObjectType type, float health)
        {
            objectType     = type;
            maxHealth      = health;
            _currentHealth = health;

            if (_rb != null)
            {
                _rb.isKinematic    = false;
                _rb.constraints    = RigidbodyConstraints.None; // 3D 자유 물리
                _isPillarKinematic = false;
            }

            int dL = LayerMask.NameToLayer("Destructible");
            gameObject.layer = dL >= 0 ? dL : 7;
        }

        public void SetTarget(bool value)
        {
            IsTarget = value;
            if (value) ApplyTargetColor();
        }

        private void ApplyTargetColor()
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(1f, 0.25f, 0.25f, 1f);
        }

        public event Action<DestructibleObject> OnDestroyed;

        private float     _currentHealth;
        private Rigidbody _rb;
        private AudioSource _audio;
        private bool      _isPillarKinematic;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody>();
            _audio = GetComponent<AudioSource>();
            _currentHealth = maxHealth;
            int dLayer = LayerMask.NameToLayer("Destructible");
            gameObject.layer = dLayer >= 0 ? dLayer : 7;
            if (isTarget) SetTarget(true);
        }

        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            if (_isPillarKinematic)
            {
                _rb.isKinematic    = false;
                _isPillarKinematic = false;
            }

            _currentHealth -= damage;
            if (_currentHealth <= 0f)
                StartCoroutine(DestroySequence());
        }

        private global::System.Collections.IEnumerator DestroySequence()
        {
            if (IsDestroyed) yield break;
            IsDestroyed = true;

            if (destroyParticle != null)
            {
                destroyParticle.transform.SetParent(null);
                destroyParticle.Play();
            }
            if (_audio != null && destroySound != null)
                _audio.PlayOneShot(destroySound);

            // FX & Audio
            SmashPuzzle.Core.FXManager.Instance?.PlayDestroyFX(objectType, transform.position);
            SmashPuzzle.Core.AudioManager.Instance?.PlayHitSFX(objectType);

            OnDestroyed?.Invoke(this);

            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
        }

        public void ReleaseKinematic()
        {
            if (_isPillarKinematic)
            {
                _rb.isKinematic    = false;
                _isPillarKinematic = false;
            }
        }

        public float GetHealthRatio() => _currentHealth / maxHealth;
    }
}
