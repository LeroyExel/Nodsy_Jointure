using UnityEngine;
using UnityEditor;

public class ReplaceObjectsWindow : EditorWindow
{
    private GameObject replacementPrefab;
    private bool keepOriginalName = true;
    private bool keepPrefabScale = false;

    [MenuItem("Tools/Replace Objects")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceObjectsWindow>("Replace Objects");
    }

    private void OnGUI()
    {
        replacementPrefab = (GameObject)EditorGUILayout.ObjectField("Replacement Prefab", replacementPrefab, typeof(GameObject), false);
        keepOriginalName = EditorGUILayout.Toggle("Keep Original Name", keepOriginalName);
        keepPrefabScale = EditorGUILayout.Toggle("Keep Prefab Scale", keepPrefabScale);

        if (GUILayout.Button("Replace Selected"))
        {
            ReplaceSelected();
        }
    }

    private void ReplaceSelected()
    {
        if (replacementPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a replacement prefab.", "OK");
            return;
        }

        Transform[] selection = Selection.transforms;
        if (selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No GameObjects selected.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Objects");
        int groupIndex = Undo.GetCurrentGroup();

        GameObject[] newObjects = new GameObject[selection.Length];
        Vector3 prefabLocalScale = replacementPrefab.transform.localScale;

        for (int i = 0; i < selection.Length; i++)
        {
            Transform target = selection[i];
            Transform parent = target.parent;
            int siblingIndex = target.GetSiblingIndex();

            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(replacementPrefab);
            Undo.RegisterCreatedObjectUndo(newObj, "Instantiate Prefab");

            if (keepOriginalName)
            {
                newObj.name = target.name;
            }

            Transform newTransform = newObj.transform;
            newTransform.SetParent(parent);
            newTransform.localPosition = target.localPosition;
            newTransform.localRotation = target.localRotation;
            newTransform.localScale = keepPrefabScale ? prefabLocalScale : target.localScale;
            newTransform.SetSiblingIndex(siblingIndex);

            newObjects[i] = newObj;
            Undo.DestroyObjectImmediate(target.gameObject);
        }

        Selection.objects = newObjects;
        Undo.CollapseUndoOperations(groupIndex);
    }
}