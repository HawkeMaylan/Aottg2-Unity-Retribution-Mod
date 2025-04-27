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

        sun.intensity = Mathf.Clamp01(sunHeight) * maxSunIntensity;
        moon.intensity = Mathf.Clamp01(-sunHeight) * maxMoonIntensity;

        float exposure = Mathf.Lerp(0.3f, 1.3f, Mathf.Clamp01(sunHeight));
        if (skyboxMaterial != null)
            skyboxMaterial.SetFloat("_Exposure", exposure);
    }
}
