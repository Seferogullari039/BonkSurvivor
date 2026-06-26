using UnityEngine;

public class XPOrb : MonoBehaviour
{
    [SerializeField] private float attractionRange = 4f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private int xpAmount = 1;
    [SerializeField] private float lifeTime = 30f;

    private Transform player;
    private bool collected;

    private void Awake()
    {
        PickupVisual.Apply(transform, PickupVisualType.XPOrb);
    }

    private void Start()
    {
        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        float finalAttractionRange = attractionRange;

        if (UpgradeManager.Instance != null)
        {
            finalAttractionRange *= UpgradeManager.Instance.XPAttractionMultiplier;
            finalAttractionRange *= UpgradeManager.Instance.PickupRangeMultiplier;
        }

        finalAttractionRange *= RelicManager.PickupRangeMultiplier;

        finalAttractionRange = LegendaryPassiveEffectManager.ResolvePickupRange(finalAttractionRange);

        if (distance <= finalAttractionRange)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            collected = true;
            AudioManager.Instance?.PlayXpPickup();

            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.PlayXPPickup(transform.position);
            }

            PickupCollectFeedback.Play(this, () =>
            {
                playerStats.AddXP(xpAmount);
                Destroy(gameObject);
            });
            return;
        }

        Destroy(gameObject);
    }
}