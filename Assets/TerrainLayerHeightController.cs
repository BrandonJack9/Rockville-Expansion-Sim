using UnityEngine;
using UnityEngine.UI;

public class TerrainLayerHeightController : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain terrain;
    public int targetLayerIndex = 0; // Index of the terrain layer to raise
    [Range(0f, 1f)]
    public float targetHeight = 0.6f; // Target normalized height when fully raised
    public float changeSpeed = 1.5f;

    [Header("Slider Control")]
    public Slider heightSlider;
    [Range(0f, 1f)]
    public float initialSliderValue = 0f;

    private TerrainData terrainData;
    private int heightmapWidth, heightmapHeight;
    private int alphamapWidth, alphamapHeight, numLayers;
    private float[,] originalHeights;
    private float sliderValue;

    void Start()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("No terrain assigned.");
            enabled = false;
            return;
        }

        terrainData = terrain.terrainData;

        heightmapWidth = terrainData.heightmapResolution;
        heightmapHeight = terrainData.heightmapResolution;

        alphamapWidth = terrainData.alphamapWidth;
        alphamapHeight = terrainData.alphamapHeight;
        numLayers = terrainData.alphamapLayers;

        if (targetLayerIndex < 0 || targetLayerIndex >= numLayers)
        {
            Debug.LogError("Invalid terrain layer index.");
            enabled = false;
            return;
        }

        // Cache original heights
        originalHeights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        // Setup slider
        if (heightSlider != null)
        {
            sliderValue = Mathf.Clamp01(heightSlider.value);
            heightSlider.onValueChanged.AddListener(val => sliderValue = Mathf.Clamp01(val));
        }
        else
        {
            sliderValue = initialSliderValue;
        }
    }

    void Update()
    {
        float[,] currentHeights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);
        float[,] newHeights = new float[heightmapHeight, heightmapWidth];

        // Get the alpha map (texture paint weights)
        float[,,] alphaMap = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);

        // Heightmap and alphamap sizes are different — compute ratio
        float ratioX = (float)alphamapWidth / heightmapWidth;
        float ratioZ = (float)alphamapHeight / heightmapHeight;

        float step = changeSpeed * Time.deltaTime;

        for (int z = 0; z < heightmapHeight; z++)
        {
            for (int x = 0; x < heightmapWidth; x++)
            {
                // Find the corresponding alpha map coordinate
                int alphaX = Mathf.Clamp(Mathf.FloorToInt(x * ratioX), 0, alphamapWidth - 1);
                int alphaZ = Mathf.Clamp(Mathf.FloorToInt(z * ratioZ), 0, alphamapHeight - 1);

                float mask = alphaMap[alphaZ, alphaX, targetLayerIndex]; // weight of the selected layer (0–1)
                float blend = mask * sliderValue;

                float originalH = originalHeights[z, x];
                float targetH = Mathf.Lerp(originalH, targetHeight, blend);

                newHeights[z, x] = Mathf.MoveTowards(currentHeights[z, x], targetH, step);
            }
        }

        terrainData.SetHeights(0, 0, newHeights);
    }

    void OnDisable()
    {
        if (terrainData != null && originalHeights != null)
        {
            terrainData.SetHeights(0, 0, originalHeights);
        }
    }
}
