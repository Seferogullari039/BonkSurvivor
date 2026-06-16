using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private bool initialized;

    public void Initialize(Vector3 shootDirection, float moveSpeed, int projectileDamage)
    {
        direction = shootDirection.normalized;
        speed = moveSpeed;
        damage = projectileDamage;
        initialized = true;
    }

    private void Start()
    {
        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
