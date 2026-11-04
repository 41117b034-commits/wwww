using UnityEngine;

public static class GameAudioSettings
{
    public const int MinimumLevel = 0;
    public const int MaximumLevel = 10;
    public const int DefaultLevel = 10;

    private const string VolumePreferenceKey = "WusheEvent.MasterVolume";
    private const string ExplicitPreferenceKey = "WusheEvent.MasterVolumeChosen";

    public static int Level
    {
        get
        {
            int storedLevel = PlayerPrefs.GetInt(VolumePreferenceKey, DefaultLevel);
            // Old versions saved 5 merely by opening the game. Treat that old
            // default as unchosen; a new explicit choice of 5 is preserved.
            if(!PlayerPrefs.HasKey(ExplicitPreferenceKey)&&storedLevel==5)return DefaultLevel;
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
        PlayerPrefs.SetInt(ExplicitPreferenceKey, 1);
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
