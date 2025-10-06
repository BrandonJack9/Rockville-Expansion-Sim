using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MultiMoundTerrainController : MonoBehaviour
{
    [System.Serializable]
    public class MoundArea
    {
        public Vector3 worldCenter = Vector3.zero;
        public float radius = 10f;
        [Range(0f, 1f)]
        public float targetHeight = 0.6f;
        [Range(0f, 1f)]
        public float plateauFraction = 0.6f;
        [Range(0.01f, 1f)]
        public float edgeFeather = 0.3f;
    }

    [Header("Terrain")]
    public Terrain terrain;
    public float changeSpeed = 1.5f;

    [Header("Control")]
    public Slider globalSlider;
    [Range(0f, 1f)]
    public float initialSliderValue = 0f;

    [Header("Mounds")]
    public List<MoundArea> moundAreas = new List<MoundArea>();

    // Internals
    private TerrainData terrainData;
    private int heightmapWidth, heightmapHeight;
    private float[,] originalHeights;
    private float sliderValue;
    private float hxPerWorldX, hzPerWorldZ;

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

        // Store original heights
        originalHeights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        hxPerWorldX = (heightmapWidth - 1) / terrainData.size.x;
        hzPerWorldZ = (heightmapHeight - 1) / terrainData.size.z;

        if (globalSlider != null)
        {
            sliderValue = Mathf.Clamp01(globalSlider.value);
            globalSlider.onValueChanged.AddListener(val => sliderValue = Mathf.Clamp01(val));
        }
        else
        {
            sliderValue = initialSliderValue;
        }
    }

    void Update()
    {
        float[,] current = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);
        float[,] targetHeights = (float[,])originalHeights.Clone(); // Start from original each frame

        foreach (var mound in moundAreas)
        {
            ApplyMoundToTarget(targetHeights, mound);
        }

        float step = changeSpeed * Time.deltaTime;

        for (int z = 0; z < heightmapHeight; z++)
        {
            for (int x = 0; x < heightmapWidth; x++)
            {
                current[z, x] = Mathf.MoveTowards(current[z, x], targetHeights[z, x], step);
            }
        }

        terrainData.SetHeights(0, 0, current);
    }

    void OnDisable()
    {
        if (terrainData != null && originalHeights != null)
        {
            terrainData.SetHeights(0, 0, originalHeights);
        }
    }

    void ApplyMoundToTarget(float[,] targetHeights, MoundArea mound)
    {
        Vector3 terrainPos = terrain.transform.position;

        int centerX = Mathf.RoundToInt((mound.worldCenter.x - terrainPos.x) * hxPerWorldX);
        int centerZ = Mathf.RoundToInt((mound.worldCenter.z - terrainPos.z) * hzPerWorldZ);

        float rX = mound.radius * hxPerWorldX;
        float rZ = mound.radius * hzPerWorldZ;
        float radius = (rX + rZ) * 0.5f;

        int startX = Mathf.Clamp(Mathf.FloorToInt(centerX - radius), 0, heightmapWidth - 1);
        int endX = Mathf.Clamp(Mathf.CeilToInt(centerX + radius), 0, heightmapWidth - 1);
        int startZ = Mathf.Clamp(Mathf.FloorToInt(centerZ - radius), 0, heightmapHeight - 1);
        int endZ = Mathf.Clamp(Mathf.CeilToInt(centerZ + radius), 0, heightmapHeight - 1);

        float plateau = Mathf.Clamp01(mound.plateauFraction);
        float feather = Mathf.Clamp(mound.edgeFeather, 0.01f, 1f);
        float featherStart = plateau;
        float featherEnd = Mathf.Min(1f, plateau + feather);
        float invFeatherRange = 1f / Mathf.Max(1e-6f, featherEnd - featherStart);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int x = startX; x <= endX; x++)
            {
                float dx = x - centerX;
                float dz = z - centerZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float nd = dist / radius;

                if (nd > 1f) continue;

                float mask = 0f;
                if (nd <= featherStart)
                {
                    mask = 1f;
                }
                else
                {
                    float t = Mathf.Clamp01((nd - featherStart) * invFeatherRange);
                    mask = 1f - (t * t * (3f - 2f * t));
                }

                float blend = sliderValue * mask;
                float originalHeight = originalHeights[z, x];
                float moundHeight = mound.targetHeight;

                float finalHeight = Mathf.Lerp(originalHeight, moundHeight, blend);

                // If multiple mounds overlap, choose the highest blended value
                targetHeights[z, x] = Mathf.Max(targetHeights[z, x], finalHeight);
            }
        }
    }
}
