using System.Collections.Generic;
using UnityEngine;

public class OrbitWeapon : WeaponBase
{
    private const float OrbScale = 0.45f;

    private Transform orbitCenter;
    private readonly List<OrbitOrb> activeOrbs = new List<OrbitOrb>();
    private float currentAngle;

    public void Configure(Transform center)
    {
        orbitCenter = center;
    }

    public override void Init(PlayerStats stats)
    {
        base.Init(stats);
        SyncOrbCount();
    }

    public override void Tick()
    {
        if (playerStats == null || orbitCenter == null) return;

        damage = playerStats.damage;

        if (activeOrbs.Count != playerStats.OrbitOrbCount)
        {
            SyncOrbCount();
        }

        if (activeOrbs.Count == 0) return;

        currentAngle += playerStats.OrbitSpeed * Time.deltaTime;

        float angleStep = 360f / activeOrbs.Count;

        for (int i = 0; i < activeOrbs.Count; i++)
        {
            OrbitOrb orb = activeOrbs[i];

            if (orb == null) continue;

            float angle = currentAngle + angleStep * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(radians),
                0.5f,
                Mathf.Sin(radians)
            ) * playerStats.OrbitRadius;

            Vector3 targetPosition = orbitCenter.position + offset;
            orb.transform.position = targetPosition;
        }
    }

    public override void Fire()
    {
    }

    public void RefreshOrbs()
    {
        SyncOrbCount();
    }

    private void SyncOrbCount()
    {
        int targetCount = playerStats != null ? playerStats.OrbitOrbCount : 0;

        while (activeOrbs.Count < targetCount)
        {
            activeOrbs.Add(CreateOrb(activeOrbs.Count));
        }

        while (activeOrbs.Count > targetCount)
        {
            int lastIndex = activeOrbs.Count - 1;
            OrbitOrb orb = activeOrbs[lastIndex];

            if (orb != null)
            {
                Object.Destroy(orb.gameObject);
            }

            activeOrbs.RemoveAt(lastIndex);
        }
    }

    private OrbitOrb CreateOrb(int index)
    {
        GameObject orbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbObject.name = "OrbitOrb_" + (index + 1);

        Collider defaultCollider = orbObject.GetComponent<Collider>();

        if (defaultCollider != null)
        {
            Object.Destroy(defaultCollider);
        }

        orbObject.transform.localScale = Vector3.one * OrbScale;

        Renderer renderer = orbObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, GameVisualPalette.OrbitOrb, 0.82f, true);
        }

        OrbitOrb orb = orbObject.AddComponent<OrbitOrb>();
        orb.Init(playerStats);
        return orb;
    }
}
