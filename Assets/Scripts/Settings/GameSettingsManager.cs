using UnityEngine;

public static class GameSettingsManager
{
    public const string MasterVolumeKey = "BonkSurvivor_MasterVolume";
    public const string MouseSensitivityKey = "BonkSurvivor_MouseSensitivity";
    public const string FullscreenKey = "BonkSurvivor_Fullscreen";

    public const float DefaultMasterVolume = 1f;
    public const float DefaultMouseSensitivity = 1f;
    public const float MinMouseSensitivity = 0.5f;
    public const float MaxMouseSensitivity = 2f;
    public const bool DefaultFullscreen = true;

    private static float masterVolume = DefaultMasterVolume;
    private static float mouseSensitivity = DefaultMouseSensitivity;
    private static bool fullscreen = DefaultFullscreen;
    private static bool isLoaded;

    public static float MasterVolume => masterVolume;
    public static float MouseSensitivity => mouseSensitivity;
    public static bool Fullscreen => fullscreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        isLoaded = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LoadOnStartup()
    {
        Load();
        ApplyAll();
    }

    public static void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume);
        mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, DefaultFullscreen ? 1 : 0) == 1;

        masterVolume = Mathf.Clamp01(masterVolume);
        mouseSensitivity = Mathf.Clamp(mouseSensitivity, MinMouseSensitivity, MaxMouseSensitivity);
        isLoaded = true;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetMasterVolume(float value)
    {
        EnsureLoaded();
        masterVolume = Mathf.Clamp01(value);
        AudioListener.volume = masterVolume;
        Save();
    }

    public static void SetMouseSensitivity(float value)
    {
        EnsureLoaded();
        mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        Save();
    }

    public static void SetFullscreen(bool value)
    {
        EnsureLoaded();
        fullscreen = value;
        Screen.fullScreen = fullscreen;
        Save();
    }

    public static void ResetDefaults()
    {
        masterVolume = DefaultMasterVolume;
        mouseSensitivity = DefaultMouseSensitivity;
        fullscreen = DefaultFullscreen;
        ApplyAll();
        Save();
    }

    public static void ApplyAll()
    {
        EnsureLoaded();
        AudioListener.volume = masterVolume;
        Screen.fullScreen = fullscreen;
    }

    private static void EnsureLoaded()
    {
        if (!isLoaded)
        {
            Load();
        }
    }
}
