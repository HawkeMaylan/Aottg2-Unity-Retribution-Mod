using UnityEngine;

public class SimpleDayNightCycle : MonoBehaviour
{
    [Header("References")]
    public Light sun;
    public Light moon;

    [Header("Settings")]
    public float dayDuration = 120f;
    public float sunInitialAngle = 0f;
    public float rotationDirection = 1f;

    [Header("Sunrise Direction")]
    [Tooltip("Horizontal direction (in degrees) along the horizon where the sun rises. 0 = North, 90 = East, 180 = South, 270 = West.")]
    public float sunAzimuth = 170f;

    [Header("Light Intensity Settings")]
    public float maxSunIntensity = 1.0f;
    public float maxMoonIntensity = 0.2f;

    [Header("Sky Color Settings")]
    public Gradient daySkyColorGradient;
    public Gradient dayGroundColorGradient;

    [Header("Sun Light Color Settings")]
    public Color sunriseSunColor = new Color(1.0f, 0.5f, 0.2f);
    public Color middaySunColor = Color.white;
    public Color sunsetSunColor = new Color(1.0f, 0.5f, 0.2f);

    [Header("Exposure Settings")]
    public float minimumNightExposure = 0.2f;

    [Header("Time Settings")]
    public float sunriseDuration = 0.1f;
    public float middayDuration = 0.3f;
    public float sunsetDuration = 0.1f;
    public float nightFadeInDuration = 0.25f;
    public float nightFadeOutDuration = 0.15f;

    [Header("Night Blackout Settings")]
    public float nightBlackoutDuration = 0.1f; // Hold 0 exposure after sunset

    private Material skyboxMaterial;
    private float timeOfDay;

    private void Start()
    {
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }
        else
        {
            skyboxMaterial = Resources.Load<Material>("HawkProcedural");
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                Debug.LogWarning("HawkProcedural material not found in Resources!");
            }
        }

        DynamicGI.UpdateEnvironment();

        var camera = Camera.main;
        if (camera != null)
        {
            var skyboxComponent = camera.GetComponent<Skybox>();
            if (skyboxComponent != null)
            {
                skyboxComponent.material = null;
            }
        }
    }

    private void Update()
    {
        UpdateTime();
        UpdateSunAndMoon();
        UpdateLighting();
    }

    private void UpdateTime()
    {
        timeOfDay += Time.deltaTime / dayDuration;
        if (timeOfDay > 1f)
            timeOfDay -= 1f;
    }

    private void UpdateSunAndMoon()
    {
        float sunAngle = (timeOfDay * 360f * rotationDirection) + sunInitialAngle;
        sun.transform.rotation = Quaternion.Euler(sunAngle, sunAzimuth, 0f);

        float moonAngle = sunAngle + 180f;
        moon.transform.rotation = Quaternion.Euler(moonAngle, sunAzimuth, 0f);
    }

    private void UpdateLighting()
    {
        if (skyboxMaterial != null)
        {
            Color currentSkyColor = daySkyColorGradient.Evaluate(timeOfDay);
            Color currentGroundColor = dayGroundColorGradient.Evaluate(timeOfDay);

            skyboxMaterial.SetColor("_SkyTint", currentSkyColor);
            skyboxMaterial.SetColor("_GroundColor", currentGroundColor);

            float sunHeight = Vector3.Dot(sun.transform.forward, Vector3.down);
            float clampedSunHeight = Mathf.Clamp01(sunHeight);

            sun.intensity = clampedSunHeight * maxSunIntensity;
            moon.intensity = Mathf.Clamp01(-sunHeight) * maxMoonIntensity;

            float exposure = CalculateExposure(timeOfDay, clampedSunHeight);
            skyboxMaterial.SetFloat("_Exposure", exposure);

            sun.color = CalculateSunColor(timeOfDay);
        }
    }

    private Color CalculateSunColor(float time)
    {
        if (time < sunriseDuration)
        {
            float t = time / sunriseDuration;
            return Color.Lerp(sunriseSunColor, middaySunColor, t);
        }
        else if (time < sunriseDuration + middayDuration)
        {
            return middaySunColor;
        }
        else if (time < sunriseDuration + middayDuration + sunsetDuration)
        {
            float t = (time - (sunriseDuration + middayDuration)) / sunsetDuration;
            return Color.Lerp(middaySunColor, sunsetSunColor, t);
        }
        else
        {
            return sunsetSunColor;
        }
    }

    private float CalculateExposure(float time, float clampedSunHeight)
    {
        if (time < 0.5f)
        {
            return Mathf.Lerp(0.2f, 1.3f, clampedSunHeight);
        }
        else
        {
            if (time >= 0.5f && time < (0.5f + nightBlackoutDuration))
            {
                return 0.0f;
            }

            float nightStart = 0.5f + nightBlackoutDuration;
            float nightMid = nightStart + nightFadeInDuration;
            float nightEnd = nightMid + nightFadeOutDuration;

            if (time >= nightStart && time <= nightMid)
            {
                float t = (time - nightStart) / nightFadeInDuration;
                return Mathf.Lerp(0.0f, minimumNightExposure, t);
            }
            else if (time > nightMid && time <= nightEnd)
            {
                float t = (time - nightMid) / nightFadeOutDuration;
                return Mathf.Lerp(minimumNightExposure, 0.0f, t);
            }
            else
            {
                return 0.0f;
            }
        }
    }
}
