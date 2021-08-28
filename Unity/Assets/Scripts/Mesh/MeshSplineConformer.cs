using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSplineConformer : MonoBehaviour {

    [SerializeField] private SplineComponent spline;
    [SerializeField] private float meshWidth = 1.5f;
    [SerializeField] private Vector3 normal = new Vector3(0,-1,0);

    private Mesh mesh;
    private MeshFilter meshFilter;
    List<Vector3> pointList;

    private void Awake() {
        //if (spline == null) spline = GetComponent<SplineDone>();
        meshFilter = GetComponent<MeshFilter>();
        transform.position = Vector3.zero;

        pointList = new List<Vector3>();

        for (float t = 0; t < 1; t+=0.001f)
        {
            pointList.Add(spline.Hermite(t));
        }
    }

    private void Start() {
        transform.position = spline.transform.position;

        UpdateMesh();

        //spline.OnDirty += Spline_OnDirty;
    }

    private void Spline_OnDirty(object sender, EventArgs e) {
        UpdateMesh();
    }

    private void UpdateMesh() {
        if (mesh != null) {
            mesh.Clear();
            Destroy(mesh);
            mesh = null;
        }

        if (pointList.Count > 2)
        {
            Vector3 point = pointList[0];
            Vector3 secondPoint = pointList[1];
            mesh = MeshUtils.CreateLineMesh(point - transform.position, secondPoint - transform.position, normal, meshWidth);

            for (int i = 2; i < pointList.Count; i++)
            {
                Vector3 thisPoint = pointList[i];
                MeshUtils.AddLinePoint(mesh, thisPoint - transform.position, spline.GetForward(i*0.001f), normal, meshWidth);
            }

            meshFilter.mesh = mesh;
        }
    }
}
