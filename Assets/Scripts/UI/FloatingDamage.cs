using TMPro;
using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    // Default off: [DamageNumber] diagnostic is opt-in only. FaceCamera behavior is unaffected.
    public static bool LogDamageNumberDebug = false;

    private static bool faceCameraLogShown;

    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private float lifetime = 0.62f;
    [SerializeField] private float moveSpeed = 1.45f;

    private Color startColor = Color.white;
    private float age;
    private float startFontSize;
    private float horizontalDrift;
    private Vector3 startScale;

    private void Start()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TMP_Text>();
        }

        if (textMesh != null)
        {
            startColor = textMesh.color;
            startFontSize = textMesh.fontSize;
        }

        startScale = transform.localScale * 0.88f;
        transform.localScale = startScale;
        horizontalDrift = Random.Range(-0.28f, 0.28f);
        FaceCamera();

        if (LogDamageNumberDebug && !faceCameraLogShown)
        {
            faceCameraLogShown = true;
            Debug.Log("[DamageNumber] faceCamera ok");
        }
    }

    private void LateUpdate()
    {
        FaceCamera();
    }

    private void FaceCamera()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        transform.rotation = camera.transform.rotation;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01(age / lifetime);
        float riseEase = 1f - fade * fade;

        transform.position += new Vector3(
            horizontalDrift * Time.deltaTime,
            moveSpeed * riseEase * Time.deltaTime,
            0f);

        float scale = Mathf.Lerp(0.74f, 0.88f, fade);
        transform.localScale = startScale * scale;

        if (textMesh == null)
        {
            if (age >= lifetime)
            {
                Destroy(gameObject);
            }

            return;
        }

        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, fade * fade);
        textMesh.fontSize = startFontSize * Mathf.Lerp(0.92f, 1f, fade);

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void Setup(int damageAmount, bool isCrit = false)
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TMP_Text>();
        }

        if (textMesh == null)
        {
            return;
        }

        textMesh.text = "-" + damageAmount;
        startFontSize = textMesh.fontSize * 0.88f;
        textMesh.fontSize = startFontSize;

        if (isCrit)
        {
            textMesh.fontSize = startFontSize * 1.18f;
            startColor = new Color(1f, 0.86f, 0.28f, 1f);
            textMesh.color = startColor;
        }
        else
        {
            startColor = new Color(0.96f, 0.96f, 0.98f, 1f);
            textMesh.color = startColor;
        }
    }
}
