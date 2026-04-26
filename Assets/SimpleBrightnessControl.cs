using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SimpleBrightnessControl : MonoBehaviour
{
    [Header("Adjust these in Inspector")]
    public float minBrightness = 0f;
    public float maxBrightness = 2f;
    public float defaultValue = 1f;
    
    private Slider slider;
    private Light2D globalLight;

    void Start()
    {
        // Auto-find light
        globalLight = FindFirstObjectByType<Light2D>();
        
        // Auto-find slider on this GameObject
        slider = GetComponent<Slider>();
        
        if (slider != null)
        {
            slider.minValue = minBrightness;
            slider.maxValue = maxBrightness;
            slider.value = defaultValue;
            slider.onValueChanged.AddListener(ApplyBrightness);
        }
        
        ApplyBrightness(defaultValue);
    }

    void ApplyBrightness(float value)
    {
        if (globalLight != null)
            globalLight.intensity = value;
    }
}