using UnityEngine;

public class FrostSigilSkill : MonoBehaviour
{
    private const float WarningDuration = 0.35f;
    private const float PlayerSearchRadius = 22f;
    private const float ClusterRadius = 3.5f;

    private float cooldownTimer;
    private bool warningActive;
    private float warningTimer;
    private Vector3 pendingCenter;
    private float pendingRadius;
    private int pendingDamage;
    private string pendingDamageSource;
    private GameObject warningVisual;

    private void Update()
    {
        if (!MainMenuManager.IsRunActive || Time.timeScale <= 0f)
        {
            return;
        }

        int level = GetSkillLevel();

        if (level <= 0)
        {
            return;
        }

        if (warningActive)
        {
            warningTimer -= Time.deltaTime;

            if (warningTimer <= 0f)
            {
                warningActive = false;
                DetonateFrost();
            }

            return;
        }

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
        {
            return;
        }

        if (!TryFindTargetCenter(out Vector3 center))
        {
            return;
        }

        BeginWarning(center, level);
        cooldownTimer = GetCooldown(level);
    }

    private void BeginWarning(Vector3 center, int level)
    {
        pendingCenter = center;
        pendingRadius = GetRadius(level);
        pendingDamage = GetDamage(level);
        pendingDamageSource = IsGlacialPrisonActive() ? "Glacial Prison" : "Frost Sigil";
        warningActive = true;
        warningTimer = WarningDuration;

        if (warningVisual != null)
        {
            Destroy(warningVisual);
        }

        warningVisual = SpawnWarningRing(pendingCenter, pendingRadius, WarningDuration, IsGlacialPrisonActive());
    }

    private void DetonateFrost()
    {
        if (warningVisual != null)
        {
            Destroy(warningVisual);
            warningVisual = null;
        }

        bool evolved = IsGlacialPrisonActive();
        SpawnImpactFrost(pendingCenter, pendingRadius, evolved);
        DamageEnemiesInRadius(pendingCenter, pendingRadius, pendingDamage, pendingDamageSource);

        // TODO: Apply movement slow when enemy slow system exists.
    }

    private static int GetSkillLevel()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(UpgradeOptionCatalog.FrostSigilIndex);
    }

    private static int GetPassiveLevel()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(UpgradeOptionCatalog.CryoCoreIndex);
    }

    private static bool IsGlacialPrisonActive()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        return tracker != null && tracker.HasEvolution(BuildEvolutionId.GlacialPrison);
    }

    private static float GetCooldown(int level)
    {
        return Mathf.Max(2.8f, 5f - level * 0.15f);
    }

    private static float GetRadius(int level)
    {
        float radius = 2.2f + level * 0.08f;
        int passiveLevel = GetPassiveLevel();
        radius *= 1f + passiveLevel * 0.03f;

        if (IsGlacialPrisonActive())
        {
            radius *= 1.25f;
        }

        return radius;
    }

    private static int GetDamage(int level)
    {
        float damage = 6f + level * 2f;
        int passiveLevel = GetPassiveLevel();
        damage *= 1f + passiveLevel * 0.08f;

        if (IsGlacialPrisonActive())
        {
            damage *= 1.35f;
        }

        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    private static bool TryFindTargetCenter(out Vector3 center)
    {
        center = Vector3.zero;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return false;
        }

        Vector3 playerPosition = player.transform.position;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            return false;
        }

        Transform bestEnemy = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            if (enemyObject.GetComponent<Enemy>() == null)
            {
                continue;
            }

            float distanceToPlayer = Vector3.Distance(playerPosition, enemyObject.transform.position);

            if (distanceToPlayer > PlayerSearchRadius)
            {
                continue;
            }

            int neighborCount = CountNeighbors(enemyObject.transform.position, enemies, ClusterRadius);
            float score = neighborCount * 12f - distanceToPlayer;

            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = enemyObject.transform;
            }
        }

        if (bestEnemy == null)
        {
            return false;
        }

        center = bestEnemy.position;
        center.y = playerPosition.y + 0.05f;
        return true;
    }

    private static int CountNeighbors(Vector3 position, GameObject[] enemies, float radius)
    {
        int count = 0;
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            if ((enemyObject.transform.position - position).sqrMagnitude <= radiusSqr)
            {
                count++;
            }
        }

        return count;
    }

    private static void DamageEnemiesInRadius(Vector3 center, float radius, int damage, string sourceName)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null)
            {
                continue;
            }

            if ((enemyObject.transform.position - center).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt(sourceName, damage);
            enemy.TakeDamage(damage);
        }
    }

    private static GameObject SpawnWarningRing(Vector3 center, float radius, float duration, bool evolved)
    {
        GameObject root = new GameObject("FrostSigilWarning");
        root.transform.position = center + Vector3.up * 0.06f;

        FrostSigilWarningFx warningFx = root.AddComponent<FrostSigilWarningFx>();
        warningFx.Initialize(radius, duration, evolved);
        Destroy(root, duration + 0.08f);
        return root;
    }

    private static void SpawnImpactFrost(Vector3 center, float radius, bool evolved)
    {
        Vector3 ground = center + Vector3.up * 0.04f;

        SpawnGroundDisc(ground, radius * 0.92f, evolved ? new Color(0.72f, 0.96f, 1f, 0.62f) : new Color(0.62f, 0.9f, 1f, 0.5f), 0.42f);
        SpawnGroundRingMesh(ground, radius * 1.05f, evolved ? new Color(0.88f, 1f, 1f, 0.78f) : new Color(0.7f, 0.94f, 1f, 0.62f), 0.28f, evolved ? 0.05f : 0.035f);

        int shardCount = evolved ? 10 : 7;

        for (int i = 0; i < shardCount; i++)
        {
            float angle = (i / (float)shardCount) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * Random.Range(0.35f, 0.82f);
            SpawnIceShard(ground + offset, evolved);
        }

        if (evolved)
        {
            GameObject pulseRoot = new GameObject("FrostSigilPulse");
            pulseRoot.transform.position = ground;
            FrostSigilPulseFx pulseFx = pulseRoot.AddComponent<FrostSigilPulseFx>();
            pulseFx.Initialize(radius, 0.32f);
            Destroy(pulseRoot, 0.35f);
        }

        SpawnSparkle(ground + Vector3.up * 0.08f, evolved ? 0.16f : 0.12f, evolved ? new Color(0.92f, 1f, 1f, 0.95f) : new Color(0.78f, 0.96f, 1f, 0.85f), 0.22f);
    }

    private static void SpawnIceShard(Vector3 position, bool evolved)
    {
        GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shard.name = "FrostSigilShard";
        shard.transform.position = position + Vector3.up * 0.08f;
        shard.transform.localScale = evolved
            ? new Vector3(0.12f, 0.22f, 0.12f)
            : new Vector3(0.09f, 0.16f, 0.09f);
        shard.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);

        RemoveCollider(shard);
        ApplyColor(shard, evolved ? new Color(0.82f, 0.98f, 1f, 0.9f) : new Color(0.68f, 0.92f, 1f, 0.82f));

        FrostSigilShardFx shardFx = shard.AddComponent<FrostSigilShardFx>();
        shardFx.Initialize(Random.Range(0.18f, 0.3f), evolved ? 1.35f : 1.15f);
        Destroy(shard, 0.35f);
    }

    private static void SpawnSparkle(Vector3 position, float size, Color color, float lifetime)
    {
        GameObject sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sparkle.name = "FrostSigilSparkle";
        sparkle.transform.position = position;
        sparkle.transform.localScale = Vector3.one * size;

        RemoveCollider(sparkle);
        ApplyColor(sparkle, color);
        Destroy(sparkle, lifetime);
    }

    private static void SpawnGroundDisc(Vector3 center, float radius, Color color, float lifetime)
    {
        GameObject discObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discObject.name = "FrostSigilDisc";
        discObject.transform.position = center;
        discObject.transform.localScale = new Vector3(radius * 1.85f, 0.02f, radius * 1.85f);

        RemoveCollider(discObject);
        ApplyColor(discObject, color);
        Destroy(discObject, Mathf.Max(0.1f, lifetime));
    }

    private static void SpawnGroundRingMesh(Vector3 center, float radius, Color color, float lifetime, float height)
    {
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "FrostSigilImpactRing";
        ringObject.transform.position = center + Vector3.up * (height * 0.5f);
        ringObject.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);

        RemoveCollider(ringObject);
        ApplyColor(ringObject, color);
        Destroy(ringObject, Mathf.Max(0.1f, lifetime));
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return;
        }

        Material material = new Material(shader);
        material.color = color;
        renderer.material = material;
    }

    private sealed class FrostSigilWarningFx : MonoBehaviour
    {
        private LineRenderer outerRing;
        private LineRenderer innerRing;
        private float duration;
        private float elapsed;
        private bool evolved;
        private Color outerBase;
        private Color innerBase;

        public void Initialize(float radius, float life, bool isEvolved)
        {
            duration = Mathf.Max(0.1f, life);
            evolved = isEvolved;
            outerBase = evolved ? new Color(0.62f, 0.95f, 1f, 0.72f) : new Color(0.5f, 0.88f, 1f, 0.58f);
            innerBase = evolved ? new Color(0.88f, 1f, 1f, 0.42f) : new Color(0.72f, 0.96f, 1f, 0.32f);

            outerRing = CreateRing("FrostSigilWarningOuter", radius, evolved ? 0.09f : 0.065f);
            innerRing = CreateRing("FrostSigilWarningInner", radius * 0.72f, evolved ? 0.045f : 0.03f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float pulse = 0.65f + Mathf.Sin(elapsed * 24f) * 0.35f;
            float fadeIn = Mathf.Clamp01(elapsed / Mathf.Max(0.08f, duration * 0.25f));
            float fadeOut = 1f - Mathf.Clamp01((elapsed - duration * 0.72f) / Mathf.Max(0.08f, duration * 0.28f));
            float alphaScale = fadeIn * fadeOut * pulse;

            ApplyRingColor(outerRing, outerBase, alphaScale);
            ApplyRingColor(innerRing, innerBase, alphaScale * 0.85f);
        }

        private LineRenderer CreateRing(string objectName, float radius, float width)
        {
            GameObject ringObject = new GameObject(objectName);
            ringObject.transform.SetParent(transform, false);

            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthMultiplier = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.material = CreateLineMaterial();

            const int segments = 48;

            line.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            return line;
        }

        private static void ApplyRingColor(LineRenderer line, Color baseColor, float alphaScale)
        {
            if (line == null)
            {
                return;
            }

            Color color = baseColor;
            color.a = baseColor.a * alphaScale;
            line.startColor = color;
            line.endColor = color;
        }

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            return shader != null ? new Material(shader) : null;
        }
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        return shader != null ? new Material(shader) : null;
    }

    private sealed class FrostSigilShardFx : MonoBehaviour
    {
        private float lifetime;
        private float elapsed;
        private float riseSpeed;
        private Vector3 startScale;
        private Renderer cachedRenderer;
        private Color baseColor;

        public void Initialize(float life, float rise)
        {
            lifetime = life;
            riseSpeed = rise;
            startScale = transform.localScale;
            cachedRenderer = GetComponent<Renderer>();

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                baseColor = cachedRenderer.material.color;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = lifetime > 0f ? Mathf.Clamp01(elapsed / lifetime) : 1f;

            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            transform.localScale = startScale * (1f + t * 0.35f);

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                Color color = baseColor;
                color.a = baseColor.a * (1f - t);
                cachedRenderer.material.color = color;
            }
        }
    }

    private sealed class FrostSigilPulseFx : MonoBehaviour
    {
        private LineRenderer pulseRing;
        private float duration;
        private float elapsed;
        private float startRadius;

        public void Initialize(float radius, float life)
        {
            duration = life;
            startRadius = radius * 0.75f;
            pulseRing = CreateRing(radius * 0.9f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float scale = Mathf.Lerp(0.85f, 1.18f, t);
            pulseRing.widthMultiplier = Mathf.Lerp(0.08f, 0.02f, t);

            for (int i = 0; i < pulseRing.positionCount; i++)
            {
                Vector3 local = pulseRing.GetPosition(i);
                pulseRing.SetPosition(i, local.normalized * startRadius * scale);
            }

            Color color = new Color(0.9f, 1f, 1f, 0.75f * (1f - t));
            pulseRing.startColor = color;
            pulseRing.endColor = color;
        }

        private LineRenderer CreateRing(float radius)
        {
            LineRenderer line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthMultiplier = 0.08f;
            line.material = CreateLineMaterial();

            const int segments = 40;
            line.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.02f, Mathf.Sin(angle) * radius));
            }

            return line;
        }
    }
}
