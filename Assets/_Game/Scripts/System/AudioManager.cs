using SmashPuzzle.Destructible;
using UnityEngine;

namespace SmashPuzzle.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip   bgmGameplay;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip sfxLaunch;
        [SerializeField] private AudioClip sfxHitWood;
        [SerializeField] private AudioClip sfxHitStone;
        [SerializeField] private AudioClip sfxExplosion;
        [SerializeField] private AudioClip sfxCombo;
        [SerializeField] private AudioClip sfxClear;
        [SerializeField] private AudioClip sfxFail;

        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxSource = GetComponent<AudioSource>();
            _sfxSource.loop        = false;
            _sfxSource.playOnAwake = false;

            if (bgmSource == null)
            {
                bgmSource             = gameObject.AddComponent<AudioSource>();
                bgmSource.loop        = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume      = 0.5f;
            }
        }

        private void OnEnable()
        {
            SmashPuzzle.Cannon.CannonController.OnFireRequested += OnFire;
            SmashPuzzle.Level.LevelManager.OnStarRating.AddListener(OnClear);
            SmashPuzzle.Level.LevelManager.OnLevelFail.AddListener(OnFail);
            ComboTracker.OnComboUpdated += OnCombo;
        }

        private void OnDisable()
        {
            SmashPuzzle.Cannon.CannonController.OnFireRequested -= OnFire;
            SmashPuzzle.Level.LevelManager.OnStarRating.RemoveListener(OnClear);
            SmashPuzzle.Level.LevelManager.OnLevelFail.RemoveListener(OnFail);
            ComboTracker.OnComboUpdated -= OnCombo;
        }

        private void OnFire(float _, float __) => PlaySFX(sfxLaunch);
        private void OnClear(int _)            => PlaySFX(sfxClear);
        private void OnFail()                  => PlaySFX(sfxFail);
        private void OnCombo(int combo)        { if (combo >= 2) PlaySFX(sfxCombo); }

        public void PlayBGM(AudioClip clip = null)
        {
            var c = clip ?? bgmGameplay;
            if (c == null || bgmSource == null) return;
            bgmSource.clip = c;
            bgmSource.Play();
        }

        public void StopBGM() => bgmSource?.Stop();

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        public void PlayHitSFX(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Wood:                   PlaySFX(sfxHitWood);   break;
                case ObjectType.Stone:
                case ObjectType.Pillar:                 PlaySFX(sfxHitStone);  break;
                case ObjectType.TNT:                    PlaySFX(sfxExplosion); break;
            }
        }

        public void SetSlowMotionPitch(bool slow)
        {
            if (bgmSource != null) bgmSource.pitch = slow ? 0.7f : 1f;
        }
    }
}
