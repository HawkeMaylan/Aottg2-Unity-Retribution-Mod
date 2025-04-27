using UnityEngine;

public class SimpleDayNightCycle : MonoBehaviour
{
    [Header("References")]
    public Light sun;
    public Light moon;

    [Header("Settings")]
    public float dayDuration = 120f; // Seconds for a full day-night cycle
    public float sunInitialAngle = 0f; // Start angle (0 = sunrise)
    public float rotationDirection = 1f; // 1 = normal, -1 = reverse

    [Header("Light Intensity Settings")]
    public float maxSunIntensity = 1.0f;
    public float maxMoonIntensity = 0.2f;

    [Header("Sky Color Settings")]
    public Gradient daySkyColorGradient;
    public Gradient dayGroundColorGradient;

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
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        float moonAngle = sunAngle + 180f;
        moon.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);
    }

    private void UpdateLighting()
    {
        if (skyboxMaterial != null)
        {
            //  Sky and Ground color purely based on timeOfDay
            Color currentSkyColor = daySkyColorGradient.Evaluate(timeOfDay);
            Color currentGroundColor = dayGroundColorGradient.Evaluate(timeOfDay);

            skyboxMaterial.SetColor("_SkyTint", currentSkyColor);
            skyboxMaterial.SetColor("_GroundColor", currentGroundColor);

            // Optional: if you still want exposure based on lighting, you can remove this if you want pure time
            float sunHeight = Vector3.Dot(sun.transform.forward, Vector3.down);
            float clampedSunHeight = Mathf.Clamp01(sunHeight);

            // Still adjusting lighting intensity
            sun.intensity = clampedSunHeight * maxSunIntensity;
            moon.intensity = Mathf.Clamp01(-sunHeight) * maxMoonIntensity;

            float exposure = clampedSunHeight > 0.01f ? Mathf.Lerp(0.0f, 1.3f, clampedSunHeight) : 0f;
            skyboxMaterial.SetFloat("_Exposure", exposure);
        }
    }
}
