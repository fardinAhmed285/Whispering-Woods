using UnityEngine;
using UnityEditor;

public class ForestGenerator : EditorWindow
{
    private Terrain terrain;
    private GameObject[] trees = new GameObject[0];
    private GameObject[] rocks = new GameObject[0];
    private GameObject[] flowers = new GameObject[0];
    private Vector2 scrollPosition;
    private int treeCount = 120;
    private int rockCount = 35;
    private int flowerCount = 100;
    private int seed = 42;

    private float clearingRadius = 15f;

    [MenuItem("Whispering Woods/Forest Generator")]
    public static void ShowWindow()
    {
        GetWindow<ForestGenerator>("Forest Generator");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Whispering Woods", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Assign your Terrain and imported nature prefabs. " +
            "The generator will place objects automatically.",
            MessageType.Info
        );

        terrain = (Terrain)EditorGUILayout.ObjectField(
            "Terrain", terrain, typeof(Terrain), true);

        trees = DrawPrefabList("Tree Prefabs", trees);
        rocks = DrawPrefabList("Rock Prefabs", rocks);
        flowers = DrawPrefabList("Flower Prefabs", flowers);

        treeCount = EditorGUILayout.IntField("Tree Count", treeCount);
        rockCount = EditorGUILayout.IntField("Rock Count", rockCount);
        flowerCount = EditorGUILayout.IntField("Flower Count", flowerCount);

        clearingRadius = EditorGUILayout.Slider(
            "Clearing Radius", clearingRadius, 5f, 30f);

        seed = EditorGUILayout.IntField("Random Seed", seed);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(terrain == null))
        {
            if (GUILayout.Button("Generate Forest", GUILayout.Height(35)))
                GenerateForest();
        }

        if (GUILayout.Button("Clear Generated Forest"))
            ClearGeneratedForest();

        EditorGUILayout.EndScrollView();
    }

    private GameObject[] DrawPrefabList(
        string label, GameObject[] prefabs)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        int count = Mathf.Max(0, EditorGUILayout.IntField(
            "Number of Prefabs", prefabs.Length));

        System.Array.Resize(ref prefabs, count);

        for (int i = 0; i < prefabs.Length; i++)
        {
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Prefab {i + 1}", prefabs[i],
                typeof(GameObject), false);
        }

        return prefabs;
    }

    private void GenerateForest()
    {
        if (terrain == null)
        {
            Debug.LogError("Assign a Terrain first.");
            return;
        }

        ClearGeneratedForest();

        Random.InitState(seed);

        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;

        GameObject root = new GameObject("GeneratedForest");
        Undo.RegisterCreatedObjectUndo(root, "Generate Forest");

        root.transform.SetParent(terrain.transform, true);

        PlaceObjects(root.transform, trees, treeCount,
            origin, size, clearingRadius, true);

        PlaceObjects(root.transform, rocks, rockCount,
            origin, size, clearingRadius, false);

        PlaceObjects(root.transform, flowers, flowerCount,
            origin, size, clearingRadius, false);

        Selection.activeGameObject = root;
        Debug.Log("Forest generated successfully.");
    }

    private void PlaceObjects(
        Transform parent,
        GameObject[] prefabs,
        int count,
        Vector3 origin,
        Vector3 size,
        float clearRadius,
        bool avoidClearing)
    {
        if (prefabs == null || prefabs.Length == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[
                Random.Range(0, prefabs.Length)];

            if (prefab == null)
                continue;

            Vector3 position = Vector3.zero;
            bool foundPosition = false;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                float x = Random.Range(3f, size.x - 3f);
                float z = Random.Range(3f, size.z - 3f);

                float centerX = size.x * 0.5f;
                float centerZ = size.z * 0.5f;

                float dx = x - centerX;
                float dz = z - centerZ;

                if (avoidClearing &&
                    dx * dx + dz * dz < clearRadius * clearRadius)
                    continue;

                position = new Vector3(
                    origin.x + x,
                    origin.y + terrainHeight(x, z),
                    origin.z + z);

                foundPosition = true;
                break;
            }

            if (!foundPosition)
                continue;

            GameObject instance = (GameObject)PrefabUtility
                .InstantiatePrefab(prefab);

            if (instance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(instance, "Place Forest Object");
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(
                0f, Random.Range(0f, 360f), 0f);

            if (prefab.CompareTag("Untagged"))
                instance.transform.localScale *= Random.Range(0.8f, 1.2f);
        }
    }

    private float terrainHeight(float x, float z)
    {
        return terrain.SampleHeight(
            new Vector3(
                terrain.transform.position.x + x,
                0f,
                terrain.transform.position.z + z));
    }

    private void ClearGeneratedForest()
    {
        Transform root = terrain != null
            ? terrain.transform.Find("GeneratedForest")
            : null;

        if (root != null)
            Undo.DestroyObjectImmediate(root.gameObject);
    }
}
