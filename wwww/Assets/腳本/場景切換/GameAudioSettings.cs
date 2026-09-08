using UnityEngine;

public static class GameAudioSettings
{
    public const int MinimumLevel = 0;
    public const int MaximumLevel = 10;
    public const int DefaultLevel = 5;

    private const string VolumePreferenceKey = "WusheEvent.MasterVolume";

    public static int Level
    {
        get
        {
            if (!PlayerPrefs.HasKey(VolumePreferenceKey))
            {
                PlayerPrefs.SetInt(VolumePreferenceKey, DefaultLevel);
                PlayerPrefs.Save();
            }

            int storedLevel = PlayerPrefs.GetInt(VolumePreferenceKey, DefaultLevel);
            return Mathf.Clamp(storedLevel, MinimumLevel, MaximumLevel);
        }
    }

    public static float NormalizedVolume
    {
        get { return Level / (float)MaximumLevel; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnGameStart()
    {
        Apply();
    }

    public static int Increase()
    {
        return SetLevel(Level + 1);
    }

    public static int Decrease()
    {
        return SetLevel(Level - 1);
    }

    public static int SetLevel(int level)
    {
        int clampedLevel = Mathf.Clamp(level, MinimumLevel, MaximumLevel);
        PlayerPrefs.SetInt(VolumePreferenceKey, clampedLevel);
        PlayerPrefs.Save();
        Apply();
        return clampedLevel;
    }

    public static void Apply()
    {
        AudioListener.pause = false;
        AudioListener.volume = NormalizedVolume;
    }
}
