
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NpcHumanSetup))]
public class NpcHumanSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NpcHumanSetup npc = (NpcHumanSetup)target;
        if (GUILayout.Button("Randomize Appearance"))
        {
            npc.RandomizeAndSetup();
            EditorUtility.SetDirty(npc);
        }
    }
}
