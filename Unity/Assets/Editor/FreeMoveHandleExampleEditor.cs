using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FreeMoveHandleExample)), CanEditMultipleObjects]
public class FreeMoveHandleExampleEditor : Editor
{
    protected virtual void OnSceneGUI()
    {
        FreeMoveHandleExample example = (FreeMoveHandleExample)target;

        float size = HandleUtility.GetHandleSize(example.targetPosition) * 0.5f;
        Vector3 snap = Vector3.one * 0.5f;

        EditorGUI.BeginChangeCheck();
        var idNext = GUIUtility.GetControlID(FocusType.Passive);
        Vector3 newTargetPosition = Handles.FreeMoveHandle(example.targetPosition, Quaternion.identity, size, snap, Handles.CubeHandleCap);

        if (GUIUtility.hotControl == idNext/*&& Event.current.type == EventType.MouseDrag && Event.current.button == 0*/)
        {
            Debug.Log("i : " + newTargetPosition + " || ID : " + idNext);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(example, "Change Look At Target Position");
            example.targetPosition = newTargetPosition;
            example.Update();
        }
    }
}