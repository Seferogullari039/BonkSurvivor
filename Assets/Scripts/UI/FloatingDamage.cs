using TMPro;
using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    // Default off: [DamageNumber] diagnostic is opt-in only. FaceCamera behavior is unaffected.
    public static bool LogDamageNumberDebug = false;

    private static bool faceCameraLogShown;

    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private float lifetime = 0.72f;
    [SerializeField] private float moveSpeed = 1.25f;

    private Color startColor = Color.white;
    private float age;
    private float startFontSize;

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

        transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (textMesh == null) return;

        float fade = 1f - Mathf.Clamp01(age / lifetime);
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, fade);

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

        if (textMesh == null) return;

        textMesh.text = "-" + damageAmount;
        startFontSize = textMesh.fontSize * 0.82f;
        textMesh.fontSize = startFontSize;

        if (isCrit)
        {
            textMesh.fontSize = startFontSize * 1.22f;
            startColor = new Color(1f, 0.86f, 0.28f, 1f);
            textMesh.color = startColor;
        }
        else
        {
            startColor = textMesh.color;
        }
    }
}
