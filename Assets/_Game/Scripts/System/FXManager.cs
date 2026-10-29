using System.Collections.Generic;
using SmashPuzzle.Destructible;
using UnityEngine;

namespace SmashPuzzle.Core
{
    public class FXManager : MonoBehaviour
    {
        public static FXManager Instance { get; private set; }

        [Header("Destroy FX Prefabs")]
        [SerializeField] private ParticleSystem fxJar;
        [SerializeField] private ParticleSystem fxWood;
        [SerializeField] private ParticleSystem fxStone;
        [SerializeField] private ParticleSystem fxTNT;
        [SerializeField] private ParticleSystem fxHit;

        [Header("Clear FX")]
        [SerializeField] private ParticleSystem fxConfetti;
        [SerializeField] private int            maxParticlesPerSystem = 50;

        private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void PlayDestroyFX(ObjectType type, Vector3 position)
        {
            var prefab = type switch
            {
                ObjectType.Jar    => fxJar,
                ObjectType.Wood   => fxWood,
                ObjectType.Stone  => fxStone,
                ObjectType.Pillar => fxStone,
                ObjectType.TNT    => fxTNT,
                _                 => null,
            };
            PlayFX(prefab, position);
        }

        public void PlayHitFX(Vector3 position) => PlayFX(fxHit, position);

        public void PlayConfetti(Vector3 position) => PlayFX(fxConfetti, position);

        private void PlayFX(ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null) return;

            var ps = GetFromPool(prefab);
            ps.transform.position = position;
            ps.transform.rotation = Quaternion.identity;

            var main = ps.main;
            if (main.maxParticles > maxParticlesPerSystem)
            {
                var m = ps.main;
                m.maxParticles = maxParticlesPerSystem;
            }

            ps.gameObject.SetActive(true);
            ps.Play();

            StartCoroutine(ReturnToPoolAfter(ps, prefab, ps.main.duration + ps.main.startLifetime.constantMax));
        }

        private ParticleSystem GetFromPool(ParticleSystem prefab)
        {
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<ParticleSystem>();
                _pools[prefab] = queue;
            }

            if (queue.Count > 0)
            {
                var pooled = queue.Dequeue();
                if (pooled != null) return pooled;
            }

            return Instantiate(prefab, transform);
        }

        private global::System.Collections.IEnumerator ReturnToPoolAfter(
            ParticleSystem ps, ParticleSystem prefab, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                if (_pools.TryGetValue(prefab, out var q))
                    q.Enqueue(ps);
            }
        }
    }
}
