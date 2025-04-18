using UnityEngine;
using UnityEditor;

public class TerrainNeighborSetter : EditorWindow
{
    [MenuItem("Tools/Auto Set Terrain Neighbors")]
    static void SetNeighbors()
    {
        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains.Length == 0)
        {
            Debug.LogWarning("No terrains found.");
            return;
        }

        // Build a map of terrain positions
        var terrainMap = new System.Collections.Generic.Dictionary<Vector2Int, Terrain>();
        foreach (var terrain in terrains)
        {
            Vector3 pos = terrain.transform.position;
            int x = Mathf.RoundToInt(pos.x / terrain.terrainData.size.x);
            int z = Mathf.RoundToInt(pos.z / terrain.terrainData.size.z);
            terrainMap[new Vector2Int(x, z)] = terrain;
        }

        // Assign neighbors
        foreach (var kvp in terrainMap)
        {
            Vector2Int key = kvp.Key;
            Terrain t = kvp.Value;

            terrainMap.TryGetValue(key + new Vector2Int(-1, 0), out Terrain left);
            terrainMap.TryGetValue(key + new Vector2Int(1, 0), out Terrain right);
            terrainMap.TryGetValue(key + new Vector2Int(0, 1), out Terrain top);
            terrainMap.TryGetValue(key + new Vector2Int(0, -1), out Terrain bottom);

            t.SetNeighbors(left, right, top, bottom);
        }

        Debug.Log("Terrain neighbors set.");
    }
}
