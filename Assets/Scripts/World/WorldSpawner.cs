using UnityEngine;

public class WorldSpawner : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private Transform propsRoot;

    [Header("Prefab Categories")]
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GameObject[] rockPrefabs;
    [SerializeField] private GameObject[] cliffPrefabs;
    [SerializeField] private GameObject[] bushPrefabs;
    [SerializeField] private GameObject[] grassPatchPrefabs;
    [SerializeField] private GameObject[] flowerPatchPrefabs;
    [SerializeField] private GameObject[] reedPrefabs;

    [Header("Additional Natural Prefabs")]
    [SerializeField] private GameObject[] branchPrefabs;

    [Header("Interactive Prefabs")]
    [SerializeField] private GameObject[] pickupStonePrefabs;

    [Header("Effect Prefabs")]
    [SerializeField] private GameObject[] floatingLeavesPrefabs;
    [SerializeField] private GameObject[] floatingPetalsPrefabs;
    [SerializeField] private GameObject[] butterflyAreaPrefabs;
    [SerializeField] private GameObject[] godRayPrefabs;

    [Header("Generation")]
    [SerializeField] private int maxAttemptsPerObject = 120;
    [SerializeField] private float edgePadding = 3f;
    [SerializeField] private float defaultYOffset = 0f;
    [SerializeField] private float pickupStoneYOffset = 0.18f;
    [SerializeField] private float pickupStoneMass = 0.12f;
    [SerializeField] private bool spawnCliffsNearEdges = true;
    [SerializeField] private float cliffEdgeBandWidth = 12f;

    [Header("Dense Ground Cover")]
    [Tooltip("Grass/flowers use more forgiving placement because they are decorative ground cover.")]
    [SerializeField] private int denseGroundCoverAttemptsPerObject = 35;
    [SerializeField] private bool denseGroundCoverIgnoresSafeRadius = false;

    [Header("Branch Interaction")]
    [SerializeField] private bool makeBranchesGrabbable = true;
    [SerializeField] private float branchMass = 0.18f;
    [SerializeField] private float branchColliderRadius = 0.16f;
    [SerializeField] private Vector3 branchColliderCenter = new Vector3(0f, 0.10f, 0f);

    [Header("Scale")]
    [SerializeField] private Vector2 treeScale = new Vector2(0.55f, 0.90f);
    [SerializeField] private Vector2 rockScale = new Vector2(0.30f, 0.70f);
    [SerializeField] private Vector2 cliffScale = new Vector2(0.55f, 0.95f);
    [SerializeField] private Vector2 bushScale = new Vector2(0.50f, 0.85f);
    [SerializeField] private Vector2 grassScale = new Vector2(0.85f, 1.45f);
    [SerializeField] private Vector2 flowerScale = new Vector2(0.95f, 1.65f);
    [SerializeField] private Vector2 reedScale = new Vector2(0.70f, 1.25f);
    [SerializeField] private Vector2 branchScale = new Vector2(0.16f, 0.34f);
    [SerializeField] private Vector2 pickupStoneScale = new Vector2(0.16f, 0.30f);
    [SerializeField] private Vector2 effectScale = new Vector2(0.35f, 0.65f);

    private RuntimeTerrainGenerator terrainGenerator;
    private WaterController waterController;
    private System.Random rng;

    public void GenerateWorld(GenerationResponse response, RuntimeTerrainGenerator runtimeTerrain, WaterController water)
    {
        if (response == null)
        {
            Debug.LogError("[WorldSpawner] Response is null.");
            return;
        }

        terrainGenerator = runtimeTerrain;
        waterController = water;

        if (terrainGenerator == null || terrainGenerator.ActiveTerrain == null)
        {
            Debug.LogError("[WorldSpawner] Terrain generator is missing.");
            return;
        }

        rng = new System.Random(response.seed);

        ClearPropsRoot();

        ObjectCounts counts = response.objects ?? new ObjectCounts();
        GenerationRules rules = response.rules ?? new GenerationRules();
        InteractiveCounts interactives = response.interactives ?? new InteractiveCounts();
        EffectCounts effects = response.effects ?? new EffectCounts();

        bool hasWater = response.audio != null && response.audio.waterEnabled;

        SpawnCategory("Trees", treePrefabs, counts.treeCount, rules.treeMaxSlope, treeScale, false, false, false, false, false, false, false, rules);
        SpawnCategory("Rocks", rockPrefabs, counts.rockCount, rules.rockMaxSlope, rockScale, true, false, false, false, false, false, false, rules);
        SpawnCategory("Branches", branchPrefabs, counts.branchCount, rules.rockMaxSlope, branchScale, true, false, false, false, true, false, false, rules);

        SpawnCategory("Cliffs", cliffPrefabs, counts.cliffCount, rules.cliffMaxSlope, cliffScale, false, false, false, false, false, false, spawnCliffsNearEdges, rules);
        SpawnCategory("Bushes", bushPrefabs, counts.bushCount, rules.bushMaxSlope, bushScale, false, false, false, false, false, false, false, rules);

        SpawnCategory("Grass", grassPatchPrefabs, counts.grassPatchCount, rules.grassMaxSlope, grassScale, false, false, true, false, false, true, false, rules);
        SpawnCategory("Flowers", flowerPatchPrefabs, counts.flowerPatchCount, rules.flowerMaxSlope, flowerScale, false, false, true, false, false, true, false, rules);

        if (hasWater)
            SpawnCategory("Reeds", reedPrefabs, counts.reedCount, rules.grassMaxSlope, reedScale, false, true, true, false, false, true, false, rules);

        SpawnCategory(
            "PickupStones",
            pickupStonePrefabs,
            interactives.pickupStoneCount,
            rules.rockMaxSlope,
            pickupStoneScale,
            true,
            hasWater,
            false,
            true,
            false,
            false,
            false,
            rules
        );

        SpawnDecorEffects("FloatingLeaves", floatingLeavesPrefabs, effects.floatingLeavesCount, 1.8f, 3.5f);
        SpawnDecorEffects("FloatingPetals", floatingPetalsPrefabs, effects.floatingPetalsCount, 1.5f, 3.0f);
        SpawnDecorEffects("Butterflies", butterflyAreaPrefabs, effects.butterflySpawnAreas, 0.3f, 1.2f);
        SpawnDecorEffects("GodRays", godRayPrefabs, effects.godRayCount, 1.2f, 2.6f);

        Debug.Log("[WorldSpawner] World objects generated.");
    }

    private void SpawnCategory(
        string categoryName,
        GameObject[] prefabs,
        int count,
        float maxSlope,
        Vector2 scaleRange,
        bool alignToSurface,
        bool nearWaterOnly,
        bool ignoreClearance,
        bool isPickupObject,
        bool isBranchObject,
        bool denseGroundCover,
        bool nearEdgeOnly,
        GenerationRules rules)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
        {
            Debug.Log($"[WorldSpawner] Category skipped: {categoryName}");
            return;
        }

        int spawned = 0;
        int attemptsPerObject = denseGroundCover
            ? Mathf.Max(5, denseGroundCoverAttemptsPerObject)
            : Mathf.Max(10, maxAttemptsPerObject);

        for (int i = 0; i < count; i++)
        {
            bool success = TrySpawnOne(
                prefabs,
                maxSlope,
                scaleRange,
                alignToSurface,
                nearWaterOnly,
                ignoreClearance,
                isPickupObject,
                isBranchObject,
                denseGroundCover,
                nearEdgeOnly,
                attemptsPerObject,
                rules
            );

            if (success)
                spawned++;
        }

        Debug.Log($"[WorldSpawner] {categoryName}: {spawned}/{count}");
    }

    private bool TrySpawnOne(
        GameObject[] prefabs,
        float maxSlope,
        Vector2 scaleRange,
        bool alignToSurface,
        bool nearWaterOnly,
        bool ignoreClearance,
        bool isPickupObject,
        bool isBranchObject,
        bool denseGroundCover,
        bool nearEdgeOnly,
        int attemptsPerObject,
        GenerationRules rules)
    {
        Terrain terrain = terrainGenerator.ActiveTerrain;
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        for (int attempt = 0; attempt < attemptsPerObject; attempt++)
        {
            Vector3 candidate = nearWaterOnly
                ? GetRandomPointNearWater()
                : nearEdgeOnly
                    ? GetRandomPointNearEdge(data, terrainPos)
                    : GetRandomPointOnTerrain(data, terrainPos);

            if (!denseGroundCoverIgnoresSafeRadius && !IsValidDistanceFromSpawn(candidate, rules.safeRadius))
                continue;

            if (!nearWaterOnly && waterController != null && waterController.IsInsideLake(candidate, rules.waterAvoidanceMargin))
                continue;

            if (nearWaterOnly && waterController != null && !waterController.IsNearLakeShore(candidate, 0.9f, 8.0f))
                continue;

            bool groundFound = terrainGenerator.TryGetTerrainPoint(
                candidate,
                maxSlope,
                out Vector3 groundPoint,
                out Vector3 normal,
                out float slope
            );

            if (!groundFound)
                continue;

            if (!ignoreClearance && !HasEnoughClearance(groundPoint, rules.minObjectDistance))
                continue;

            GameObject prefab = prefabs[rng.Next(0, prefabs.Length)];
            SpawnObject(prefab, groundPoint, normal, scaleRange, alignToSurface, isPickupObject, isBranchObject, denseGroundCover);
            return true;
        }

        return false;
    }

    private void SpawnDecorEffects(string categoryName, GameObject[] prefabs, int count, float minHeightOffset, float maxHeightOffset)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
        {
            Debug.Log($"[WorldSpawner] Effects skipped: {categoryName}");
            return;
        }

        Terrain terrain = terrainGenerator.ActiveTerrain;
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            Vector3 candidate = GetRandomPointOnTerrain(data, terrainPos);

            if (!terrainGenerator.TryGetTerrainPoint(candidate, 28f, out Vector3 groundPoint, out Vector3 normal, out float slope))
                continue;

            GameObject prefab = prefabs[rng.Next(0, prefabs.Length)];

            Vector3 position = groundPoint + Vector3.up * RandomRange(minHeightOffset, maxHeightOffset);
            Quaternion rotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);

            Transform parent = propsRoot != null ? propsRoot : transform;
            GameObject instance = Instantiate(prefab, position, rotation, parent);

            float scale = RandomRange(effectScale.x, effectScale.y);
            instance.transform.localScale *= scale;
            instance.name = $"{prefab.name}_GeneratedEffect";

            spawned++;
        }

        Debug.Log($"[WorldSpawner] {categoryName}: {spawned}/{count}");
    }

    private Vector3 GetRandomPointOnTerrain(TerrainData data, Vector3 terrainPos)
    {
        float x = RandomRange(terrainPos.x + edgePadding, terrainPos.x + data.size.x - edgePadding);
        float z = RandomRange(terrainPos.z + edgePadding, terrainPos.z + data.size.z - edgePadding);

        return new Vector3(x, 0f, z);
    }

    private Vector3 GetRandomPointNearEdge(TerrainData data, Vector3 terrainPos)
    {
        float band = Mathf.Clamp(cliffEdgeBandWidth, edgePadding, Mathf.Min(data.size.x, data.size.z) * 0.45f);
        float minX = terrainPos.x + edgePadding;
        float maxX = terrainPos.x + data.size.x - edgePadding;
        float minZ = terrainPos.z + edgePadding;
        float maxZ = terrainPos.z + data.size.z - edgePadding;

        int side = rng.Next(0, 4);
        float x = RandomRange(minX, maxX);
        float z = RandomRange(minZ, maxZ);

        switch (side)
        {
            case 0:
                x = RandomRange(minX, Mathf.Min(minX + band, maxX));
                break;
            case 1:
                x = RandomRange(Mathf.Max(maxX - band, minX), maxX);
                break;
            case 2:
                z = RandomRange(minZ, Mathf.Min(minZ + band, maxZ));
                break;
            default:
                z = RandomRange(Mathf.Max(maxZ - band, minZ), maxZ);
                break;
        }

        return new Vector3(x, 0f, z);
    }

    private Vector3 GetRandomPointNearWater()
    {
        if (waterController == null || !waterController.CurrentLake.enabled)
        {
            Terrain terrain = terrainGenerator.ActiveTerrain;
            return GetRandomPointOnTerrain(terrain.terrainData, terrain.transform.position);
        }

        WaterController.LakeData lake = waterController.CurrentLake;

        float angle = RandomRange(0f, Mathf.PI * 2f);
        float radius = RandomRange(lake.radius + 1.4f, lake.radius + 6.5f);

        float x = lake.center.x + Mathf.Cos(angle) * radius;
        float z = lake.center.z + Mathf.Sin(angle) * radius;

        return new Vector3(x, 0f, z);
    }

    private bool IsValidDistanceFromSpawn(Vector3 position, float safeRadius)
    {
        Vector3 spawn = terrainGenerator.GetSafeSpawnPosition(0f);
        Vector2 p = new Vector2(position.x - spawn.x, position.z - spawn.z);

        return p.magnitude >= safeRadius;
    }

    private bool HasEnoughClearance(Vector3 position, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius);

        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            if (propsRoot != null && collider.transform.IsChildOf(propsRoot))
                return false;
        }

        return true;
    }

    private void SpawnObject(
        GameObject prefab,
        Vector3 position,
        Vector3 normal,
        Vector2 scaleRange,
        bool alignToSurface,
        bool isPickupObject,
        bool isBranchObject,
        bool denseGroundCover)
    {
        if (prefab == null)
            return;

        Quaternion yRotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);

        Quaternion rotation = alignToSurface
            ? Quaternion.FromToRotation(Vector3.up, normal) * yRotation
            : yRotation;

        Transform parent = propsRoot != null ? propsRoot : transform;
        float yOffset = isPickupObject ? pickupStoneYOffset : defaultYOffset;

        GameObject instance = Instantiate(prefab, position + normal.normalized * yOffset, rotation, parent);

        float scale = RandomRange(scaleRange.x, scaleRange.y);

        if (isPickupObject)
        {
            // Pickup stones must appear small immediately. Some source prefabs have large authoring scale,
            // so multiplying by the prefab scale causes the first spawn to be huge.
            instance.transform.localScale = Vector3.one * scale;
        }
        else
        {
            instance.transform.localScale *= scale;
        }

        instance.name = $"{prefab.name}_Generated";

        if (isPickupObject)
            ConfigurePickupPhysics(instance);
        else if (isBranchObject && makeBranchesGrabbable)
            ConfigureBranchPhysics(instance);
    }

    private void ConfigurePickupPhysics(GameObject instance)
    {
        if (instance == null)
            return;

        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null)
            rb = instance.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = pickupStoneMass;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.maxAngularVelocity = 8f;
        rb.solverIterations = 12;
        rb.solverVelocityIterations = 12;

        Collider[] colliders = instance.GetComponentsInChildren<Collider>();

        if (colliders == null || colliders.Length == 0)
        {
            SphereCollider sphereCollider = instance.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.15f;
            sphereCollider.isTrigger = false;
        }
        else
        {
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                    continue;

                collider.isTrigger = false;

                MeshCollider meshCollider = collider as MeshCollider;
                if (meshCollider != null)
                    meshCollider.convex = true;
            }
        }

        ResetIfFallen reset = instance.GetComponent<ResetIfFallen>();
        if (reset == null)
            instance.AddComponent<ResetIfFallen>();

        var grab = instance.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
            instance.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        ReturnToStartAfterRelease returner = instance.GetComponent<ReturnToStartAfterRelease>();
        if (returner == null)
            returner = instance.AddComponent<ReturnToStartAfterRelease>();

        returner.SetCurrentTransformAsReturnPoint();

        ThrowableStoneCounter counter = instance.GetComponent<ThrowableStoneCounter>();
        if (counter == null)
            instance.AddComponent<ThrowableStoneCounter>();
    }

    private void ConfigureBranchPhysics(GameObject instance)
    {
        if (instance == null)
            return;

        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null)
            rb = instance.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = branchMass;
        rb.linearDamping = 0.08f;
        rb.angularDamping = 0.08f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Collider[] colliders = instance.GetComponentsInChildren<Collider>();

        if (colliders == null || colliders.Length == 0)
        {
            SphereCollider sphereCollider = instance.AddComponent<SphereCollider>();
            sphereCollider.center = branchColliderCenter;
            sphereCollider.radius = branchColliderRadius;
            sphereCollider.isTrigger = false;
        }
        else
        {
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                    continue;

                collider.isTrigger = false;

                MeshCollider meshCollider = collider as MeshCollider;
                if (meshCollider != null)
                    meshCollider.convex = true;
            }
        }

        var grab = instance.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
            instance.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        ResetIfFallen reset = instance.GetComponent<ResetIfFallen>();
        if (reset == null)
            instance.AddComponent<ResetIfFallen>();

        ReturnToStartAfterRelease returner = instance.GetComponent<ReturnToStartAfterRelease>();
        if (returner == null)
            returner = instance.AddComponent<ReturnToStartAfterRelease>();

        returner.SetCurrentTransformAsReturnPoint();
    }

    private float RandomRange(float min, float max)
    {
        return (float)(min + rng.NextDouble() * (max - min));
    }

    private void ClearPropsRoot()
    {
        if (propsRoot == null)
            return;

        for (int i = propsRoot.childCount - 1; i >= 0; i--)
            Destroy(propsRoot.GetChild(i).gameObject);
    }
}
