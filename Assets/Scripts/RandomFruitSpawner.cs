
using UnityEngine;
using UnityEngine.AI;

public class RandomFruitSpawner : MonoBehaviour
{
    [Header("Fruit Prefabs")]
    public GameObject[] fruitPrefabs;

    [Header("Ground")]
    public Terrain terrain;
    public LayerMask groundLayers = ~0;
    public float raycastHeight = 200f;
    public float raycastDistance = 500f;
    public float spawnHeightOffset = 0.05f;

    [Header("Spawning")]
    public int fruitCount = 50;
    public float spawnRadius = 50f;
    public float minDistanceBetweenFruits = 3f;

    [Header("Placement")]
    public bool useNavMesh = false;
    public bool randomRotation = true;
    public int maxAttemptsPerFruit = 30;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    private void Start()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0)
        {
            Debug.LogError(
                "Add your fruit prefabs to RandomFruitSpawner!", this);
            return;
        }

        if (!useRandomSeed)
            Random.InitState(seed);

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        SpawnFruits();
    }

    void SpawnFruits()
    {
        int spawned = 0;
        int attempts = 0;
        int maxTotalAttempts =
            fruitCount * Mathf.Max(1, maxAttemptsPerFruit);

        while (spawned < fruitCount && attempts < maxTotalAttempts)
        {
            attempts++;

            Vector2 offset =
                Random.insideUnitCircle * spawnRadius;

            Vector3 candidate = transform.position +
                new Vector3(offset.x, 0f, offset.y);

            Vector3 groundPosition;

            if (!TryGetGroundPosition(candidate, out groundPosition))
                continue;

            if (useNavMesh)
            {
                if (!NavMesh.SamplePosition(
                    groundPosition,
                    out NavMeshHit navHit,
                    2f,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                // Keep the actual ground height, not the NavMesh height.
                if (Vector3.Distance(
                    navHit.position, groundPosition) > 2f)
                {
                    continue;
                }
            }

            bool tooClose = false;

            foreach (Transform child in transform)
            {
                if (Vector3.Distance(
                    child.position, groundPosition) <
                    minDistanceBetweenFruits)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            GameObject prefab = fruitPrefabs[
                Random.Range(0, fruitPrefabs.Length)];

            Quaternion rotation = randomRotation
                ? Quaternion.Euler(
                    0f, Random.Range(0f, 360f), 0f)
                : prefab.transform.rotation;

            Vector3 spawnPosition = groundPosition;
            spawnPosition.y += spawnHeightOffset;

            GameObject fruit = Instantiate(
                prefab, spawnPosition, rotation, transform);

            // Ensure spawned fruit can be detected by the wolf.
            if (!fruit.CompareTag("Fruit"))
                fruit.tag = "Fruit";

            spawned++;
        }

        Debug.Log(
            "Fruit spawning complete. Spawned " +
            spawned + " of " + fruitCount + " fruits.", this);
    }

    bool TryGetGroundPosition(
        Vector3 candidate, out Vector3 groundPosition)
    {
        groundPosition = candidate;

        // Prefer the actual Terrain surface height.
        if (terrain != null)
        {
            Vector3 local = candidate - terrain.transform.position;
            TerrainData data = terrain.terrainData;
            Vector3 size = data.size;

            if (local.x >= 0f && local.x <= size.x &&
                local.z >= 0f && local.z <= size.z)
            {
                float normalizedX = local.x / size.x;
                float normalizedZ = local.z / size.z;

                float height = data.GetInterpolatedHeight(
                    normalizedX, normalizedZ);

                groundPosition = new Vector3(
                    candidate.x,
                    terrain.transform.position.y + height,
                    candidate.z);

                return true;
            }
        }

        // Fallback for non-Terrain ground surfaces.
        Vector3 rayOrigin = new Vector3(
            candidate.x,
            candidate.y + raycastHeight,
            candidate.z);

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            raycastDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;
            return true;
        }

        return false;
    }
}
