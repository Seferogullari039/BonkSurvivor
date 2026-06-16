using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private float attractionRange = 4f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private int coinAmount = 1;
    [SerializeField] private float lifeTime = 30f;

    private Transform player;

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

        if (distance <= attractionRange)
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
        if (!other.CompareTag("Player")) return;

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            AudioManager.Instance?.PlayCoinPickup();
            playerStats.AddCoins(coinAmount);

            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.PlayCoinPickup(transform.position);
            }

            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
