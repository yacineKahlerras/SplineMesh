using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateMesh03))]
public class SplineMeshEditor : Editor
{
    /*private void OnSceneGUI()
    {
        var splineMesh = target as GenerateMesh03;
        
        Vector3 pos1 = splineMesh.transform.TransformPoint(splineMesh.GetVertexPos(15));
        Vector3 pos2 = splineMesh.transform.TransformPoint(splineMesh.GetVertexPos(16));

        Vector3 centerPos = (pos1 + pos2) / 2;

        var buttonSize = HandleUtility.GetHandleSize(centerPos) * 0.1f;
        Handles.Button(centerPos, Quaternion.identity, buttonSize, buttonSize, Handles.CubeHandleCap);
    }*/
}
