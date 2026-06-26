using System;
using System.Collections;
using UnityEngine;

public static class PickupCollectFeedback
{
    private const float CollectDuration = 0.1f;

    public static void Play(MonoBehaviour pickup, Action onComplete)
    {
        if (pickup == null)
        {
            onComplete?.Invoke();
            return;
        }

        Collider collider = pickup.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }

        PickupCollectAnimator animator = pickup.gameObject.GetComponent<PickupCollectAnimator>();

        if (animator == null)
        {
            animator = pickup.gameObject.AddComponent<PickupCollectAnimator>();
        }

        animator.Play(onComplete);
    }

    private sealed class PickupCollectAnimator : MonoBehaviour
    {
        private Action onComplete;
        private Vector3 startScale;
        private Coroutine routine;

        public void Play(Action complete)
        {
            onComplete = complete;
            startScale = transform.localScale;

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            float elapsed = 0f;

            while (elapsed < CollectDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / CollectDuration);
                float scaleMultiplier = 1f + Mathf.Sin(progress * Mathf.PI) * 0.28f;
                transform.localScale = startScale * scaleMultiplier;
                yield return null;
            }

            onComplete?.Invoke();
            Destroy(this);
        }
    }
}
