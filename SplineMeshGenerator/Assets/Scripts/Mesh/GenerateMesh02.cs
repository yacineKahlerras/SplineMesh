using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GenerateMesh02 : MonoBehaviour {
    /* scratchpad */
    private MeshFilter mf;
    public SplineComponent spline;
    List<Vector3> pointList;
    Vertex[] verts;

    public float fixedEdgeLoops = 3f;
    [SerializeField] Vector3[] positions;
    [SerializeField] Vector3[] normals;
    [SerializeField] float[] uCoords;
    [SerializeField] int[] lines;

    void Start () {
        mf = GetComponent<MeshFilter> ();
        pointList = new List<Vector3>();

        // generate mesh from the start
        GenerateMesh ();
    }
 
    // creates a mesh based on points and initiale shape
    private void GenerateMesh() {
        var mesh = GetMesh ();
        var shape = GetExtrudeShape ();
        var path = GetPath ();
 
        Extrude (mesh, shape, path);
    }

    // gets the vertices, normals and uvs of the initiale shape
    private ExtrudeShape GetExtrudeShape() {
        verts = new Vertex[positions.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].point = positions[i];
            verts[i].normal = normals[i];
            verts[i].uCoord = uCoords[i];
        }
 
        return new ExtrudeShape (verts, lines);
    }
 
    // get every single point from the spline and its orientation
    private OrientedPoint[] GetPath() {
 
        var path = new List<OrientedPoint> ();
 
        for (float t = 0; t <= 1; t += 1f/(fixedEdgeLoops-1)) {
            var point = spline.transform.InverseTransformPoint(spline.Hermite(t));
            var rotation = spline.GetOrientation3D(t, Vector3.up);
            path.Add (new OrientedPoint (point, rotation));
        }
 
        return path.ToArray ();
    }
 
    // get sharedMesh or create one
    private Mesh GetMesh() {
        if (mf.sharedMesh == null) {
            mf.sharedMesh = new Mesh ();
        }
        return mf.sharedMesh;
    }
 
    private void Extrude(Mesh mesh, ExtrudeShape shape, OrientedPoint[] path) {

        int vertsInShape = shape.verts.Length;
        int segments = path.Length - 1;
        int edgeLoops = path.Length;
        int vertCount = vertsInShape * edgeLoops;
        int triCount = shape.lines.Length * segments;
        int triIndexCount = triCount * 3;
 
        var triangleIndices = new int[triIndexCount*2];
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        // setting up Vertices , normals , uvs
        ShapeData shapeData = VertsCalculator(shape, path, vertsInShape, vertices, normals, uvs);
        vertices = shapeData.vertecies;
        normals = shapeData.normals;
        uvs = shapeData.UVs;

        // setting up Triangles
        triangleIndices = TrianglesSetter(triangleIndices, vertsInShape, segments, shape);

        mesh.Clear ();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangleIndices;
    }

    // setting up Triangles
    public int[] TrianglesSetter(int[] triangleIndices, int vertsInShape, int segments, ExtrudeShape shape)
    {
        int ti = 0;

        // foreach edge loop
        for (int segment = 0; segment < segments; segment++)
        {
            // to navigate different edge loops
            int offset = segment * vertsInShape;

            // foreach line in the shape
            for (int LineIndice = 0; LineIndice < shape.lines.Length; LineIndice += 2)
            {
                // points for triangles
                int currentA = offset + shape.lines[LineIndice];
                int currentB = offset + shape.lines[LineIndice + 1];
                int nextA = offset + shape.lines[LineIndice] + vertsInShape;
                int nextB = offset + shape.lines[LineIndice + 1] + vertsInShape;

                // triangles on upper side
                triangleIndices[ti] = currentB; ti++;
                triangleIndices[ti] = currentA; ti++;
                triangleIndices[ti] = nextA; ti++;

                triangleIndices[ti] = nextA; ti++;
                triangleIndices[ti] = nextB; ti++;
                triangleIndices[ti] = currentB; ti++;

                // reverse triangles on downside
                triangleIndices[ti] = nextA; ti++;
                triangleIndices[ti] = currentA; ti++;
                triangleIndices[ti] = currentB; ti++;

                triangleIndices[ti] = currentB; ti++;
                triangleIndices[ti] = nextB; ti++;
                triangleIndices[ti] = nextA; ti++;
            }
        }

        return triangleIndices;
    }

    // vertecies , normals, UVs, distance covered setter
    public ShapeData VertsCalculator(ExtrudeShape shape, OrientedPoint[] path, int vertsInShape, Vector3[] vertices, Vector3[] normals, Vector2[] uvs)
    {
        // culculate total distance of the spline
        float totalLength = 0;
        float distanceCovered = 0;
        for (int i = 0; i < path.Length - 1; i++)
        {
            var d = Vector3.Distance(path[i].position, path[i + 1].position);
            totalLength += d;
        }

        // foreach edgeLoop in the whole created mesh
        for (int i = 0; i < path.Length; i++)
        {
            // distance covered for the v coordinates
            int offset = i * vertsInShape;
            if (i > 0)
            {
                var d = Vector3.Distance(path[i].position, path[i - 1].position);
                distanceCovered += d;
            }
            float v = distanceCovered / totalLength;

            // get world points of vertices and assign them
            for (int j = 0; j < vertsInShape; j++)
            {
                int id = offset + j;
                vertices[id] = path[i].LocalToWorld(shape.verts[j].point);
                normals[id] = path[i].LocalToWorldDirection(shape.verts[j].normal);
                uvs[id] = new Vector2(shape.verts[j].uCoord, v);
            }
        }

        return new ShapeData(vertices, normals, uvs);
    }

    // shape data
    public struct ExtrudeShape {
        public Vertex[] verts;
        public int[] lines;
 
        public ExtrudeShape(Vertex[] verts, int[] lines) {
            this.verts = verts;
            this.lines = lines;
        }
    }
 
    // vertex data
    public struct Vertex {
        public Vector3 point;
        public Vector3 normal;
        public float uCoord;
 
 
        public Vertex(Vector3 point, Vector3 normal, float uCoord) {
            this.point = point;
            this.normal = normal;
            this.uCoord = uCoord;
        }
    }
 
    // point rotation and local position
    public struct OrientedPoint {
        public Vector3 position;
        public Quaternion rotation;
 
 
        public OrientedPoint(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
        }
 
        // get world point from local point
        public Vector3 LocalToWorld(Vector3 point) {
            return position + rotation * point;
        }
 
        // get local point from world point
        public Vector3 WorldToLocal(Vector3 point) {
            return Quaternion.Inverse (rotation) * (point - position);
        }
 
        // get local direction from local point
        public Vector3 LocalToWorldDirection(Vector3 dir) {
            return rotation * dir;
        }
    }

    // shape data contain vertecies, normals, UVs
    public struct ShapeData
    {
        public Vector3[] vertecies;
        public Vector3[] normals;
        public Vector2[] UVs;

        public ShapeData(Vector3[] vertecies, Vector3[] normals, Vector2[] UVs)
        {
            this.vertecies = vertecies;
            this.normals = normals;
            this.UVs = UVs;
        }
    }
}
 