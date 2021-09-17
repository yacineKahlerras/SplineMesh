using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateMesh03))]
public class SplineMeshEditor : Editor
{
    private void OnSceneGUI()
    {
        var splineMesh = target as GenerateMesh03;
        
        
    }
}
