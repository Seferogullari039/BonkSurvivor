using UnityEngine;

public class WhirlwindRingSpin : MonoBehaviour
{
    private float spinSpeed = 240f;
    private float lifetime = 2f;
    private float elapsed;

    public void Initialize(float duration)
    {
        lifetime = Mathf.Max(0.5f, duration);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
