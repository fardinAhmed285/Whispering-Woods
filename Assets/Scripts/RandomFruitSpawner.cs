using UnityEngine;
using UnityEngine.AI;

public class RandomFruitSpawner : MonoBehaviour
{
    [Header("Fruit Prefabs")]
    public GameObject[] fruitPrefabs;

    [Header("Spawning")]
    public int fruitCount = 50;
    public float spawnRadius = 50f;
    public float minDistanceBetweenFruits = 3f;
    public float spawnHeightOffset = 0f;

    [Header("Placement")]
    public bool useNavMesh = true;
    public bool randomRotation = true;
    public int maxAttemptsPerFruit = 30;

    [Header("Random Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    private void Start()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0)
        {
            Debug.LogError("Add your fruit prefabs to RandomFruitSpawner!", this);
            return;
        }

        if (!useRandomSeed)
            Random.InitState(seed);

        SpawnFruits();
    }

    void SpawnFruits()
    {
        int spawned = 0;
        int attempts = 0;
        int maxTotalAttempts = fruitCount * maxAttemptsPerFruit;

        while (spawned < fruitCount && attempts < maxTotalAttempts)
        {
            attempts++;

            Vector2 randomCircle =
                Random.insideUnitCircle * spawnRadius;

            Vector3 candidate = transform.position +
                new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (useNavMesh)
            {
                if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    5f,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                candidate = hit.position;
            }
            else
            {
                // Raycast down to find the ground.
                Vector3 rayOrigin = candidate + Vector3.up * 100f;

                if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit groundHit,
                    200f))
                {
                    candidate = groundHit.point;
                }
                else
                {
                    continue;
                }
            }

            candidate.y += spawnHeightOffset;

            bool tooClose = false;

            foreach (Transform child in transform)
            {
                if (Vector3.Distance(
                    child.position, candidate) <
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
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : prefab.transform.rotation;

            Instantiate(prefab, candidate, rotation, transform);

            spawned++;
        }

        Debug.Log(
            "Fruit spawning complete. Spawned " +
            spawned + " of " + fruitCount + " fruits.", this);
    }
}
