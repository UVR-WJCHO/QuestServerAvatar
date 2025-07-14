using UnityEngine;
using UnityEditor;

public class RemoveBonePrefix : MonoBehaviour
{
    [MenuItem("Tools/Bones/Remove 'mixamorig:' Prefix")]
    static void RemoveMixamoPrefix()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Please select the root GameObject of your character.");
            return;
        }

        Transform root = Selection.activeGameObject.transform;
        int count = 0;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("mixamorig:"))
            {
                Undo.RecordObject(t, "Rename Bone");
                t.name = t.name.Replace("mixamorig:", "");
                count++;
            }
        }

        Debug.Log($"Removed 'mixamorig:' prefix from {count} bones.");
    }
}
