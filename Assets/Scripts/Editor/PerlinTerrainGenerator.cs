using UnityEngine;
using UnityEditor;

public class PerlinTerrainGenerator : EditorWindow
{
    private Terrain terrain;
    private float noiseScale = 0.035f;
    private float heightVariation = 0.4f;
    private float baseHeight = 0.4f;
    private int seed = 42;

    private Vector2 scroll;

    [MenuItem("Whispering Woods/Perlin Terrain")]
    public static void ShowWindow()
    {
        GetWindow<PerlinTerrainGenerator>("Perlin Terrain");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Perlin Noise Terrain", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField(
            "Terrain", terrain, typeof(Terrain), true);

        noiseScale = EditorGUILayout.Slider(
            "Noise Scale", noiseScale, 0.005f, 0.1f);

        heightVariation = EditorGUILayout.Slider(
            "Height Variation", heightVariation, 0.05f, 0.8f);

        baseHeight = EditorGUILayout.Slider(
            "Base Height", baseHeight, 0.1f, 0.8f);

        seed = EditorGUILayout.IntField("Seed", seed);

        EditorGUILayout.HelpBox(
            "Save your scene before generating. This replaces " +
            "the existing terrain heights.",
            MessageType.Warning);

        using (new EditorGUI.DisabledScope(terrain == null))
        {
            if (GUILayout.Button("Generate Perlin Terrain", GUILayout.Height(35)))
                GenerateTerrain();
        }

        EditorGUILayout.EndScrollView();
    }

    private void GenerateTerrain()
    {
        TerrainData data = terrain.terrainData;
        int resolution = data.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        System.Random random = new System.Random(seed);
        float offsetX = (float)random.NextDouble() * 10000f;
        float offsetZ = (float)random.NextDouble() * 10000f;

        Undo.RegisterCompleteObjectUndo(data, "Generate Perlin Terrain");

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = x * noiseScale + offsetX;
                float nz = z * noiseScale + offsetZ;

                float broad = Mathf.PerlinNoise(nx * 0.5f, nz * 0.5f);
                float detail = Mathf.PerlinNoise(nx, nz);

                float noise = broad * 0.7f + detail * 0.3f;

                heights[z, x] = Mathf.Clamp01(
                    baseHeight + (noise - 0.5f) * heightVariation);
            }
        }

        data.SetHeights(0, 0, heights);
        terrain.Flush();

        Debug.Log("Perlin noise terrain generated.");
    }
}
