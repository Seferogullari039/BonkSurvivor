using UnityEngine;

[DisallowMultipleComponent]
public class SlimeVisualAnimator : MonoBehaviour
{
    [SerializeField] private float idleAmount = 0.04f;
    [SerializeField] private float jumpHeight = 0.08f;
    [SerializeField] private float jumpInterval = 2.2f;
    [SerializeField] private float dashDistance = 0.06f;
    [SerializeField] private float dashInterval = 3.5f;
    [SerializeField] private float animationSpeed = 1f;

    private Transform animatedModel;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private float phaseOffset;

    private void Awake()
    {
        Transform visualRoot = ResolveVisualRoot();

        if (visualRoot == null)
        {
            enabled = false;
            return;
        }

        animatedModel = visualRoot.Find("Model");

        if (animatedModel == null || !animatedModel.gameObject.activeInHierarchy)
        {
            enabled = false;
            return;
        }

        baseLocalPosition = animatedModel.localPosition;
        baseLocalScale = animatedModel.localScale;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (animatedModel == null)
        {
            enabled = false;
            return;
        }

        float time = Time.time * animationSpeed + phaseOffset;

        float breath = Mathf.Sin(time * 2.5f) * idleAmount;
        Vector3 breathScale = new Vector3(
            baseLocalScale.x + breath,
            baseLocalScale.y - breath * 0.5f,
            baseLocalScale.z + breath);

        float jumpY = EvaluateHop(time, jumpInterval, jumpHeight);
        float dashZ = EvaluateHop(time + jumpInterval * 0.35f, dashInterval, dashDistance);

        animatedModel.localScale = breathScale;
        animatedModel.localPosition = baseLocalPosition + new Vector3(0f, jumpY, dashZ);
    }

    private Transform ResolveVisualRoot()
    {
        if (transform.name == "VisualRoot")
        {
            return transform;
        }

        Transform found = transform.Find("VisualRoot");

        if (found != null)
        {
            return found;
        }

        Transform parent = transform.parent;

        while (parent != null)
        {
            if (parent.name == "VisualRoot")
            {
                return parent;
            }

            parent = parent.parent;
        }

        return null;
    }

    private static float EvaluateHop(float time, float interval, float height)
    {
        if (interval <= 0.001f || height <= 0f)
        {
            return 0f;
        }

        float phase = (time % interval) / interval;

        if (phase >= 0.15f)
        {
            return 0f;
        }

        float normalized = phase / 0.15f;
        return Mathf.Sin(normalized * Mathf.PI) * height;
    }
}
