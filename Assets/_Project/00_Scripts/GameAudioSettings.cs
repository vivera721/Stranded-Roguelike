using System;
using UnityEngine;

public static class GameAudioSettings
{
    private const string MutedKey = "Audio.Muted";
    private const string BgmVolumeKey = "Audio.BGMVolume";
    private const string SfxVolumeKey = "Audio.SFXVolume";

    private static bool isLoaded;
    private static bool isMuted;
    private static float bgmVolume = 1f;
    private static float sfxVolume = 1f;

    public static event Action Changed;

    public static bool IsMuted
    {
        get
        {
            EnsureLoaded();
            return isMuted;
        }
    }

    public static float BgmVolume
    {
        get
        {
            EnsureLoaded();
            return bgmVolume;
        }
    }

    public static float SfxVolume
    {
        get
        {
            EnsureLoaded();
            return sfxVolume;
        }
    }

    public static float EffectiveBgmVolume => IsMuted ? 0f : BgmVolume;
    public static float EffectiveSfxVolume => IsMuted ? 0f : SfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isLoaded = false;
        isMuted = false;
        bgmVolume = 1f;
        sfxVolume = 1f;
        Changed = null;
    }

    public static void SetMuted(bool value)
    {
        EnsureLoaded();

        if (isMuted == value)
        {
            return;
        }

        isMuted = value;
        Save();
        Changed?.Invoke();
    }

    public static void SetBgmVolume(float value)
    {
        EnsureLoaded();
        value = Mathf.Clamp01(value);

        if (Mathf.Approximately(bgmVolume, value))
        {
            return;
        }

        bgmVolume = value;
        Save();
        Changed?.Invoke();
    }

    public static void SetSfxVolume(float value)
    {
        EnsureLoaded();
        value = Mathf.Clamp01(value);

        if (Mathf.Approximately(sfxVolume, value))
        {
            return;
        }

        sfxVolume = value;
        Save();
        Changed?.Invoke();
    }

    public static void SetVolumesAndUnmute(float newBgmVolume, float newSfxVolume)
    {
        EnsureLoaded();

        bgmVolume = Mathf.Clamp01(newBgmVolume);
        sfxVolume = Mathf.Clamp01(newSfxVolume);
        isMuted = false;

        Save();
        Changed?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(MutedKey, isMuted ? 1 : 0);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }
}
