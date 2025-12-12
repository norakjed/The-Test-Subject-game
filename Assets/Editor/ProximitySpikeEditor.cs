#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProximitySpike))]
public class ProximitySpikeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProximitySpike script = (ProximitySpike)target;
        GUILayout.Space(6);
        if (GUILayout.Button("Preview Show Spike"))
        {
            script.PreviewShowSpike();
            EditorUtility.SetDirty(script);
        }

        if (GUILayout.Button("Set Start Hidden = False"))
        {
            script.startHidden = false;
            EditorUtility.SetDirty(script);
        }
    }
}
#endif
