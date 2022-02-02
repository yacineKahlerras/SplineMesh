using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(SplineMesh))]
public class SplineMeshEditor : Editor
{
    static Vector3[] vertices, normals;
    MeshFilter mf;
    static SplineMesh splineMesh;
    bool meshLineIndicesHeaderClicked = false,
        controlsHeaderClicked = false,
        graphicsHeaderClicked=false,
        edgeLoopScalesHeaderClicked = false;
    private ReorderableList meshLineIndicesList;
    private ReorderableList edgeLoopScalesList;
    enum TypeOfList { meshLineIndices, edgeLoopScales}

    private void OnEnable()
    {
        // create the lists
        meshLineIndicesList = CreateList("meshLineIndices", "Index", meshLineIndicesList);
        edgeLoopScalesList = CreateList("edgeLoopScales", "Scale", edgeLoopScalesList);
    }

    public override void OnInspectorGUI()
    {
        splineMesh = target as SplineMesh;

        // mesh basic parameters
        splineMesh.spline = (SplineComponent) EditorGUILayout.ObjectField("Spline", splineMesh.spline, typeof(SplineComponent), true);
        //splineMesh.originalMesh = (Mesh) EditorGUILayout.ObjectField("Mesh", splineMesh.originalMesh, typeof(Mesh), true);
        splineMesh.fixedEdgeLoops = Mathf.Clamp(EditorGUILayout.IntField("EdgeLoops", splineMesh.fixedEdgeLoops), 2, int.MaxValue);
        splineMesh.inititalRotation = EditorGUILayout.Vector3Field("Rotation", splineMesh.inititalRotation);
        splineMesh.thresholdAngle = Mathf.Clamp(EditorGUILayout.FloatField("Threshhold Angle", splineMesh.thresholdAngle), 0, 360);
        EditorGUILayout.Space();

        // inspector mesh modifiers buttons
        Controls();
        EditorGUILayout.Space();
        // graphics of UI
        ControlsGraphics();
        EditorGUILayout.Space();

        // update list changes
        meshLineIndicesList = UpdateList("Mesh Line Indices", meshLineIndicesList, TypeOfList.meshLineIndices);
        EditorGUILayout.Space();
        edgeLoopScalesList = UpdateList("Edge Loop Scales", edgeLoopScalesList, TypeOfList.edgeLoopScales);
        EditorGUILayout.Space();

        //GenerateMeshButton();
    }

    void GenerateMeshButton()
    {
        string buttonString = "Generate Mesh";
        if(splineMesh.meshIsGenerated) buttonString = "Back to original mesh";

        // Generate Mesh From Spline
        if (GUILayout.Button(buttonString))
        {
            if (!splineMesh.meshIsGenerated)
            {
                splineMesh.Start();
            }
            else
            {
                splineMesh.BackOriginalMesh();
            }
        }
    }

    private void OnSceneGUI()
    {
        // get data from the shape
        splineMesh = target as SplineMesh;
        splineMesh.TryGetComponent(out MeshFilter mf);
        vertices = mf.sharedMesh.vertices;
        normals = mf.sharedMesh.normals;

        MeshLineIndicesAdder();
    }

    // buttons control how UI works
    void Controls()
    {
        controlsHeaderClicked = EditorGUILayout.BeginFoldoutHeaderGroup(controlsHeaderClicked, "Controls");
        if (controlsHeaderClicked)
        {
            if (Selection.activeTransform)
            {
                // buttons
                GUILayout.BeginHorizontal();
                splineMesh.drawNormals = GUILayout.Toggle(splineMesh.drawNormals, "Normals", "button");
                splineMesh.drawVertexButtons = GUILayout.Toggle(splineMesh.drawVertexButtons, "Vertices Buttons", "button");
                splineMesh.drawGizmoLines = GUILayout.Toggle(splineMesh.drawGizmoLines, "Shape Lines", "button");// enum for selection
                GUILayout.EndHorizontal();

                // select vertices by
                splineMesh.selectVerticesBy = (SplineMesh.TypesOfSelectingVertices)EditorGUILayout.EnumPopup("Select Vertices By", splineMesh.selectVerticesBy);
            }
            else controlsHeaderClicked = false;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // graphics of UI
    void ControlsGraphics()
    {
        graphicsHeaderClicked = EditorGUILayout.BeginFoldoutHeaderGroup(graphicsHeaderClicked, "Graphics");
        if (graphicsHeaderClicked)
        {
            if (Selection.activeTransform)
            {
                splineMesh.vertexButtonColor = EditorGUILayout.ColorField("Vertices", splineMesh.vertexButtonColor);
                splineMesh.selectedVertexButtonColor = EditorGUILayout.ColorField("Vertices Hover", splineMesh.selectedVertexButtonColor);
                splineMesh.gizmosVertexLinesColor = EditorGUILayout.ColorField("Shape Lines", splineMesh.gizmosVertexLinesColor);
                splineMesh.normalsColor = EditorGUILayout.ColorField("Normals", splineMesh.normalsColor);
                splineMesh.normalsGizmosLinesLength = EditorGUILayout.FloatField("Normals line length", splineMesh.normalsGizmosLinesLength);
            }
            else graphicsHeaderClicked = false;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // create the list
    ReorderableList CreateList(string varName, string elementsName, ReorderableList reorderableList)
    {
        // list structure configuration
        reorderableList = new ReorderableList(serializedObject,
                serializedObject.FindProperty(varName),
                false, false, false, true);

        // elements configuration
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width,
                EditorGUIUtility.singleLineHeight), element, new GUIContent(elementsName));
        };

        return reorderableList;
    }

    // update list changes
    ReorderableList UpdateList(string listLabel, ReorderableList reorderableList, TypeOfList t)
    {
        // mesh line indices list
        EditorGUILayout.BeginHorizontal();

        // if we clicked on a list header
        bool listHeaderClicked = false;
        if (t == 0) listHeaderClicked = meshLineIndicesHeaderClicked; else listHeaderClicked = edgeLoopScalesHeaderClicked;
        listHeaderClicked = EditorGUILayout.BeginFoldoutHeaderGroup(listHeaderClicked, listLabel);

        // determining type of the element
        int customListIntSize = EditorGUILayout.DelayedIntField("", reorderableList.serializedProperty.arraySize, GUILayout.Width(50));

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndFoldoutHeaderGroup();

        // if we clicked on the list header then draw the list
        if (listHeaderClicked)
        {
            if (Selection.activeTransform) { EditorGUILayout.Space(); reorderableList.DoLayoutList(); }
            else listHeaderClicked = false; 
        }

        // if we changed list size then resize list
        if (customListIntSize != reorderableList.serializedProperty.arraySize)
        {
            while (customListIntSize < reorderableList.serializedProperty.arraySize)
            {
                var i = reorderableList.serializedProperty.arraySize - 1;
                var list = reorderableList.serializedProperty;
                list.DeleteArrayElementAtIndex(i);
            }
        }

        // update the variables
        if (t == 0) meshLineIndicesHeaderClicked = listHeaderClicked; else edgeLoopScalesHeaderClicked = listHeaderClicked;
        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();

        return reorderableList;
    }

    // verifies the lines
    public void MeshLineIndicesAdder()
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
    static void DrawGizmosSelected(SplineMesh splineMesh, GizmoType gizmoType)
    {
        // drawing normals on each vertex of the mesh
        if(vertices!=null && splineMesh.drawNormals)
        for (int i=0; i< vertices.Length;i++)
        {
            Vector3 from, to;
            to = splineMesh.transform.TransformPoint(vertices[i] + normals[i] * splineMesh.normalsGizmosLinesLength);
            from = splineMesh.transform.TransformPoint(vertices[i]);
            Gizmos.color = splineMesh.normalsColor;
            Gizmos.DrawLine(from, to);
        }

        // draw gizmos lines from the list of lines of the mesh
        if (vertices != null && splineMesh.drawGizmoLines)
        {
            Gizmos.color = splineMesh.gizmosVertexLinesColor;
            if (splineMesh.meshLineIndices.Count > 0) DrawMeshLines(splineMesh.meshLineIndices, vertices, splineMesh);
        }
            
    }

    // draw gizmos lines from the list of lines of the mesh
    static void DrawMeshLines(List<int> meshLinesIndices, Vector3[] vertices, SplineMesh splineMesh)
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

                Gizmos.color = splineMesh.gizmosVertexLinesColor;
                Gizmos.DrawLine(from, to);
            }
        }
    }

    // custom GUI button for Angle Points
    static bool VertexButton(SplineMesh splineMesh, Vector3 position, int? indice = null)
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
                    splineMesh.selectVerticesBy == SplineMesh.TypesOfSelectingVertices.Dragging && !altPressed)
                {
                    GUIUtility.hotControl = controlID;
                    buttonOutput = true;
                    Event.current.Use();
                }
                break;

            // when dragging the handle
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == controlID && Event.current.button == 0 &&
                    splineMesh.selectVerticesBy == SplineMesh.TypesOfSelectingVertices.Clicking && !altPressed)
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
                    Handles.color = splineMesh.selectedVertexButtonColor;
                    Handles.SphereHandleCap(controlID, position, Quaternion.identity, size, EventType.Repaint);
                }
                else
                {
                    Handles.color = splineMesh.vertexButtonColor;
                    Handles.SphereHandleCap(controlID, position, Quaternion.identity, size, EventType.Repaint);
                }
                break;
        }

        return buttonOutput;
    }
}
