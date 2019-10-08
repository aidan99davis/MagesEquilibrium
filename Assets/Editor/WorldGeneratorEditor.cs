using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(World)), CanEditMultipleObjects]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI() {
        base.DrawDefaultInspector();
        World world = (World)target;
        if (GUILayout.Button("Build Object")) {
            world.Initialise();
        }
    }
}