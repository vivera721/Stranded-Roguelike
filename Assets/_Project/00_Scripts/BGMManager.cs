using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private static float rememberedBaseVolume = 1f;
    private static bool hasRememberedBaseVolume;

    [Header("BGM")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    private float baseAudioSourceVolume = 1f;
    private bool hasCapturedBaseVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSharedVolume()
    {
        rememberedBaseVolume = 1f;
        hasRememberedBaseVolume = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A duplicate BGMManager was disabled.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        GameAudioSettings.Changed -= ApplyAudioSettings;
        GameAudioSettings.Changed += ApplyAudioSettings;
        ApplyAudioSettings();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        GameAudioSettings.Changed -= ApplyAudioSettings;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Play()
    {
        if (audioSource == null)
        {
            return;
        }

        if (backgroundMusic != null && audioSource.clip != backgroundMusic)
        {
            audioSource.clip = backgroundMusic;
        }

        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void Play(AudioClip music)
    {
        if (music == null)
        {
            return;
        }

        backgroundMusic = music;

        if (audioSource == null)
        {
            ConfigureAudioSource();
            ApplyAudioSettings();
        }

        audioSource.clip = music;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource?.Stop();
    }

    public void SetVolume(float value)
    {
        GameAudioSettings.SetBgmVolume(value);
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
            if (!hasRememberedBaseVolume)
            {
                rememberedBaseVolume = Mathf.Clamp01(audioSource.volume);
                hasRememberedBaseVolume = true;
            }

            baseAudioSourceVolume = rememberedBaseVolume;
            hasCapturedBaseVolume = true;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f;

        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
        }
    }

    private void ApplyAudioSettings()
    {
        if (audioSource != null)
        {
            audioSource.volume = baseAudioSourceVolume * GameAudioSettings.EffectiveBgmVolume;
        }
    }
}
