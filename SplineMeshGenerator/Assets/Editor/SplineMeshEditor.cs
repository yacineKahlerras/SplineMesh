using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateMesh03))]
public class SplineMeshEditor : Editor
{
    static Vector3[] vertices, normals;
    MeshFilter mf;
    static Vector3 from, to;

    private void OnSceneGUI()
    {
        var splineMesh = target as GenerateMesh03;
        splineMesh.TryGetComponent(out MeshFilter mf);
        vertices = mf.sharedMesh.vertices;
        normals = mf.sharedMesh.normals;

        if (vertices != null)
            for(int i = 0; i < vertices.Length; i++)
                if (VertexButton(splineMesh, vertices[i])) splineMesh.addToRandomList(i);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmosSelected(GenerateMesh03 splineMesh, GizmoType gizmoType)
    {
        if(vertices!=null)
        for (int i=0; i< vertices.Length;i++)
        {
            to = splineMesh.transform.TransformPoint(vertices[i] + normals[i] * splineMesh.normalsGizmosLinesLength);
            from = splineMesh.transform.TransformPoint(vertices[i]);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, to);
        }
    }

    // custom GUI button for Angle Points
    static bool VertexButton(GenerateMesh03 splineMesh, Vector3 position)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive); // Gets a new ControlID for the handle
        position = splineMesh.transform.TransformPoint(position);
        var size = HandleUtility.GetHandleSize(position) * 0.1f;
        Vector3 screenPosition = Handles.matrix.MultiplyPoint(position); // getting screen position of the handle
        bool buttonOutput = false; // returns true when pressed
        float anglePointCircleRangeSize = size; // is normal when not pressed on, gets bigger when pressed on
        switch (Event.current.GetTypeForControl(controlID))
        {
            // initialization of setting of the closeness Range
            case EventType.Layout:
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(screenPosition, anglePointCircleRangeSize));
                break;

            // when dragging the handle
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                {
                    GUIUtility.hotControl = controlID;
                    buttonOutput = true;
                    Event.current.Use();
                }
                break;

            // how the handle looks
            case EventType.Repaint:
                // This is the part that switches according to mouse over, and the rectangles act as the visual element of the button
                if (HandleUtility.nearestControl == controlID) 
                {
                    Handles.color = Color.red;
                    Handles.SphereHandleCap(controlID, position, Quaternion.identity, size, EventType.Repaint);
                }
                else
                {
                    Handles.color = Color.yellow;
                    Handles.SphereHandleCap(controlID, position, Quaternion.identity, size, EventType.Repaint);
                }
                break;
        }

        return buttonOutput;
    }
}
