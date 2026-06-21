using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AnimatedSkeletonClipRuntimeTester : MonoBehaviour
{
    private const string IdleStateName = "Idle";
    private const string RunStateName = "Run";
    private const string AttackStateName = "Attack";

    [SerializeField] private Animator animator;
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool loopSequence = true;

    private readonly (string state, float duration)[] sequence =
    {
        (IdleStateName, 1f),
        (RunStateName, 2f),
        (AttackStateName, 1f),
        (RunStateName, 2f),
    };

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(PlaySequence());
        }
    }

    public void BeginTest()
    {
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        do
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                (string state, float duration) step = sequence[i];
                PlayState(step.state);
                yield return new WaitForSeconds(step.duration);
                LogCurrentState(step.state);
            }
        }
        while (loopSequence);
    }

    private void PlayState(string stateName)
    {
        if (animator == null)
        {
            Debug.LogWarning("[AnimatedSkeletonClipRuntimeTester] Animator missing.", this);
            return;
        }

        animator.CrossFadeInFixedTime(stateName, 0.05f, 0, 0f);
        animator.Update(0f);
    }

    private void LogCurrentState(string requestedState)
    {
        if (animator == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        string clipInfo = clipInfos.Length > 0 && clipInfos[0].clip != null
            ? clipInfos[0].clip.name + " len=" + clipInfos[0].clip.length.ToString("F2")
            : "none";

        Debug.Log("[AnimatedSkeletonClipRuntimeTester] requested=" + requestedState
            + " currentState=" + ResolveStateLabel(stateInfo)
            + " clipInfo=" + clipInfo
            + " normalized=" + stateInfo.normalizedTime.ToString("F2")
            + " avatarValid=" + (animator.avatar != null && animator.avatar.isValid)
            + " controller=" + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null"));
    }

    private static string ResolveStateLabel(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName(AttackStateName))
        {
            return AttackStateName;
        }

        if (stateInfo.IsName(RunStateName))
        {
            return RunStateName;
        }

        if (stateInfo.IsName(IdleStateName))
        {
            return IdleStateName;
        }

        return stateInfo.shortNameHash.ToString();
    }
}
