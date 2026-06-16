using UnityEngine;

public class FPSScreenShake : MonoBehaviour
{
    private static FPSScreenShake instance;

    private Vector3 baseLocalPosition;
    private float timer;
    private float duration;
    private float intensity;

    private void Awake()
    {
        instance = this;
        baseLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            float fade = duration > 0f ? Mathf.Clamp01(timer / duration) : 0f;

            Vector3 offset = Random.insideUnitSphere * intensity * fade;
            offset.z *= 0.2f;

            transform.localPosition = baseLocalPosition + offset;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, baseLocalPosition, Time.deltaTime * 20f);
        }
    }

    private void OnDisable()
    {
        transform.localPosition = baseLocalPosition;
    }

    private static FPSScreenShake GetOrCreate()
    {
        if (instance != null) return instance;

        Camera cam = Camera.main;
        if (cam == null) return null;

        instance = cam.GetComponent<FPSScreenShake>();
        if (instance == null)
        {
            instance = cam.gameObject.AddComponent<FPSScreenShake>();
        }

        return instance;
    }

    public static void ShakeSmall()
    {
        Shake(0.025f, 0.08f);
    }

    public static void ShakeMedium()
    {
        Shake(0.045f, 0.12f);
    }

    public static void ShakeBig()
    {
        Shake(0.08f, 0.18f);
    }

    public static void Shake(float shakeIntensity, float shakeDuration)
    {
        FPSScreenShake shaker = GetOrCreate();
        if (shaker == null) return;

        shaker.baseLocalPosition = shaker.transform.localPosition;
        shaker.intensity = Mathf.Max(shaker.intensity, shakeIntensity);
        shaker.duration = shakeDuration;
        shaker.timer = shakeDuration;
    }
}
