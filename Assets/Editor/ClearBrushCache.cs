using UnityEditor;
using UnityEngine;

public class ClearTerrainBrushCache
{
    [MenuItem("Tools/Terrain/Clear Brush Cache")]
    public static void ClearBrushCache()
    {
        Debug.Log(" Clearing terrain brush cache...");

        // Flush unused assets from memory
        Resources.UnloadUnusedAssets();

        // Force garbage collection (cleans RenderTextures, temp data)
        System.GC.Collect();

        Debug.Log(" Terrain brush cache cleared.");
    }
}
