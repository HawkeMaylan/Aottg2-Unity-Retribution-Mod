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

    [Header("Light Intensity Settings")]
    public float maxSunIntensity = 1.0f;
    public float maxMoonIntensity = 0.2f;

    [Header("Sky Color Settings")]
    public Gradient daySkyColorGradient;
    public Gradient dayGroundColorGradient;

    [Header("Sun Light Color Settings")]
    public Color sunriseSunColor = new Color(1.0f, 0.5f, 0.2f); // Orange/red
    public Color middaySunColor = Color.white; // White
    public Color sunsetSunColor = new Color(1.0f, 0.5f, 0.2f);  // Orange/red again

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
            Color currentSkyColor = daySkyColorGradient.Evaluate(timeOfDay);
            Color currentGroundColor = dayGroundColorGradient.Evaluate(timeOfDay);

            skyboxMaterial.SetColor("_SkyTint", currentSkyColor);
            skyboxMaterial.SetColor("_GroundColor", currentGroundColor);

            float sunHeight = Vector3.Dot(sun.transform.forward, Vector3.down);
            float clampedSunHeight = Mathf.Clamp01(sunHeight);

            // Adjust Sun and Moon light intensity
            sun.intensity = clampedSunHeight * maxSunIntensity;
            moon.intensity = Mathf.Clamp01(-sunHeight) * maxMoonIntensity;

            // Exposure fix
            float exposure = Mathf.Lerp(0.2f, 1.3f, clampedSunHeight);
            skyboxMaterial.SetFloat("_Exposure", exposure);

            //  Adjust Sun color based on new timing
            sun.color = CalculateSunColor(timeOfDay);
        }
    }

    private Color CalculateSunColor(float time)
    {
        if (time < 0.1f)
        {
            // Sunrise to Midday (orange to white)
            float t = time / 0.1f;
            return Color.Lerp(sunriseSunColor, middaySunColor, t);
        }
        else if (time < 0.4f)
        {
            // Hold pure midday color
            return middaySunColor;
        }
        else if (time < 0.5f)
        {
            // Midday to Sunset (white to orange)
            float t = (time - 0.4f) / 0.1f;
            return Color.Lerp(middaySunColor, sunsetSunColor, t);
        }
        else
        {
            // Hold sunset color at night
            return sunsetSunColor;
        }
    }
}
