using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using EditorAttributes;

public class SettingsScript : MonoBehaviour
{
    const string PrefPlayerSpeed = "settings.player.speed";
    const string PrefPlayerVolume = "settings.player.volume";
    const string PrefCameraSensitivity = "settings.player.sensitivity";
    const string PrefCameraFov = "settings.player.fov";
    const string PrefRenderDistance = "settings.player.renderDistance";
    const string PrefGameQuality = "settings.player.quality";

    public GameObject Settings;
    public Button button;

    [Title("Player")]
    public SettingsElement playerSpeedSetting;
    public float playerSpeedMin = 1f;
    public float playerSpeedMax = 10f;
    public SettingsElement playerVolumeSetting;
    public AudioSource playerVolumeAudioSource;
    public float playerVolumeMin = 0f;
    public float playerVolumeMax = 10f;

    [Title("Camera")]
    public SettingsElement cameraSensitivitySetting;
    public float cameraSensitivityMin = 0.05f;
    public float cameraSensitivityMax = 1f;
    public SettingsElement cameraFovSetting;
    public float cameraFovMin = 40f;
    public float cameraFovMax = 120f;

    [Title("Graphics")]
    public SettingsElement renderDistanceSetting;
    public float renderDistanceMin = 100f;
    public float renderDistanceMax = 5000f;
    public TMP_Dropdown qualityDropdown;

    PlayerScript playerScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerScript = GetComponent<PlayerScript>();

        if (button != null)
        {
            button.onClick.AddListener(OnToggleMenu);
            button.gameObject.SetActive(playerScript != null && playerScript.IsUsingMobileControls());
        }

        InitializeSettings();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && Settings != null)
        {
            OnToggleMenu();
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void InitializeSettings()
    {
        if (playerScript == null)
        {
            return;
        }

        BindSetting(playerSpeedSetting, playerSpeedMin, playerSpeedMax, SetPlayerSpeed);
        BindSetting(playerVolumeSetting, playerVolumeMin, playerVolumeMax, SetPlayerVolume);
        BindSetting(cameraSensitivitySetting, cameraSensitivityMin, cameraSensitivityMax, SetPlayerSense);
        BindSetting(cameraFovSetting, cameraFovMin, cameraFovMax, SetFovValue);
        BindSetting(renderDistanceSetting, renderDistanceMin, renderDistanceMax, SetRenderValue);
        BindQualityDropdown();

        LoadSavedSettings();
    }

    void BindSetting(SettingsElement setting, float minValue, float maxValue, UnityEngine.Events.UnityAction<float> callback)
    {
        if (setting == null || setting.slider == null)
        {
            return;
        }

        float clampedMin = Mathf.Min(minValue, maxValue);
        float clampedMax = Mathf.Max(minValue, maxValue);
        setting.slider.minValue = clampedMin;
        setting.slider.maxValue = clampedMax;
        setting.slider.onValueChanged.AddListener(callback);
    }

    void BindQualityDropdown()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.onValueChanged.AddListener(SetGameQuality);
    }

    void OnToggleMenu()
    {
        if (Settings == null)
        {
            return;
        }

        if (Settings.activeSelf == true){
            if (playerScript != null && playerScript.IsUsingMobileControls())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Settings.SetActive(false);
            if (playerScript != null)
            {
                playerScript.SetCanMove(true);
            }
        }
        
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Settings.SetActive(true);
            if (playerScript != null)
            {
                playerScript.SetCanMove(false);
            }
            
        }
    }

    public void SetPlayerSpeed(float speedValue)
    {
        SetPlayerSpeed(speedValue, true);
    }

    void SetPlayerSpeed(float speedValue, bool savePreference)
    {
        if (playerScript == null)
        {
            return;
        }

        float clampedSpeed = ClampToRange(speedValue, playerSpeedMin, playerSpeedMax);
        playerScript.ApplyPlayerSpeed(clampedSpeed);
        UpdateSettingDisplay(playerSpeedSetting, clampedSpeed, clampedSpeed.ToString("F0"));
        SavePreference(PrefPlayerSpeed, clampedSpeed, savePreference);
    }

    public void SetPlayerSense(float senseValue)
    {
        SetPlayerSense(senseValue, true);
    }

    public void SetPlayerVolume(float volumeValue)
    {
        SetPlayerVolume(volumeValue, true);
    }

    void SetPlayerVolume(float volumeValue, bool savePreference)
    {
        float clampedVolume = ClampToRange(volumeValue, playerVolumeMin, playerVolumeMax);
        ApplyVolumeToAudioSource(clampedVolume);
        UpdateSettingDisplay(playerVolumeSetting, clampedVolume, clampedVolume.ToString("F0"));
        SavePreference(PrefPlayerVolume, clampedVolume, savePreference);
    }

    void SetPlayerSense(float senseValue, bool savePreference)
    {
        if (playerScript == null)
        {
            return;
        }

        float clampedSense = ClampToRange(senseValue, cameraSensitivityMin, cameraSensitivityMax);
        playerScript.ApplyCameraSensitivity(clampedSense);
        UpdateSettingDisplay(cameraSensitivitySetting, clampedSense, clampedSense.ToString("F1"));
        SavePreference(PrefCameraSensitivity, clampedSense, savePreference);
    }

    public void SetFovValue(float fovValue)
    {
        SetFovValue(fovValue, true);
    }

    void SetFovValue(float fovValue, bool savePreference)
    {
        if (playerScript == null)
        {
            return;
        }

        float clampedFov = ClampToRange(fovValue, cameraFovMin, cameraFovMax);
        playerScript.ApplyCameraFov(clampedFov);
        UpdateSettingDisplay(cameraFovSetting, clampedFov, ((int)clampedFov).ToString());
        SavePreference(PrefCameraFov, clampedFov, savePreference);
    }

    public void SetRenderValue(float renderValue)
    {
        SetRenderValue(renderValue, true);
    }

    void SetRenderValue(float renderValue, bool savePreference)
    {
        if (playerScript == null)
        {
            return;
        }

        float clampedRenderDistance = ClampToRange(renderValue, renderDistanceMin, renderDistanceMax);
        playerScript.ApplyRenderDistance(clampedRenderDistance);
        UpdateSettingDisplay(renderDistanceSetting, clampedRenderDistance, clampedRenderDistance.ToString("F0"));
        SavePreference(PrefRenderDistance, clampedRenderDistance, savePreference);
    }

    public void SetGameQuality(int qualityIndex)
    {
        SetGameQuality(qualityIndex, true);
    }

    void SetGameQuality(int qualityIndex, bool savePreference)
    {
        int levelCount = QualitySettings.names.Length;
        if (levelCount == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(qualityIndex, 0, levelCount - 1);
        QualitySettings.SetQualityLevel(clampedIndex, true);

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(clampedIndex);
            qualityDropdown.RefreshShownValue();
        }

        if (savePreference)
        {
            PlayerPrefs.SetInt(PrefGameQuality, clampedIndex);
            PlayerPrefs.Save();
        }
    }

    void UpdateSettingDisplay(SettingsElement setting, float value, string displayValue)
    {
        if (setting == null)
        {
            return;
        }

        if (setting.slider != null)
        {
            setting.slider.SetValueWithoutNotify(value);
        }

        if (setting.valueText != null)
        {
            setting.valueText.text = displayValue;
        }
    }

    void SavePreference(string key, float value, bool shouldSave)
    {
        if (!shouldSave)
        {
            return;
        }

        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    void LoadSavedSettings()
    {
        if (playerScript == null)
        {
            return;
        }

        float savedSpeed = PlayerPrefs.GetFloat(PrefPlayerSpeed, playerScript.PlayerSpeed);
        float savedVolume = PlayerPrefs.GetFloat(PrefPlayerVolume, GetDefaultVolumeSettingValue());
        float savedSensitivity = PlayerPrefs.GetFloat(PrefCameraSensitivity, playerScript.CameraSense);
        float savedFov = PlayerPrefs.GetFloat(PrefCameraFov, playerScript.GetCameraFov());
        float savedRenderDistance = PlayerPrefs.GetFloat(PrefRenderDistance, playerScript.GetRenderDistance());

        SetPlayerSpeed(savedSpeed, false);
        SetPlayerVolume(savedVolume, false);
        SetPlayerSense(savedSensitivity, false);
        SetFovValue(savedFov, false);
        SetRenderValue(savedRenderDistance, false);

        int savedQuality = PlayerPrefs.GetInt(PrefGameQuality, QualitySettings.GetQualityLevel());
        SetGameQuality(savedQuality, false);
    }

    float ClampToRange(float value, float minValue, float maxValue)
    {
        float clampedMin = Mathf.Min(minValue, maxValue);
        float clampedMax = Mathf.Max(minValue, maxValue);
        return Mathf.Clamp(value, clampedMin, clampedMax);
    }

    float GetDefaultVolumeSettingValue()
    {
        if (playerVolumeAudioSource == null)
        {
            return playerVolumeMin;
        }

        float clampedMin = Mathf.Min(playerVolumeMin, playerVolumeMax);
        float clampedMax = Mathf.Max(playerVolumeMin, playerVolumeMax);
        return Mathf.Lerp(clampedMin, clampedMax, Mathf.Clamp01(playerVolumeAudioSource.volume));
    }

    void ApplyVolumeToAudioSource(float settingValue)
    {
        if (playerVolumeAudioSource == null)
        {
            return;
        }

        float clampedMin = Mathf.Min(playerVolumeMin, playerVolumeMax);
        float clampedMax = Mathf.Max(playerVolumeMin, playerVolumeMax);
        float normalizedVolume = Mathf.InverseLerp(clampedMin, clampedMax, settingValue);
        playerVolumeAudioSource.volume = Mathf.Clamp01(normalizedVolume);
    }

    public void OpenDonationPage()
    {
        Application.OpenURL("");
    }
}
