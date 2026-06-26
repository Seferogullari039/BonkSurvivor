using UnityEngine;

[DefaultExecutionOrder(100)]
public class EnemyHitFeedback : MonoBehaviour
{
    public void PlayHit(Vector3 hitSource)
    {
        HitFeedbackUtility.TryPlayHitSound();
    }

    public void PlayDeath(Vector3 position)
    {
        if (HitFeedbackUtility.TrySpawnDeathVfx(position))
        {
            return;
        }

        CombatDeathFeedback.PlayBurst(position);
    }
}
