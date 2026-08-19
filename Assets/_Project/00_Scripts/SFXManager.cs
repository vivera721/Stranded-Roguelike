using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    [DisallowMultipleComponent]
    public sealed class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("Enemy Hit SFX")]
        [SerializeField] private List<AudioClip> enemyHitSoundEffects = new List<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float enemyHitVolume = 0.35f;
        [SerializeField, Min(0f)] private float minimumHitInterval = 0.03f;

        [Header("Enemy Death SFX")]
        [SerializeField] private List<AudioClip> insectDeathSoundEffects = new List<AudioClip>();
        [SerializeField] private List<AudioClip> humanoidDeathSoundEffects = new List<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float enemyDeathVolume = 0.5f;
        [SerializeField, Min(0f)] private float minimumDeathInterval = 0.04f;

        private float nextHitSoundTime;
        private float nextDeathSoundTime;
        private float baseAudioSourceVolume = 1f;
        private bool hasCapturedBaseVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("A duplicate SFXManager was disabled.", this);
                enabled = false;
                return;
            }

            Instance = this;
            ConfigureAudioSource();
            ApplyAudioSettings();
            PreloadClips(enemyHitSoundEffects);
            PreloadClips(insectDeathSoundEffects);
            PreloadClips(humanoidDeathSoundEffects);
        }

        private void OnEnable()
        {
            SurvivorDamageEvents.EnemyDamaged -= OnEnemyDamaged;
            SurvivorDamageEvents.EnemyDamaged += OnEnemyDamaged;
            SurvivorDamageEvents.EnemyDied -= OnEnemyDied;
            SurvivorDamageEvents.EnemyDied += OnEnemyDied;
            GameAudioSettings.Changed -= ApplyAudioSettings;
            GameAudioSettings.Changed += ApplyAudioSettings;
            ApplyAudioSettings();
        }

        private void OnDisable()
        {
            SurvivorDamageEvents.EnemyDamaged -= OnEnemyDamaged;
            SurvivorDamageEvents.EnemyDied -= OnEnemyDied;
            GameAudioSettings.Changed -= ApplyAudioSettings;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnemyDamaged(
            EnemyHealth enemy,
            int damage,
            Vector2 hitOrigin,
            SurvivorDamageKind damageKind)
        {
            if (enemy == null
                || damage <= 0
                || damageKind == SurvivorDamageKind.StatusTick
                || Time.unscaledTime < nextHitSoundTime)
            {
                return;
            }

            if (PlayRandomClip(enemyHitSoundEffects, enemyHitVolume))
            {
                nextHitSoundTime = Time.unscaledTime + minimumHitInterval;
            }
        }

        private void OnEnemyDied(EnemyHealth enemy)
        {
            if (enemy == null
                || enemy.CompareTag("Boss")
                || Time.unscaledTime < nextDeathSoundTime)
            {
                return;
            }

            SurvivorEnemy survivorEnemy = enemy.GetComponent<SurvivorEnemy>();
            SurvivorEnemySfxType sfxType = survivorEnemy != null
                ? survivorEnemy.SfxType
                : InferSfxTypeFromName(enemy.name);

            List<AudioClip> deathClips = sfxType == SurvivorEnemySfxType.Insect
                ? insectDeathSoundEffects
                : humanoidDeathSoundEffects;

            if (PlayRandomClip(deathClips, enemyDeathVolume))
            {
                nextDeathSoundTime = Time.unscaledTime + minimumDeathInterval;
            }
        }

        [ContextMenu("SFX Test/Play Random Enemy Hit")]
        private void TestEnemyHitSound()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("SFX tests can only be used in Play Mode.", this);
                return;
            }

            PlayRandomClip(enemyHitSoundEffects, enemyHitVolume);
        }

        [ContextMenu("SFX Test/Play Random Insect Death")]
        private void TestInsectDeathSound()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("SFX tests can only be used in Play Mode.", this);
                return;
            }

            PlayRandomClip(insectDeathSoundEffects, enemyDeathVolume);
        }

        [ContextMenu("SFX Test/Play Random Humanoid Death")]
        private void TestHumanoidDeathSound()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("SFX tests can only be used in Play Mode.", this);
                return;
            }

            PlayRandomClip(humanoidDeathSoundEffects, enemyDeathVolume);
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (!hasCapturedBaseVolume)
            {
                baseAudioSourceVolume = Mathf.Clamp01(audioSource.volume);
                hasCapturedBaseVolume = true;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void ApplyAudioSettings()
        {
            if (audioSource != null)
            {
                audioSource.volume = baseAudioSourceVolume * GameAudioSettings.EffectiveSfxVolume;
            }
        }

        private bool PlayRandomClip(List<AudioClip> clips, float volume)
        {
            AudioClip clip = GetRandomClip(clips);
            if (clip == null || audioSource == null)
            {
                return false;
            }

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            return true;
        }

        private static AudioClip GetRandomClip(List<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0)
            {
                return null;
            }

            int startIndex = UnityEngine.Random.Range(0, clips.Count);
            for (int i = 0; i < clips.Count; i++)
            {
                AudioClip clip = clips[(startIndex + i) % clips.Count];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static void PreloadClips(List<AudioClip> clips)
        {
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                clips[i]?.LoadAudioData();
            }
        }

        private static SurvivorEnemySfxType InferSfxTypeFromName(string objectName)
        {
            if (!string.IsNullOrWhiteSpace(objectName)
                && (objectName.IndexOf("Insect", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("Bug", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return SurvivorEnemySfxType.Insect;
            }

            return SurvivorEnemySfxType.Humanoid;
        }
    }
}
