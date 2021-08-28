using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshCreator : MonoBehaviour
{
    Mesh mesh;
    public Vector3[] verts;
    public int[] triangles;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void CreateShape()
    {
        verts = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(0,1,0),
            new Vector3(1,1,0),
            new Vector3(0,0,1),
            new Vector3(1,0,1),
        };

        triangles = new int[]
        {
            2,1,0,
            2,3,1,
            4,1,0
        };
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void Update()
    {
        UpdateMesh();
    }
}
