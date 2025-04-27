using UnityEngine;

public class SimpleDayNightCycle : MonoBehaviour
{
    [Header("References")]
    public Light sun;
    public Light moon;

    [Header("Settings")]
    public float dayDuration = 120f; // Seconds for full day-night cycle
    public float sunInitialAngle = 0f; // Start angle (0 = sunrise)

    [Header("Light Intensity Settings")]
    public float maxSunIntensity = 1.0f; // Maximum intensity for the sun
    public float maxMoonIntensity = 0.2f; // Maximum intensity for the moon

    [Header("Sky Color Settings")]
    public Color daySkyColor = new Color(0.5f, 0.7f, 1f); // Light blue day sky
    public Color nightSkyColor = Color.black; // Pitch black night sky
    public Color dayGroundColor = new Color(0.369f, 0.349f, 0.341f); // Default ground
    public Color nightGroundColor = Color.black; // Pitch black ground at night

    private Material skyboxMaterial;
    private float timeOfDay; // 0 to 1 over a full day

    private void Start()
    {
        // If you manually assigned it, use that first
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }
        else
        {
            // Otherwise, auto-load it by name from Resources folder
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

        // Always update lighting environment
        DynamicGI.UpdateEnvironment();

        // Optional: Clear any camera-specific skybox overrides
        var camera = Camera.main;
        if (camera != null)
        {
            var skyboxComponent = camera.GetComponent<Skybox>();
            if (skyboxComponent != null)
            {
                skyboxComponent.material = null; // clear per-camera override if any
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
        float sunAngle = (timeOfDay * 360f) + sunInitialAngle;
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        moon.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
    }

    private void UpdateLighting()
    {
        float sunHeight = Vector3.Dot(sun.transform.forward, Vector3.down);
        float clampedSunHeight = Mathf.Clamp01(sunHeight);

        // Light intensities
        sun.intensity = clampedSunHeight * maxSunIntensity;
        moon.intensity = Mathf.Clamp01(-sunHeight) * maxMoonIntensity;

        // Skybox exposure: 0 at night, 1.3 at daytime
        float exposure = clampedSunHeight > 0.01f ? Mathf.Lerp(0.0f, 1.3f, clampedSunHeight) : 0f;

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Exposure", exposure);

            // Sky Tint color based on time
            Color currentSkyColor = Color.Lerp(nightSkyColor, daySkyColor, clampedSunHeight);
            skyboxMaterial.SetColor("_SkyTint", currentSkyColor);

            // Ground Color based on time
            Color currentGroundColor = Color.Lerp(nightGroundColor, dayGroundColor, clampedSunHeight);
            skyboxMaterial.SetColor("_GroundColor", currentGroundColor);
        }
    }
}
