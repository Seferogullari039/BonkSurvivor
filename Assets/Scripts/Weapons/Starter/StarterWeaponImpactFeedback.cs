using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class StarterWeaponImpactFeedback
{
    public static void PlayBowFire(StarterWeaponViewModel viewModel, Vector3 muzzlePosition)
    {
        SpawnMuzzleSpark(muzzlePosition, new Color(1f, 0.95f, 0.72f), 0.07f, 0.11f);
    }

    public static void PlayFireStaffFire(StarterWeaponViewModel viewModel, Vector3 muzzlePosition)
    {
        SpawnMuzzleSpark(muzzlePosition, new Color(1f, 0.55f, 0.14f), 0.09f, 0.13f);
        SpawnMuzzleSpark(muzzlePosition, new Color(1f, 0.3f, 0.06f), 0.05f, 0.09f);
    }

    public static void PlaySwordSlash(StarterWeaponViewModel viewModel)
    {
        viewModel?.PlayImpactPunch(0.03f, 0.01f, 4f, 2.2f, 0.1f);
        SpawnSlashPulse(viewModel);
    }

    public static void PlayBlunderbussFire(StarterWeaponViewModel viewModel, Vector3 muzzlePosition, Vector3 forward)
    {
        viewModel?.PlayImpactPunch(0.034f, 0.012f, 3.2f, 1.4f, 0.1f);
        SpawnMuzzlePuff(muzzlePosition, forward, new Color(0.62f, 0.58f, 0.52f), new Color(1f, 0.58f, 0.2f));
    }

    public static void PlayThunderSpearFire(StarterWeaponViewModel viewModel, Vector3 tipPosition, Vector3 forward)
    {
        SpawnMuzzleSpark(tipPosition, new Color(0.35f, 0.92f, 1f), 0.08f, 0.12f);
        SpawnElectricArc(tipPosition, forward);
    }

    public static bool TryResolveMuzzlePoint(
        StarterWeaponViewModel viewModel,
        Vector3 fallbackPosition,
        Vector3 fallbackForward,
        out Vector3 worldPosition,
        out Vector3 forward)
    {
        if (viewModel != null && viewModel.TryGetWeaponMuzzlePoint(out worldPosition, out forward))
        {
            return true;
        }

        worldPosition = fallbackPosition;
        forward = fallbackForward.sqrMagnitude > 0.001f ? fallbackForward.normalized : Vector3.forward;
        return false;
    }

    private static void SpawnMuzzleSpark(Vector3 position, Color color, float size, float lifetime)
    {
        GameObject host = new GameObject("WeaponMuzzleSparkFx");
        WeaponImpactFxRunner runner = host.AddComponent<WeaponImpactFxRunner>();
        runner.PlaySpark(position, color, size, lifetime);
    }

    private static void SpawnMuzzlePuff(Vector3 position, Vector3 forward, Color smokeColor, Color flashColor)
    {
        GameObject host = new GameObject("WeaponMuzzlePuffFx");
        WeaponImpactFxRunner runner = host.AddComponent<WeaponImpactFxRunner>();
        runner.PlayPuff(position, forward, smokeColor, flashColor);
    }

    private static void SpawnElectricArc(Vector3 position, Vector3 forward)
    {
        GameObject host = new GameObject("WeaponElectricArcFx");
        WeaponImpactFxRunner runner = host.AddComponent<WeaponImpactFxRunner>();
        runner.PlayElectricArc(position, forward);
    }

    private static void SpawnSlashPulse(StarterWeaponViewModel viewModel)
    {
        if (viewModel == null || !viewModel.TryGetWeaponMuzzlePoint(out Vector3 position, out Vector3 forward))
        {
            return;
        }

        GameObject host = new GameObject("WeaponSlashPulseFx");
        WeaponImpactFxRunner runner = host.AddComponent<WeaponImpactFxRunner>();
        runner.PlaySlashPulse(position, forward);
    }

    private sealed class WeaponImpactFxRunner : MonoBehaviour
    {
        public void PlaySpark(Vector3 position, Color color, float size, float lifetime)
        {
            StartCoroutine(SparkRoutine(position, color, size, lifetime));
        }

        public void PlayPuff(Vector3 position, Vector3 forward, Color smokeColor, Color flashColor)
        {
            StartCoroutine(PuffRoutine(position, forward, smokeColor, flashColor));
        }

        public void PlayElectricArc(Vector3 position, Vector3 forward)
        {
            StartCoroutine(ElectricArcRoutine(position, forward));
        }

        public void PlaySlashPulse(Vector3 position, Vector3 forward)
        {
            StartCoroutine(SlashPulseRoutine(position, forward));
        }

        private IEnumerator SparkRoutine(Vector3 position, Color color, float size, float lifetime)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "MuzzleSpark";
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * size;
            RemoveCollider(spark);
            ConfigureRenderer(spark.GetComponent<Renderer>(), color, 0.68f, true, 0.55f);

            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / lifetime;

                if (spark != null)
                {
                    spark.transform.localScale = Vector3.one * size * (0.85f + fade * 0.45f);
                }

                yield return null;
            }

            if (spark != null)
            {
                Destroy(spark);
            }

            Destroy(gameObject);
        }

        private IEnumerator PuffRoutine(Vector3 position, Vector3 forward, Color smokeColor, Color flashColor)
        {
            Vector3 puffOrigin = position + forward.normalized * 0.04f;

            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "MuzzleFlash";
            flash.transform.position = puffOrigin;
            flash.transform.localScale = Vector3.one * 0.09f;
            RemoveCollider(flash);
            ConfigureRenderer(flash.GetComponent<Renderer>(), flashColor, 0.45f, true, 0.5f);

            GameObject smoke = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            smoke.name = "MuzzleSmoke";
            smoke.transform.position = puffOrigin + forward.normalized * 0.03f;
            smoke.transform.localScale = Vector3.one * 0.06f;
            RemoveCollider(smoke);
            ConfigureRenderer(smoke.GetComponent<Renderer>(), smokeColor, 0.35f, false, 0.15f);

            const float duration = 0.14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (flash != null)
                {
                    flash.transform.localScale = Vector3.one * (0.09f + fade * 0.08f);
                }

                if (smoke != null)
                {
                    smoke.transform.position = puffOrigin + forward.normalized * (0.03f + elapsed * 0.35f);
                    smoke.transform.localScale = Vector3.one * (0.06f + elapsed * 0.18f);
                }

                yield return null;
            }

            if (flash != null)
            {
                Destroy(flash);
            }

            if (smoke != null)
            {
                Destroy(smoke);
            }

            Destroy(gameObject);
        }

        private IEnumerator ElectricArcRoutine(Vector3 position, Vector3 forward)
        {
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            Vector3 start = position;
            Vector3 end = position + forward * 0.22f + Vector3.up * 0.02f;

            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            line.name = "ElectricArc";
            line.transform.position = (start + end) * 0.5f;
            line.transform.rotation = Quaternion.LookRotation(end - start) * Quaternion.Euler(90f, 0f, 0f);
            line.transform.localScale = new Vector3(0.012f, Vector3.Distance(start, end) * 0.5f, 0.012f);
            RemoveCollider(line);
            ConfigureRenderer(line.GetComponent<Renderer>(), new Color(0.3f, 0.9f, 1f), 0.2f, true, 0.65f);

            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "ElectricSpark";
            spark.transform.position = end;
            spark.transform.localScale = Vector3.one * 0.05f;
            RemoveCollider(spark);
            ConfigureRenderer(spark.GetComponent<Renderer>(), new Color(0.55f, 0.98f, 1f), 0.55f, true, 0.7f);

            const float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (line != null)
                {
                    Vector3 scale = line.transform.localScale;
                    scale.x = 0.012f * fade;
                    scale.z = 0.012f * fade;
                    line.transform.localScale = scale;
                }

                if (spark != null)
                {
                    spark.transform.localScale = Vector3.one * (0.05f * fade);
                }

                yield return null;
            }

            if (line != null)
            {
                Destroy(line);
            }

            if (spark != null)
            {
                Destroy(spark);
            }

            Destroy(gameObject);
        }

        private IEnumerator SlashPulseRoutine(Vector3 position, Vector3 forward)
        {
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slash.name = "ViewmodelSlashPulse";
            slash.transform.position = position + forward * 0.08f;
            slash.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 18f, 90f);
            slash.transform.localScale = new Vector3(0.14f, 0.004f, 0.05f);
            RemoveCollider(slash);
            ConfigureRenderer(slash.GetComponent<Renderer>(), new Color(0.92f, 0.96f, 1f), 0.18f, true, 0.4f);

            const float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (slash != null)
                {
                    slash.transform.localScale = new Vector3(0.14f + fade * 0.06f, 0.004f, 0.05f + fade * 0.03f);
                }

                yield return null;
            }

            if (slash != null)
            {
                Destroy(slash);
            }

            Destroy(gameObject);
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private static void ConfigureRenderer(Renderer renderer, Color color, float smoothness, bool glow, float emission)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
        }
    }
}
