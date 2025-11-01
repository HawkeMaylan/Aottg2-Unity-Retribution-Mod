using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainGroupSync : EditorWindow
{
    Terrain sourceTerrain;

    [MenuItem("Tools/Terrain Group Sync")]
    public static void ShowWindow()
    {
        GetWindow<TerrainGroupSync>("Sync Terrain Group");
    }

    void OnGUI()
    {
        sourceTerrain = (Terrain)EditorGUILayout.ObjectField("Source Terrain", sourceTerrain, typeof(Terrain), true);

        if (sourceTerrain == null)
        {
            EditorGUILayout.HelpBox("Assign a source terrain to copy data from.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Sync to Group"))
        {
            SyncTerrainsByGroupID(sourceTerrain);
        }
    }

    void SyncTerrainsByGroupID(Terrain source)
    {
        int groupID = source.groupingID;
        Terrain[] allTerrains = Terrain.activeTerrains;
        List<Terrain> targets = new List<Terrain>();

        foreach (Terrain t in allTerrains)
        {
            if (t != source && t.groupingID == groupID)
                targets.Add(t);
        }

        foreach (Terrain target in targets)
        {
            Undo.RegisterCompleteObjectUndo(target.terrainData, "Sync Terrain Group");

            // Copy terrain layers (textures)
            target.terrainData.terrainLayers = source.terrainData.terrainLayers;

            // Copy texture paint (alphamaps)
            target.terrainData.alphamapResolution = source.terrainData.alphamapResolution;
            target.terrainData.SetAlphamaps(0, 0,
                source.terrainData.GetAlphamaps(0, 0,
                source.terrainData.alphamapWidth, source.terrainData.alphamapHeight));

            // Copy detail prototypes and data
            target.terrainData.detailPrototypes = source.terrainData.detailPrototypes;
            for (int i = 0; i < source.terrainData.detailPrototypes.Length; i++)
            {
                target.terrainData.SetDetailLayer(0, 0, i,
                    source.terrainData.GetDetailLayer(0, 0,
                    source.terrainData.detailWidth, source.terrainData.detailHeight, i));
            }

            // Copy tree prototypes and instances
            target.terrainData.treePrototypes = source.terrainData.treePrototypes;
            target.terrainData.treeInstances = source.terrainData.treeInstances;
        }

        Debug.Log($"Synced {targets.Count} terrain(s) in group {groupID}.");
    }
}
