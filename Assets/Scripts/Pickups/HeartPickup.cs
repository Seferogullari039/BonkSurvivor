using UnityEngine;
using UnityEngine.Rendering;

public class HeartPickup : MonoBehaviour
{
    private const float TriggerRadius = 0.42f;
    private const float BobHeight = 0.1f;
    private const float BobSpeed = 3.2f;
    private const float RotateSpeed = 85f;
    private const float LifeTime = 25f;

    private static readonly Color HeartColor = new Color(0.92f, 0.22f, 0.38f);
    private static readonly Color HeartHighlight = new Color(1f, 0.45f, 0.58f);

    private Vector3 basePosition;
    private float bobPhase;

    public static void TrySpawnAt(Vector3 position, float dropChance)
    {
        if (dropChance <= 0f || Random.value >= dropChance)
        {
            return;
        }

        SpawnAt(position);
    }

    public static void SpawnAt(Vector3 position)
    {
        Vector3 offset = new Vector3(
            Random.Range(-0.35f, 0.35f),
            0.35f,
            Random.Range(-0.35f, 0.35f));

        GameObject pickupObject = new GameObject("HeartPickup");
        pickupObject.transform.position = position + offset;

        SphereCollider collider = pickupObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = TriggerRadius;

        pickupObject.AddComponent<HeartPickup>();
    }

    private void Awake()
    {
        basePosition = transform.position;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        BuildVisual();
        Destroy(gameObject, LifeTime);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * BobSpeed + bobPhase) * BobHeight;
        transform.position = basePosition + Vector3.up * bob;
        transform.Rotate(0f, RotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            return;
        }

        int healAmount = Mathf.Max(8, Mathf.RoundToInt(playerStats.EffectiveMaxHealth * 0.10f));
        playerStats.HealAmount(healAmount);
        Destroy(gameObject);
    }

    private void BuildVisual()
    {
        const float lobeSize = 0.14f;

        GameObject leftLobe = CreateVisualPrimitive(transform, PrimitiveType.Sphere, "HeartLeft");
        leftLobe.transform.localPosition = new Vector3(-lobeSize * 0.55f, lobeSize * 0.35f, 0f);
        leftLobe.transform.localScale = Vector3.one * lobeSize;
        ConfigureRenderer(leftLobe.GetComponent<Renderer>(), HeartColor, 0.68f);

        GameObject rightLobe = CreateVisualPrimitive(transform, PrimitiveType.Sphere, "HeartRight");
        rightLobe.transform.localPosition = new Vector3(lobeSize * 0.55f, lobeSize * 0.35f, 0f);
        rightLobe.transform.localScale = Vector3.one * lobeSize;
        ConfigureRenderer(rightLobe.GetComponent<Renderer>(), HeartColor, 0.68f);

        GameObject tip = CreateVisualPrimitive(transform, PrimitiveType.Sphere, "HeartTip");
        tip.transform.localPosition = new Vector3(0f, -lobeSize * 0.15f, 0f);
        tip.transform.localScale = new Vector3(lobeSize * 1.15f, lobeSize * 0.95f, lobeSize);
        ConfigureRenderer(tip.GetComponent<Renderer>(), HeartHighlight, 0.72f);
    }

    private static GameObject CreateVisualPrimitive(Transform parent, PrimitiveType primitive, string objectName)
    {
        GameObject visualObject = GameObject.CreatePrimitive(primitive);
        visualObject.name = objectName;
        visualObject.transform.SetParent(parent, false);

        Collider collider = visualObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        return visualObject;
    }

    private static void ConfigureRenderer(Renderer renderer, Color color, float smoothness)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        GameVisualStyle.ApplyColor(renderer, color, smoothness, false, 0f);
    }
}
