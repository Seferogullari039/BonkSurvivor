using UnityEngine;

public class BossDashAbility : MonoBehaviour
{
    private const float DashInterval = 4f;
    private const float TelegraphDuration = 0.6f;
    private const float DashDuration = 0.45f;
    private const float DashSpeed = 14f;

    private static readonly Color TelegraphColorA = new Color(0.75f, 0.08f, 0.28f);
    private static readonly Color TelegraphColorB = new Color(1f, 0.22f, 0.22f);

    private enum DashPhase
    {
        Idle,
        WindUp,
        Dashing
    }

    private Enemy enemy;
    private Renderer bossRenderer;
    private Transform playerTarget;
    private Color normalColor;
    private DashPhase phase = DashPhase.Idle;
    private float phaseTimer;
    private float idleTimer;
    private Vector3 dashDirection;
    private bool telegraphAudioPlayed;

    public void Initialize(Color bossColor)
    {
        normalColor = bossColor;
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        bossRenderer = GetComponent<Renderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void Update()
    {
        if (playerTarget == null) return;

        switch (phase)
        {
            case DashPhase.Idle:
                UpdateIdle();
                break;
            case DashPhase.WindUp:
                UpdateWindUp();
                break;
            case DashPhase.Dashing:
                UpdateDash();
                break;
        }
    }

    private void UpdateIdle()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer < DashInterval) return;

        idleTimer = 0f;
        phase = DashPhase.WindUp;
        phaseTimer = 0f;
        telegraphAudioPlayed = false;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;
        dashDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
    }

    private void UpdateWindUp()
    {
        phaseTimer += Time.deltaTime;

        if (!telegraphAudioPlayed)
        {
            AudioManager.Instance?.PlayTelegraphWarning();
            telegraphAudioPlayed = true;
        }

        if (bossRenderer != null)
        {
            float pulse = Mathf.PingPong(phaseTimer * 8f, 1f);
            Color telegraphColor = Color.Lerp(TelegraphColorA, TelegraphColorB, pulse);
            EnemyTelegraphUtility.ApplyFlashColor(bossRenderer, telegraphColor, 0.75f);
        }

        if (phaseTimer < TelegraphDuration) return;

        phase = DashPhase.Dashing;
        phaseTimer = 0f;

        if (enemy != null)
        {
            enemy.SetMovementLocked(true);
        }

        EnemyTelegraphUtility.RestoreColor(bossRenderer, normalColor, 0.88f, true, 0.45f);
    }

    private void UpdateDash()
    {
        phaseTimer += Time.deltaTime;
        transform.position += dashDirection * DashSpeed * Time.deltaTime;

        if (phaseTimer < DashDuration) return;

        if (enemy != null)
        {
            enemy.SetMovementLocked(false);
        }

        EnemyTelegraphUtility.RestoreColor(bossRenderer, normalColor, 0.88f, true, 0.45f);

        phase = DashPhase.Idle;
        phaseTimer = 0f;
    }
}
