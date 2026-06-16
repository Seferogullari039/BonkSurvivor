using TMPro;
using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float moveSpeed = 1.4f;

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
        startFontSize = textMesh.fontSize;

        if (isCrit)
        {
            textMesh.fontSize = startFontSize * 1.35f;
            startColor = new Color(1f, 0.86f, 0.28f, 1f);
            textMesh.color = startColor;
        }
        else
        {
            startColor = textMesh.color;
        }
    }
}
