using UnityEngine;

public class FireLightFlicker : MonoBehaviour
{
    public Light fireLight;

    public float minIntensity = 2.2f;
    public float maxIntensity = 3.8f;
    public float flickerSpeed = 6f;

    private void Awake()
    {
        if (fireLight == null)
            fireLight = GetComponent<Light>();
    }

    private void Update()
    {
        if (fireLight == null)
            return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}