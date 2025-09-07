using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(RuleExtractor))]
public class RuleExtractorEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (GUILayout.Button("Extract Rules")) {
            (target as RuleExtractor).ExtractRules();
            EditorUtility.SetDirty((target as RuleExtractor).rulesFile);
        }
    }
}
