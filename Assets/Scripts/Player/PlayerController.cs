using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody rb;
    private float baseMoveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        baseMoveSpeed = moveSpeed;
    }

    public void ResetMoveSpeed()
    {
        moveSpeed = baseMoveSpeed;
    }

    public void ApplyMetaMoveSpeedBonus(float bonusPercent)
    {
        moveSpeed = baseMoveSpeed * (1f + Mathf.Max(0f, bonusPercent));
    }

    private void FixedUpdate()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.W)) vertical = 1f;

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}