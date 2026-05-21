using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class PlatformTransformTool : EditorWindow
{
    [SerializeField] private int count = 0;
    [SerializeField] private List<Transform> platforms = new List<Transform>();
    [SerializeField] private List<Transform> targets = new List<Transform>();

    [SerializeField] private bool appendPlatformsOnDrop = true;
    [SerializeField] private bool appendTargetsOnDrop = true;

    [MenuItem("Tools/Platform Transform Tool")]
    private static void Open()
    {
        GetWindow<PlatformTransformTool>("Platform Transform Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Platform Transform Tool", EditorStyles.boldLabel);

        int newCount = EditorGUILayout.IntField("Count", count);
        newCount = Mathf.Max(0, newCount);

        if (newCount != count)
        {
            count = newCount;
            ResizeList(platforms, count);
            ResizeList(targets, count);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);
        appendPlatformsOnDrop = EditorGUILayout.Toggle("Append On Drop", appendPlatformsOnDrop);
        DrawDropArea("Drop Platforms Here", platforms, appendPlatformsOnDrop);
        DrawTransformList(platforms);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
        appendTargetsOnDrop = EditorGUILayout.Toggle("Append On Drop", appendTargetsOnDrop);
        DrawDropArea("Drop Targets Here", targets, appendTargetsOnDrop);
        DrawTransformList(targets);

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply"))
            Apply();
    }

    private void DrawDropArea(string label, List<Transform> list, bool append)
    {
        Rect dropArea = GUILayoutUtility.GetRect(0f, 40f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, label, EditorStyles.helpBox);

        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddDroppedTransforms(list, DragAndDrop.objectReferences, append);
                SyncCountWithLists();
            }

            evt.Use();
        }
    }

    private void SyncCountWithLists()
    {
        count = Mathf.Max(count, Mathf.Max(platforms.Count, targets.Count));
        ResizeList(platforms, count);
        ResizeList(targets, count);
    }

    private static void AddDroppedTransforms(List<Transform> list, Object[] objects, bool append)
    {
        if (!append)
            list.Clear();

        for (int i = 0; i < objects.Length; i++)
        {
            Transform t = GetTransformFromObject(objects[i]);
            if (t != null)
                list.Add(t);
        }
    }

    private static Transform GetTransformFromObject(Object obj)
    {
        if (obj is Transform transform)
            return transform;

        if (obj is GameObject gameObject)
            return gameObject.transform;

        if (obj is Component component)
            return component.transform;

        return null;
    }

    private static void ResizeList(List<Transform> list, int size)
    {
        while (list.Count < size)
            list.Add(null);

        while (list.Count > size)
            list.RemoveAt(list.Count - 1);
    }

    private static void DrawTransformList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i] = (Transform)EditorGUILayout.ObjectField($"Element {i}", list[i], typeof(Transform), true);
        }
    }

    private void Apply()
    {
        for (int i = 0; i < count; i++)
        {
            Transform platform = platforms[i];
            Transform target = targets[i];

            if (platform == null || target == null)
                continue;

            Undo.RecordObject(platform, "Apply Platform Transforms");
            platform.position = target.position;
            platform.rotation = target.rotation;
            platform.localScale = target.lossyScale;
            EditorUtility.SetDirty(platform);
        }
    }
}
#endif