using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TB;

[RequireComponent(typeof(MeshFilter))]
public class GenerateMesh03 : MonoBehaviour {
    /* scratchpad */
    [HideInInspector] public MeshFilter mf;
    public SplineComponent spline;
    List<Vector3> pointList;
    Vertex[] verts;
    
    [Range(2, Mathf.Infinity)] public int fixedEdgeLoops = 2;
    public Vector3 inititalRotation = new Vector3(0, 0, 0);
    Vector3[] positions;
    Vector3[] normals, initialNormals;
    float[] uCoords;
    int[] lines;
    int[] initialTriangles;
    public float thresholdAngle;
    float thresholdAngleINIT;
    public float normalsGizmosLinesLength;

    public List<int> meshLineIndices; // the indices that make the lines of the shape
    public bool drawNormals = true, drawVertexButtons = true, drawGizmoLines = true;
    public enum TypesOfSelectingVertices { Dragging, Clicking } // type of selection of vertices
    public TypesOfSelectingVertices selectVerticesBy; // type of selection of vertices
    List<Vector3> edgeLoopCenters = new List<Vector3>(); // centers of each edgeLoop
    Vector3 centerOfMesh; // center of the mesh
    
    public List<float> edgeLoopScales = new List<float>();
    List<float> tempEdgeLoopScales = new List<float>();
    Dictionary<int, List<int>> temporaryEdgeLoopIndices = new Dictionary<int, List<int>>(); // gets verts for each edgeLoop
    public Dictionary<int, List<int>> mainEdgeLoopIndices = new Dictionary<int, List<int>>();
    [HideInInspector] public bool hasUnweldedVerts = false;

    // adds to the list of line indices
    public void addMeshLineIndices(int i) => meshLineIndices.Add(i);

    void Start () {
        mf = GetComponent<MeshFilter> ();
        pointList = new List<Vector3>();

        // gets a 2D mesh's info : vertices, normals, lines 
        GetMeshInfo(mf);
        thresholdAngleINIT = thresholdAngle;

        // generate mesh from the start
        GenerateMesh ();
        hasUnweldedVerts = true;
    }

    private void Update()
    {
        if (thresholdAngleINIT != thresholdAngle)
        {
            NormalSolver.RecalculateNormals(mf.mesh, thresholdAngle, transform);
            thresholdAngleINIT = thresholdAngle;
        }

        EdgeLoopScaler();
    }

    // return a position from a vertex
    public Vector3 GetVertexPos(int i)
    {
        mf = GetComponent<MeshFilter>();
        return mf.mesh.vertices[i];
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
        for (int i = 0; i < verts.Length; i++) verts[i].point = positions[i];

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

        // gets verts for each edgeLoop
        for (int i = 0; i < fixedEdgeLoops; i++)
        {
            mainEdgeLoopIndices.Add(i, new List<int>());
            temporaryEdgeLoopIndices.Add(i, new List<int>());

            edgeLoopScales.Add(1);
            tempEdgeLoopScales.Add(1);
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
 
        //var triangleIndices = new int[triIndexCount*2];
        var triangleIndices = new List<int>();
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        // setting up Vertices , normals , uvs
        ShapeData shapeData = VertsCalculator(shape, path, vertsInShape, vertices, normals, uvs);
        vertices = shapeData.vertecies;
        normals = shapeData.normals;
        uvs = shapeData.UVs;

        // setting up Triangles
        triangleIndices = TrianglesSetter(triangleIndices, vertsInShape, segments, shape, path);

        // faces at the end of extruded shape
        var shapeData3 = FacesAtEachEnd(vertices, normals, triangleIndices, initialTriangles, vertsInShape, path);
        vertices = shapeData3.vertecies;
        normals = shapeData3.normals;
        triangleIndices = shapeData3.triangles;

        mesh.Clear ();
        mesh.vertices = vertices;
        mesh.normals = normals;
        //mesh.uv = uvs;
        mesh.triangles = triangleIndices.ToArray();
        NormalSolver.RecalculateNormals(mesh, thresholdAngle, transform);
    }

    // setting up Triangles
    public List<int> TrianglesSetter(List<int> triangleIndices, int vertsInShape, int segments, ExtrudeShape shape, OrientedPoint[] path)
    {
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
                triangleIndices.Add(currentB); triangleIndices.Add(currentA); triangleIndices.Add(nextA);
                triangleIndices.Add(nextA); triangleIndices.Add(nextB); triangleIndices.Add(currentB);

                // triangles on down side
                triangleIndices.Add(nextA); triangleIndices.Add(currentA); triangleIndices.Add(currentB);
                triangleIndices.Add(currentB); triangleIndices.Add(nextB); triangleIndices.Add(nextA);
            }
        }

        return triangleIndices;
    }

    // faces at the end of extruded shape
    public ShapeData FacesAtEachEnd(Vector3[] vertices, Vector3[] normals, List<int> triangleIndices , int[] initialTriangles, int vertsInShape, OrientedPoint[] path)
    {
        // getting the vertices for the extruded shape
        var myVertices = new List<Vector3>();
        var myNormals = new List<Vector3>();
        for (int i = 0; i < vertices.Length; i++) { myVertices.Add(vertices[i]); myNormals.Add(normals[i]); }

        /** Start Face **/
        // last vertex index number
        var vertOffset = myVertices.Count;
        // adding new vertices for the face
        for (int i = 0; i < vertsInShape; i++)
        {
            myVertices.Add(myVertices[i]);
            temporaryEdgeLoopIndices[0].Add(myVertices.Count - 1); // gets verts for each edgeLoop
        }
        for (int i = 0; i < initialNormals.Length; i++) myNormals.Add(initialNormals[i]);
        // making triangles between the new added vertices
        for (int i = 0; i < initialTriangles.Length; i++) triangleIndices.Add(initialTriangles[i] + vertOffset);
        for (int i = 0; i < initialTriangles.Length; i += 3)
        {
            triangleIndices.Add(initialTriangles[i + 2] + vertOffset);
            triangleIndices.Add(initialTriangles[i + 1] + vertOffset);
            triangleIndices.Add(initialTriangles[i] + vertOffset);
        }

        /** End Face **/
        // offset for last edge loop
        var lastEdgeLoopStartIndexOffset = (path.Length-1) * vertsInShape;
        vertOffset = myVertices.Count;
        // adding new vertices and normals on the last edgeLoop
        for (int i = 0; i < vertsInShape; i++)
        {
            myVertices.Add(myVertices[i + lastEdgeLoopStartIndexOffset]);
            temporaryEdgeLoopIndices[path.Length - 1].Add(myVertices.Count - 1); // gets verts for each edgeLoop
        }
        for (int i = 0; i < initialNormals.Length; i++) myNormals.Add(-initialNormals[i]);
        // creating triangles on the newly created face
        for (int i = 0; i < initialTriangles.Length; i++) triangleIndices.Add(initialTriangles[i] + vertOffset);
        for (int i = 0; i < initialTriangles.Length; i+=3)
        {
            triangleIndices.Add(initialTriangles[i+2] + vertOffset);
            triangleIndices.Add(initialTriangles[i+1] + vertOffset);
            triangleIndices.Add(initialTriangles[i] + vertOffset);
        }

        return new ShapeData(myVertices.ToArray(), myNormals.ToArray(), triangleIndices);
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
        for (int edgeLoop = 0; edgeLoop < path.Length; edgeLoop++)
        {
            // distance covered for the v coordinates
            int offset = edgeLoop * vertsInShape;
            if (edgeLoop > 0)
            {
                var d = Vector3.Distance(path[edgeLoop].position, path[edgeLoop - 1].position);
                distanceCovered += d;
            }
            float v = distanceCovered / totalLength;

            // get world points of vertices and assign them
            for (int vert = 0; vert < vertsInShape; vert++)
            {
                int id = offset + vert;
                vertices[id] = path[edgeLoop].LocalToWorld(shape.verts[vert].point);
                normals[id] = path[edgeLoop].LocalToWorldDirection(shape.verts[vert].normal);
                uvs[id] = new Vector2(shape.verts[vert].uCoord, v);
                
                temporaryEdgeLoopIndices[edgeLoop].Add(id); // gets verts for each edgeLoop
            }

            // get the center of each edgeloop
            edgeLoopCenters.Add(path[edgeLoop].LocalToWorld(centerOfMesh));
        }

        return new ShapeData(vertices, normals, uvs);
    }

    // scales all vertices of an edgeLoop relative to a center
    public void EdgeLoopScaler()
    {
        var vertices = mf.mesh.vertices;

        for (int edgeLoopIndex = 0; edgeLoopIndex < edgeLoopScales.Count; edgeLoopIndex++)
        {
            var center = edgeLoopCenters[edgeLoopIndex];

            if (edgeLoopScales[edgeLoopIndex] != tempEdgeLoopScales[edgeLoopIndex])
            {
                for (int indice = 0; indice < mainEdgeLoopIndices[edgeLoopIndex].Count; indice++)
                {
                    var scale = edgeLoopScales[edgeLoopIndex];
                    var pastScale = tempEdgeLoopScales[edgeLoopIndex];
                    var vertIndice = mainEdgeLoopIndices[edgeLoopIndex][indice];

                    var currentDist = vertices[vertIndice] - center;
                    var originalDist = currentDist / pastScale;
                    var scaledDist = originalDist * scale;

                    vertices[vertIndice] = scaledDist + center;
                }

                mf.mesh.vertices = vertices;
                tempEdgeLoopScales[edgeLoopIndex] = edgeLoopScales[edgeLoopIndex];
                NormalSolver.RecalculateNormals(mf.mesh, thresholdAngle, transform);
            }
        }
    }

    // gets a 2D mesh's info : vertices, normals, lines 
    public void GetMeshInfo(MeshFilter mf)
    {
        // getting vertices
        List<Vector3> meshVertices = new List<Vector3>();
        foreach (var pos in mf.mesh.vertices) meshVertices.Add(pos);

        // getting lines
        List<int> meshLines = new List<int>();
        for (int i = 0; i < meshLineIndices.Count; i++)
        {
            meshLines.Add(meshLineIndices[i]);
            if (i < meshLineIndices.Count - 1) meshLines.Add(meshLineIndices[i + 1]);
            else if (i == meshLineIndices.Count - 1) meshLines.Add(meshLineIndices[i]);
        }

        // passing in the info to usable variables
        positions = meshVertices.ToArray();
        initialNormals = mf.mesh.normals;
        initialTriangles = mf.mesh.triangles;
        lines = meshLines.ToArray();
        centerOfMesh = mf.mesh.bounds.center; // get center of initial mesh
    }

    // rotate vertex to face a degree
    public Vector3 RotateVertex(Vector3 center, Vector3 vert, Quaternion angle)
    {
        // rotates the vertex
        vert = angle * (vert - center) + center;

        return vert;
    }

    // update the new edgeLoop vertices
    public void updateEdgeLoopVerts(int indice, int newIndice)
    {
        bool found = false;
        for (int edgeLoop = 0; edgeLoop < temporaryEdgeLoopIndices.Keys.Count; edgeLoop++)
        {
            for (int index = 0; index < temporaryEdgeLoopIndices[edgeLoop].Count; index++)
            {
                if(indice == temporaryEdgeLoopIndices[edgeLoop][index])
                {
                    mainEdgeLoopIndices[edgeLoop].Add(newIndice);
                    found = true;
                    break;
                }
            }
            if (found) break;
        }
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
        public List<int> triangles;

        public ShapeData(Vector3[] vertecies, Vector3[] normals, Vector2[] UVs, List<int> triangles = null)
        {
            this.vertecies = vertecies;
            this.normals = normals;
            this.UVs = UVs;
            this.triangles = triangles;
        }

        public ShapeData(Vector3[] vertecies, Vector3[] normals, List<int> triangles, Vector2[] UVs = null)
        {
            this.vertecies = vertecies;
            this.normals = normals;
            this.UVs = UVs;
            this.triangles = triangles;
        }
    }
}