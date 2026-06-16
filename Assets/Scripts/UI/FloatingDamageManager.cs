using UnityEngine;

public class FloatingDamageManager : MonoBehaviour
{
    public static FloatingDamageManager Instance { get; private set; }

    [SerializeField] private GameObject floatingDamagePrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnDamage(Vector3 position, int damage, bool isCrit = false)
    {
        if (floatingDamagePrefab == null) return;

        Vector3 spawnPosition = position + new Vector3(
            Random.Range(-0.28f, 0.28f),
            Random.Range(0.05f, 0.25f),
            Random.Range(-0.28f, 0.28f)
        );

        GameObject damageObject = Instantiate(
            floatingDamagePrefab,
            spawnPosition,
            Quaternion.identity
        );

        FloatingDamage floatingDamage =
            damageObject.GetComponent<FloatingDamage>();

        if (floatingDamage != null)
        {
            floatingDamage.Setup(damage, isCrit);
        }
    }
}
