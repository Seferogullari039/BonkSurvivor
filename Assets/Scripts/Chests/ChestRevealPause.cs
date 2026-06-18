using UnityEngine;

public static class ChestRevealPause
{
    private static int pauseDepth;
    private static float savedTimeScale = 1f;
    private static bool ownsPause;

    public static bool IsPaused => pauseDepth > 0;

    public static void Begin()
    {
        if (pauseDepth == 0)
        {
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            ownsPause = true;
        }

        pauseDepth++;
    }

    public static void End()
    {
        if (pauseDepth <= 0)
        {
            return;
        }

        pauseDepth--;

        if (pauseDepth == 0 && ownsPause)
        {
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            ownsPause = false;
        }
    }

    public static void ForceEnd()
    {
        pauseDepth = 0;

        if (ownsPause)
        {
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            ownsPause = false;
        }
    }
}
