using UnityEngine;

public class TerrainBoundaryBuilder : MonoBehaviour
{
    [Header("Boundary")]
    [SerializeField] private Transform boundaryRoot;
    [SerializeField] private float wallHeight = 6f;
    [SerializeField] private float wallThickness = 1f;
    [SerializeField] private float inwardOffset = 0.5f;

    [Header("Physics")]
    [SerializeField] private PhysicsMaterial wallPhysicsMaterial;

    public void RebuildBoundaries(Terrain terrain)
    {
        ClearBoundaries();

        if (terrain == null)
        {
            Debug.LogWarning("[TerrainBoundaryBuilder] Terrain is null.");
            return;
        }

        TerrainData data = terrain.terrainData;
        Vector3 pos = terrain.transform.position;

        float minX = pos.x + inwardOffset;
        float maxX = pos.x + data.size.x - inwardOffset;
        float minZ = pos.z + inwardOffset;
        float maxZ = pos.z + data.size.z - inwardOffset;

        float centerY = pos.y + wallHeight * 0.5f;

        CreateWall(
            "Boundary_North",
            new Vector3((minX + maxX) * 0.5f, centerY, maxZ),
            new Vector3(data.size.x, wallHeight, wallThickness)
        );

        CreateWall(
            "Boundary_South",
            new Vector3((minX + maxX) * 0.5f, centerY, minZ),
            new Vector3(data.size.x, wallHeight, wallThickness)
        );

        CreateWall(
            "Boundary_East",
            new Vector3(maxX, centerY, (minZ + maxZ) * 0.5f),
            new Vector3(wallThickness, wallHeight, data.size.z)
        );

        CreateWall(
            "Boundary_West",
            new Vector3(minX, centerY, (minZ + maxZ) * 0.5f),
            new Vector3(wallThickness, wallHeight, data.size.z)
        );

        Debug.Log("[TerrainBoundaryBuilder] Invisible terrain boundaries rebuilt.");
    }

    public void ClearBoundaries()
    {
        Transform root = boundaryRoot != null ? boundaryRoot : transform;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private void CreateWall(string wallName, Vector3 position, Vector3 size)
    {
        Transform root = boundaryRoot != null ? boundaryRoot : transform;

        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(root);
        wall.transform.position = position;
        wall.transform.rotation = Quaternion.identity;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;

        if (wallPhysicsMaterial != null)
            collider.material = wallPhysicsMaterial;

        wall.layer = gameObject.layer;
    }
}