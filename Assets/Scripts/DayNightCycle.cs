using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using UnityEngine.UI;

[System.Serializable]
public class DayNightOffsetLight
{
    public Light light;

    // Per-light weight lets you bias how strongly this light follows the global offset curve.
    [Range(0f, 3f)]
    public float offsetWeight = 1f;
}

[ExecuteAlways]
public class DayNightCycle : MonoBehaviour
{
    static readonly int SkyboxExposure = Shader.PropertyToID("_Exposure");
    static readonly int SkyboxAtmosphereThickness = Shader.PropertyToID("_AtmosphereThickness");

    [Title("Scene References")]
    public Light directionalLight;
    public Material proceduralSkybox;

    [Title("Cycle Controls")]
    [Range(0f, 24f), OnValueChanged(nameof(ApplyTimeOfDay))]
    public float timeOfDay = 12f;

    [Clamp(0.01f, 240f)]
    public float dayLengthInMinutes = 4f;

    public bool autoCycle = true;
    public bool updateEnvironmentLighting = true;

    [Title("UI")]
    public Toggle dayNightToggle;

    [Title("Sun Motion")]
    [Range(0f, 360f)]
    public float sunYaw = 170f;

    [Title("Visual Curves")]
    public Gradient sunColorOverDay;
    public AnimationCurve sunIntensityOverDay;
    public AnimationCurve ambientIntensityOverDay;
    public AnimationCurve skyExposureOverDay;
    public AnimationCurve skyAtmosphereThicknessOverDay;

    [Title("Additional Light Offsets")]
    public List<DayNightOffsetLight> offsetLights = new();

    public float offsetIntensityScale = 1f;

    public AnimationCurve offsetIntensityOverDay;

    readonly Dictionary<Light, float> offsetLightBaseIntensity = new();

    void Reset()
    {
        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
        }

        if (proceduralSkybox == null)
        {
            proceduralSkybox = RenderSettings.skybox;
        }

        ApplyDefaultCurves();
        ApplyTimeOfDay();
    }

    void OnEnable()
    {
        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
        }

        if (proceduralSkybox == null)
        {
            proceduralSkybox = RenderSettings.skybox;
        }

        EnsureCurves();
        BindDayNightToggle();
        CacheOffsetLightBaseIntensities();
        ApplyTimeOfDay();
    }

    void OnDisable()
    {
        UnbindDayNightToggle();
    }

    void OnValidate()
    {
        timeOfDay = Mathf.Repeat(timeOfDay, 24f);
        dayLengthInMinutes = Mathf.Max(0.01f, dayLengthInMinutes);

        EnsureCurves();
        CacheOffsetLightBaseIntensities();
        ApplyTimeOfDay();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            ApplyTimeOfDay();
            return;
        }

        if (!autoCycle)
        {
            ApplyTimeOfDay();
            return;
        }

        // Convert one full in-game day (24 hours) into the configured real-time duration.
        float hoursPerSecond = 24f / (dayLengthInMinutes * 60f);
        timeOfDay = Mathf.Repeat(timeOfDay + (hoursPerSecond * Time.deltaTime), 24f);

        ApplyTimeOfDay();
    }

    void BindDayNightToggle()
    {
        if (dayNightToggle == null)
        {
            return;
        }

        dayNightToggle.onValueChanged.RemoveListener(OnDayNightToggleChanged);
        dayNightToggle.onValueChanged.AddListener(OnDayNightToggleChanged);

        // Let the UI state drive the runtime state immediately after binding.
        OnDayNightToggleChanged(dayNightToggle.isOn);
    }

    void UnbindDayNightToggle()
    {
        if (dayNightToggle == null)
        {
            return;
        }

        dayNightToggle.onValueChanged.RemoveListener(OnDayNightToggleChanged);
    }

    public void OnDayNightToggleChanged(bool isEnabled)
    {
        autoCycle = isEnabled;

        if (!autoCycle)
        {
            // Requirement: when cycle is toggled off, snap to 12:00 (midday).
            timeOfDay = 12f;
        }

        ApplyTimeOfDay();
    }

    void ApplyTimeOfDay()
    {
        float normalizedTime = Mathf.InverseLerp(0f, 24f, timeOfDay);

        if (directionalLight != null)
        {
            // 6:00 and 18:00 sit around the horizon, 12:00 is highest point overhead.
            float sunPitch = (normalizedTime * 360f) - 90f;
            directionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);

            directionalLight.color = sunColorOverDay.Evaluate(normalizedTime);
            directionalLight.intensity = Mathf.Max(0f, sunIntensityOverDay.Evaluate(normalizedTime));
        }

        if (proceduralSkybox != null)
        {
            if (proceduralSkybox.HasProperty(SkyboxExposure))
            {
                proceduralSkybox.SetFloat(SkyboxExposure, Mathf.Max(0f, skyExposureOverDay.Evaluate(normalizedTime)));
            }

            if (proceduralSkybox.HasProperty(SkyboxAtmosphereThickness))
            {
                proceduralSkybox.SetFloat(SkyboxAtmosphereThickness, Mathf.Max(0f, skyAtmosphereThicknessOverDay.Evaluate(normalizedTime)));
            }
        }

        if (updateEnvironmentLighting)
        {
            // This is Lighting > Environment > Environment Lighting > Intensity Multiplier.
            // Reflection intensity is intentionally not modified.
            RenderSettings.ambientIntensity = Mathf.Max(0f, ambientIntensityOverDay.Evaluate(normalizedTime));
            DynamicGI.UpdateEnvironment();
        }

        ApplyOffsetLights(normalizedTime);
    }

    void EnsureCurves()
    {
        // If the component was added from code or lost serialized data, regenerate practical defaults.
        if (sunColorOverDay == null || sunColorOverDay.colorKeys == null || sunColorOverDay.colorKeys.Length == 0)
        {
            ApplyDefaultCurves();
            return;
        }

        if (sunIntensityOverDay == null || sunIntensityOverDay.length == 0 ||
            ambientIntensityOverDay == null || ambientIntensityOverDay.length == 0 ||
            offsetIntensityOverDay == null || offsetIntensityOverDay.length == 0 ||
            skyExposureOverDay == null || skyExposureOverDay.length == 0 ||
            skyAtmosphereThicknessOverDay == null || skyAtmosphereThicknessOverDay.length == 0)
        {
            ApplyDefaultCurves();
        }
    }

    void ApplyDefaultCurves()
    {
        // Color keys target cooler moonlight at night and warm tones around sunrise/sunset.
        sunColorOverDay = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(0.18f, 0.24f, 0.40f), 0.00f),
                new GradientColorKey(new Color(1.00f, 0.56f, 0.34f), 0.23f),
                new GradientColorKey(new Color(1.00f, 0.97f, 0.88f), 0.50f),
                new GradientColorKey(new Color(1.00f, 0.56f, 0.34f), 0.77f),
                new GradientColorKey(new Color(0.18f, 0.24f, 0.40f), 1.00f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        // Night is dim, transitions are smooth, midday reaches full power.
        sunIntensityOverDay = new AnimationCurve(
            new Keyframe(0.00f, 0.00f),
            new Keyframe(0.20f, 0.00f),
            new Keyframe(0.27f, 0.45f),
            new Keyframe(0.50f, 1.20f),
            new Keyframe(0.73f, 0.45f),
            new Keyframe(0.80f, 0.00f),
            new Keyframe(1.00f, 0.00f)
        );

        // Drives Lighting tab Environment Lighting intensity multiplier only.
        ambientIntensityOverDay = new AnimationCurve(
            new Keyframe(0.00f, 0.20f),
            new Keyframe(0.23f, 0.35f),
            new Keyframe(0.50f, 1.00f),
            new Keyframe(0.77f, 0.35f),
            new Keyframe(1.00f, 0.20f)
        );

        // Additive offset around each light's own baseline intensity.
        // This avoids forcing all extra lights to a single absolute intensity value.
        offsetIntensityOverDay = new AnimationCurve(
            new Keyframe(0.00f, -0.35f),
            new Keyframe(0.23f, -0.10f),
            new Keyframe(0.50f, 0.40f),
            new Keyframe(0.77f, -0.10f),
            new Keyframe(1.00f, -0.35f)
        );

        // Higher exposure by day, lower at night for better contrast and realism.
        skyExposureOverDay = new AnimationCurve(
            new Keyframe(0.00f, 0.35f),
            new Keyframe(0.23f, 0.55f),
            new Keyframe(0.50f, 1.30f),
            new Keyframe(0.77f, 0.55f),
            new Keyframe(1.00f, 0.35f)
        );

        // Slightly thicker atmosphere around horizon transitions to mimic warmer haze.
        skyAtmosphereThicknessOverDay = new AnimationCurve(
            new Keyframe(0.00f, 0.90f),
            new Keyframe(0.23f, 1.20f),
            new Keyframe(0.50f, 0.85f),
            new Keyframe(0.77f, 1.20f),
            new Keyframe(1.00f, 0.90f)
        );
    }

    [Button("Recapture Light Baselines")]
    void RecaptureOffsetLightBaselines()
    {
        offsetLightBaseIntensity.Clear();
        CacheOffsetLightBaseIntensities(forceRefresh: true);
        ApplyTimeOfDay();
    }

    void CacheOffsetLightBaseIntensities(bool forceRefresh = false)
    {
        if (offsetLights == null)
        {
            offsetLightBaseIntensity.Clear();
            return;
        }

        HashSet<Light> activeLights = new();

        for (int i = 0; i < offsetLights.Count; i++)
        {
            DayNightOffsetLight configuredLight = offsetLights[i];
            if (configuredLight == null || configuredLight.light == null)
            {
                continue;
            }

            Light light = configuredLight.light;
            activeLights.Add(light);

            if (forceRefresh || !offsetLightBaseIntensity.ContainsKey(light))
            {
                offsetLightBaseIntensity[light] = light.intensity;
            }
        }

        if (offsetLightBaseIntensity.Count == 0)
        {
            return;
        }

        List<Light> staleLights = new();
        foreach (Light cachedLight in offsetLightBaseIntensity.Keys)
        {
            if (!activeLights.Contains(cachedLight))
            {
                staleLights.Add(cachedLight);
            }
        }

        for (int i = 0; i < staleLights.Count; i++)
        {
            offsetLightBaseIntensity.Remove(staleLights[i]);
        }
    }

    void ApplyOffsetLights(float normalizedTime)
    {
        if (offsetLights == null || offsetLights.Count == 0)
        {
            return;
        }

        CacheOffsetLightBaseIntensities();

        float globalOffset = offsetIntensityOverDay.Evaluate(normalizedTime) * offsetIntensityScale;

        for (int i = 0; i < offsetLights.Count; i++)
        {
            DayNightOffsetLight configuredLight = offsetLights[i];
            if (configuredLight == null || configuredLight.light == null)
            {
                continue;
            }

            Light light = configuredLight.light;

            if (!offsetLightBaseIntensity.TryGetValue(light, out float baseIntensity))
            {
                baseIntensity = light.intensity;
                offsetLightBaseIntensity[light] = baseIntensity;
            }

            float offset = globalOffset * configuredLight.offsetWeight;
            light.intensity = Mathf.Max(0f, baseIntensity + offset);
        }
    }
}
