using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TB;

[RequireComponent(typeof(MeshFilter))]
public class GenerateMesh0 : MonoBehaviour {
    /* scratchpad */
    [HideInInspector] public MeshFilter mf;
    public SplineComponent spline;
    List<Vector3> pointList;
    Vertex[] verts;
    
    public float fixedEdgeLoops = 3f;
    public Vector3 inititalRotation = new Vector3(0, 0, 0);
    Vector3[] positions;
    Vector3[] normals, initialNormals;
    float[] uCoords;
    int[] lines;
    int[] initialTriangles;
    public float thresholdAngle, thresholdAngleINIT;
    public float normalsGizmosLinesLength;

    public List<int> randomList;

    public void addToRandomList(int i)
    {
        randomList.Add(i);
    }

    void Start () {
        mf = GetComponent<MeshFilter> ();
        pointList = new List<Vector3>();

        // gets a 2D mesh's info : vertices, normals, lines 
        GetMeshInfo(mf);

        // generate mesh from the start
        GenerateMesh ();

        thresholdAngleINIT = thresholdAngle;
    }

    private void Update()
    {
        if (thresholdAngleINIT != thresholdAngle)
        {
            GenerateMesh();
            thresholdAngleINIT = thresholdAngle;
        }
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
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].point = positions[i];
            //verts[i].normal = normals[i];
            //verts[i].uCoord = uCoords[i];
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

        ShapeData shapeData2 = CalculateTheNormals(vertices, triangleIndices.ToArray());
        vertices = shapeData2.vertecies;
        normals = shapeData2.normals;
        triangleIndices = shapeData2.triangles;

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

        NormalSolver.RecalculateNormals(mesh, thresholdAngle);
        NormalSolver.RecalculateTangents(mesh);
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
        for (int i = 0; i < vertsInShape; i++) myVertices.Add(myVertices[i]);
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
        for (int i = 0; i < vertsInShape; i++) myVertices.Add(myVertices[i + lastEdgeLoopStartIndexOffset]);
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

        // rotate vertex to face a degree if we entered an angle
        if (inititalRotation != Vector3.zero)
            for (int i = 0; i < shape.verts.Length; i++)
            {
                // rorating the shape to the direction of the first point
                shape.verts[i].point = RotateVertex(shape.verts[0].point, shape.verts[i].point, path);
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
            for (int j = 0; j < vertsInShape; j++)
            {
                int id = offset + j;
                vertices[id] = path[edgeLoop].LocalToWorld(shape.verts[j].point);
                normals[id] = path[edgeLoop].LocalToWorldDirection(shape.verts[j].normal);
                uvs[id] = new Vector2(shape.verts[j].uCoord, v);
            }
        }

        return new ShapeData(vertices, normals, uvs);
    }

    // converts mesh into data
    public void MeshToDataConverter(Mesh mesh)
    {
        ShapeData shapeData;
        shapeData.vertecies = mesh.vertices;
        shapeData.normals = mesh.normals;
    }

    // gets a 2D mesh's info : vertices, normals, lines 
    public void GetMeshInfo(MeshFilter mf)
    {
        // getting vertices
        List<Vector3> meshVertices = new List<Vector3>();
        foreach (var pos in mf.mesh.vertices) meshVertices.Add(pos);

        // getting lines
        List<int> meshLines = new List<int>();
        /*for (int i = 0; i < mf.mesh.triangles.Length; i += 3)
        {
            meshLines.Add(mf.mesh.triangles[i]);
            meshLines.Add(mf.mesh.triangles[i + 1]);

            meshLines.Add(mf.mesh.triangles[i]);
            meshLines.Add(mf.mesh.triangles[i + 2]);

            meshLines.Add(mf.mesh.triangles[i + 1]);
            meshLines.Add(mf.mesh.triangles[i + 2]);
        }*/
        for (int i = 0; i < randomList.Count; i++)
        {
            meshLines.Add(randomList[i]);
            if(i < randomList.Count-1) meshLines.Add(randomList[i + 1]);
            else meshLines.Add(randomList[0]);
        }

        // getting normals
        List<Vector3> meshNormals = new List<Vector3>();
        //meshNormals = CalculateNormals(meshVertices);

        // passing in the info to usable variables
        positions = meshVertices.ToArray();
        initialNormals = mf.mesh.normals;
        initialTriangles = mf.mesh.triangles;
        //normals = meshNormals.ToArray();
        lines = meshLines.ToArray();
    }

    // calculates the normals on a circle
    public List<Vector3> CalculateNormals(List<Vector3> meshVertices)
    {
        // point for the diameter of the disc
        var firstPos = meshVertices[0];
        var secondPos = meshVertices[31];
        var center = (firstPos + secondPos) / 2;

        // for each point on the dic calculate the normal
        List<Vector3> normals = new List<Vector3>();
        for (int i = 0; i < meshVertices.Count; i++)
        {
            var newNormal = (meshVertices[i] - center).normalized;
            normals.Add(newNormal);
        }

        return normals;
    }

    // rotate vertex to face a degree
    public Vector3 RotateVertex(Vector3 center, Vector3 vert, OrientedPoint[] path)
    {
        //the degrees the vertices are to be rotated, for example (0,90,0) 
        Quaternion newRotation = new Quaternion();
        newRotation.eulerAngles = inititalRotation;

        // rotates the vertex
        vert = newRotation * (vert - center) + center;

        return vert;
    }

    // rotate mesh to a degree
    public ExtrudeShape RotateMesh(ExtrudeShape shape)
    {
        var vertices = shape.verts;

        Vector3 center = vertices[0].point;//any V3 you want as the pivot point.
        Quaternion newRotation = new Quaternion();
        newRotation.eulerAngles = new Vector3(90, 0, 0);//the degrees the vertices are to be rotated, for example (0,90,0) 

        for (int i = 0; i < vertices.Length; i++)
        {//vertices being the array of vertices of your mesh
            vertices[i].point = newRotation * (vertices[i].point - center) + center;
        }

        shape.verts = vertices;

        return shape;
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

    // calculates all the normals of the shape
    public ShapeData CalculateTheNormals(Vector3[] vertices, int[] triangleIndices)
    {
        // getting the infos
        var cosineThreshold = Mathf.Cos(thresholdAngle * Mathf.Deg2Rad);
        var myVertices = new List<Vector3>();
        var myNormals = new List<Vector3>();
        var myTriangles = new List<int>();
        foreach (var v in vertices) { myVertices.Add(v); myNormals.Add(Vector3.zero); }

        // setting all vertices as havent seen before
        var foundIndices = new Dictionary<Vector3, bool>();
        for (int i = 0; i < myVertices.Count; i++) foundIndices.Add(myVertices[i], false);

        // for each triangle
        for (int triIndice = 0; triIndice < triangleIndices.Length; triIndice+=3)
        {
            // calculate triangle normal
            var pointA = triangleIndices[triIndice + 0];
            var pointB = triangleIndices[triIndice + 1];
            var pointC = triangleIndices[triIndice + 2];
            var normal = GetTriangleNormal(myVertices, pointA, pointB, pointC);

            // going thru each point of the triangle to see if we seen this or not
            var points = new int[] { pointA, pointB, pointC };
            int triPointsOffset = 0;
            foreach(var p in points)
            {
                // if first time seeing this point then add a normal to it and move on
                if (foundIndices[vertices[p]] == false)
                {
                    foundIndices[vertices[p]] = true;
                    myNormals[p] = normal;
                    triPointsOffset++;
                }
                // if not first time seeing it then make a new vertex and a new normal and replace it on the triangle
                else
                {
                    // The dot product is the cosine of the angle between the two triangles.
                    // A larger cosine means a smaller angle.
                    /*var dot = Vector3.Dot( normal, myNormals[p]);
                    if (dot >= cosineThreshold)
                    {
                        myNormals[p] += normal;
                        triPointsOffset++;
                    }
                    else
                    {*/
                        myVertices.Add(myVertices[p]);
                        myNormals.Add(normal);
                        triangleIndices[triIndice + triPointsOffset] = myVertices.Count - 1;
                        triPointsOffset++;
                    //}
                }
            }
        }

        // normalizing all the normals
        for (int i = 0; i < myNormals.Count; i++) myNormals[i].Normalize();
        // putting all the information that we just made into a new triangle
        foreach (var t in triangleIndices) myTriangles.Add(t);

        return new ShapeData(myVertices.ToArray(), myNormals.ToArray(), myTriangles);
    }

    // calculates normal for a triangle
    public Vector3 GetTriangleNormal(List<Vector3> vertices, int a, int b, int c)
    {
        var pointA = vertices[a];
        var pointB = vertices[b];
        var pointC = vertices[c];

        var vectAB = pointB - pointA;
        var vectAC = pointC - pointA;

        return Vector3.Cross(vectAB, vectAC).normalized;
    }
}