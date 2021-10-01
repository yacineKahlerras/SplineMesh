using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateMesh03))]
public class SplineMeshEditor : Editor
{
    static Vector3[] vertices, normals;
    MeshFilter mf;
    static GenerateMesh03 splineMesh;
    int currentIndex, previousIndex;

    public override void OnInspectorGUI()
    {
        //base.DrawDefaultInspector();
        splineMesh = target as GenerateMesh03;

        splineMesh.spline = (SplineComponent) EditorGUILayout.ObjectField("Spline", splineMesh.spline, typeof(GenerateMesh03), false);
        splineMesh.fixedEdgeLoops = Mathf.Clamp(EditorGUILayout.IntField("EdgeLoops", splineMesh.fixedEdgeLoops), 2, int.MaxValue);
        splineMesh.inititalRotation = EditorGUILayout.Vector3Field("Rotation", splineMesh.inititalRotation);
        splineMesh.thresholdAngle = Mathf.Clamp(EditorGUILayout.FloatField("Threshhold Angle", splineMesh.thresholdAngle), 0, 360);
        splineMesh.normalsGizmosLinesLength = EditorGUILayout.FloatField("Normals line length", splineMesh.normalsGizmosLinesLength);

        EditorGUILayout.BeginHorizontal();
        splineMesh.drawNormals = GUILayout.Toggle(splineMesh.drawNormals, "Normals", "button");
        splineMesh.drawVertexButtons = GUILayout.Toggle(splineMesh.drawVertexButtons, "Vertices Buttons", "button");
        splineMesh.drawGizmoLines = GUILayout.Toggle(splineMesh.drawGizmoLines, "Shape Lines", "button");
        EditorGUILayout.EndHorizontal();

        splineMesh.selectVerticesBy = (GenerateMesh03.TypesOfSelectingVertices) EditorGUILayout.EnumPopup("Select Vertices By", splineMesh.selectVerticesBy);
    }

    private void OnSceneGUI()
    {
        // get data from the shape
        splineMesh = target as GenerateMesh03;
        splineMesh.TryGetComponent(out MeshFilter mf);
        vertices = mf.sharedMesh.vertices;
        normals = mf.sharedMesh.normals;

        LineMaker();
    }

    // verifies the lines
    public void LineMaker()
    {
        // create a custom button for each vertex and if click add it to list
        if (vertices != null && splineMesh.drawVertexButtons)
        {
            //for each vertex
            for (int vertIndex = 0; vertIndex < vertices.Length; vertIndex++)
            {
                // if we clicked on a vertex button
                if (VertexButton(splineMesh, vertices[vertIndex], vertIndex))
                {
                    // if we have atleast a point
                    if (splineMesh.meshLineIndices.Count > 0)
                    {
                        bool vertAddedBefore = false;
                        for (int addedIndex = 0; addedIndex < splineMesh.meshLineIndices.Count; addedIndex++)
                        {
                            if (splineMesh.meshLineIndices[addedIndex] == vertIndex)
                            {
                                var lineIndicesCount = splineMesh.meshLineIndices.Count;
                                var beforeIndex = addedIndex > 0 ? splineMesh.meshLineIndices[addedIndex - 1] : -99;
                                var afterIndex = addedIndex < (lineIndicesCount-1) ? splineMesh.meshLineIndices[addedIndex + 1] : -99;
                                var lastAddedIndex = splineMesh.meshLineIndices[lineIndicesCount - 1];

                                if (lastAddedIndex != beforeIndex && lastAddedIndex != afterIndex && lastAddedIndex!= vertIndex)
                                {
                                    splineMesh.addMeshLineIndices(vertIndex);
                                }
                                vertAddedBefore = true;
                            }
                        }
                        if(!vertAddedBefore)
                        {
                            splineMesh.addMeshLineIndices(vertIndex);
                        }
                    }
                    else
                    {
                        splineMesh.addMeshLineIndices(vertIndex);
                    }
                }
            }
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmosSelected(GenerateMesh03 splineMesh, GizmoType gizmoType)
    {
        // drawing normals on each vertex of the mesh
        if(vertices!=null && splineMesh.drawNormals)
        for (int i=0; i< vertices.Length;i++)
        {
            Vector3 from, to;
            to = splineMesh.transform.TransformPoint(vertices[i] + normals[i] * splineMesh.normalsGizmosLinesLength);
            from = splineMesh.transform.TransformPoint(vertices[i]);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, to);
        }

        // draw gizmos lines from the list of lines of the mesh
        if (vertices != null && splineMesh.drawGizmoLines)
        {
            Gizmos.color = Color.green;
            DrawMeshLines(splineMesh.meshLineIndices, vertices, splineMesh);
        }
            
    }

    // draw gizmos lines from the list of lines of the mesh
    static void DrawMeshLines(List<int> meshLinesIndices, Vector3[] vertices, GenerateMesh03 splineMesh)
    {
        Vector3 from, to;
        int pointA, pointB;
        for (int i = 0; i < meshLinesIndices.Count; i++)
        {
            if(i < meshLinesIndices.Count - 1)
            {
                pointA = meshLinesIndices[i];
                pointB = meshLinesIndices[i + 1];

                from = splineMesh.transform.TransformPoint(vertices[pointA]);
                to = splineMesh.transform.TransformPoint(vertices[pointB]);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(from, to);
            }
        }
    }

    // custom GUI button for Angle Points
    static bool VertexButton(GenerateMesh03 splineMesh, Vector3 position, int? indice = null)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive); // Gets a new ControlID for the handle
        position = splineMesh.transform.TransformPoint(position);
        var size = HandleUtility.GetHandleSize(position) * 0.1f;
        Vector3 screenPosition = Handles.matrix.MultiplyPoint(position); // getting screen position of the handle
        bool buttonOutput = false; // returns true when pressed
        float anglePointCircleRangeSize = size; // is normal when not pressed on, gets bigger when pressed on
        var altPressed = Event.current.alt; // if we clicked on alt

        switch (Event.current.GetTypeForControl(controlID))
        {
            // initialization of setting of the closeness Range
            case EventType.Layout:
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(screenPosition, anglePointCircleRangeSize));
                break;

            // when dragging the handle
            case EventType.MouseDrag:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0 &&
                    splineMesh.selectVerticesBy == GenerateMesh03.TypesOfSelectingVertices.Dragging && !altPressed)
                {
                    GUIUtility.hotControl = controlID;
                    buttonOutput = true;
                    Event.current.Use();
                }
                break;

            // when dragging the handle
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0 &&
                    splineMesh.selectVerticesBy == GenerateMesh03.TypesOfSelectingVertices.Clicking && !altPressed)
                {
                    GUIUtility.hotControl = controlID;
                    buttonOutput = true;
                    Event.current.Use();
                }
                break;

            // how the handle looks
            case EventType.Repaint:
                // This is the part that switches according to mouse over, and the rectangles act as the visual element of the button
                if (HandleUtility.nearestControl == controlID && !altPressed) 
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
