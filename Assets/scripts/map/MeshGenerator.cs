﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MeshGenerator : MonoBehaviour
{
    [Header("Mesh Objects")]
    public MeshFilter walls;
    public MeshFilter cave;
    public MeshFilter floor;

    [Header("Mesh Parameters")]
    public float squareSize = 1;
    public float wallHeight = 5;
    public bool is2D;
    
    private List<Vector3> vertices;
    private List<int> triangles;

    private Dictionary<int, List<Triangle>> triangleDictionary;
    private List<List<int>> outlines;
    private HashSet<int> checkedVertices;

    private SquareGrid squareGrid;

    // private bool generating;
    
    private void Init(ushort[,] map, float squareSize)
    {
        vertices = new List<Vector3>();

        triangles = new List<int>();

        triangleDictionary = new Dictionary<int, List<Triangle>>();

        outlines = new List<List<int>>();

        checkedVertices = new HashSet<int>();

        squareGrid = new SquareGrid(map, squareSize);
    }
    
    internal SquareGrid GetSquareGrid()
    {
        return this.squareGrid;
    }

    public void GenerateMesh(ushort[,] map)
    {
        // this.generating = true;

        Init(map, squareSize);

        MeshCollider caveCollider = walls.GetComponent<MeshCollider>();
        Destroy(caveCollider);

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();

        cave.mesh = mesh;

        caveCollider = cave.gameObject.AddComponent<MeshCollider>();
        caveCollider.sharedMesh = mesh;

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;


        if (is2D)
        {
            Generate2DColliders();
        }
        else
        {
            CreateWallMesh();
            CreateFloorMesh();
        }

        // this.generating = false;

    }

    private void CreateFloorMesh()
    {
        MeshCollider floorCollider = floor.GetComponent<MeshCollider>();
        Destroy(floorCollider);

        List<Vector3> floorVertices = new List<Vector3>();
        List<int> floorTriangles = new List<int>();
        
        int width = squareGrid.controlNodes.GetLength(0);
        int heigth = squareGrid.controlNodes.GetLength(1);

        Vector3 left = squareGrid.controlNodes[0, heigth - 1].position;
        Vector3 bottomLeft = squareGrid.controlNodes[0, 0].position;
        Vector3 bottomRight = squareGrid.controlNodes[width - 1, 0].position;
        Vector3 right = squareGrid.controlNodes[width - 1, heigth - 1].position;
        
        floorVertices.Add(left);
        floorVertices.Add(bottomLeft);
        floorVertices.Add(bottomRight);
        floorVertices.Add(right);

        floorTriangles.Add(0);
        floorTriangles.Add(3);
        floorTriangles.Add(2);

        floorTriangles.Add(2);
        floorTriangles.Add(1);
        floorTriangles.Add(0);

        Mesh floorMesh = new Mesh();

        floorMesh.vertices = floorVertices.ToArray();
        floorMesh.triangles = floorTriangles.ToArray();

        floorMesh.RecalculateNormals();

        floor.mesh = floorMesh;

        floorCollider = floor.gameObject.AddComponent<MeshCollider>();
        floorCollider.sharedMesh = floorMesh;

        floor.gameObject.transform.position = new Vector3(
            transform.position.x,
            transform.position.y - wallHeight,
            transform.position.z);
    }

    void CreateWallMesh()
    {
        MeshCollider wallCollider = walls.GetComponent<MeshCollider>();
        Destroy(wallCollider);

        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        
        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); // left
                wallVertices.Add(vertices[outline[i + 1]]); // right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                wallTriangles.Add(startIndex + 0); // left
                wallTriangles.Add(startIndex + 2); // bottom left
                wallTriangles.Add(startIndex + 3); // bottom right

                wallTriangles.Add(startIndex + 3); // bottom right
                wallTriangles.Add(startIndex + 1); // right
                wallTriangles.Add(startIndex + 0); // left
            }
        }

        Mesh wallMesh = new Mesh();

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();

        wallMesh.RecalculateNormals();

        walls.mesh = wallMesh;

        wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;

        transform.position = new Vector3(
           transform.position.x,
           wallHeight,
           transform.position.z);
    }

    void Generate2DColliders()
    {

        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++)
        {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutlines();

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
            }
            edgeCollider.points = edgePoints;
        }

    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);

                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);

                break;
        }

    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);

        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);

        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);

        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);

    }

    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);

        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    void CalculateMeshOutlines()
    {

        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }

        SimplifyMeshOutlines();
    }

    void SimplifyMeshOutlines()
    {
        for (int outlineIndex = 0; outlineIndex < outlines.Count; outlineIndex++)
        {
            List<int> simplifiedOutline = new List<int>();
            Vector3 dirOld = Vector3.zero;
            for (int i = 0; i < outlines[outlineIndex].Count; i++)
            {
                Vector3 p1 = vertices[outlines[outlineIndex][i]];
                Vector3 p2 = vertices[outlines[outlineIndex][(i + 1) % outlines[outlineIndex].Count]];
                Vector3 dir = p1 - p2;
                if (dir != dirOld)
                {
                    dirOld = dir;
                    simplifiedOutline.Add(outlines[outlineIndex][i]);
                }
            }
            outlines[outlineIndex] = simplifiedOutline;
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if (trianglesContainingVertexA[i].Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }


        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

    internal class SquareGrid
    {
        public float squareSize;

        public Square[,] squares;

        public ControlNode[,] controlNodes;
        
        public SquareGrid(ushort[,] map, float squareSize)
        {
            this.squareSize = squareSize;

            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);

            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(
                        -mapWidth / 2 + x * squareSize + squareSize / 2,
                        0,
                        -mapHeight / 2 + y * squareSize + squareSize / 2);

                    controlNodes[x, y] = new ControlNode(pos, new MapGenerator.Coord(x, y), map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];

            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(
                        controlNodes[x, y + 1],
                        controlNodes[x + 1, y + 1],
                        controlNodes[x + 1, y],
                        controlNodes[x, y]);
                }
            }
        }

        public Vector3 GetPosition(MapGenerator.Coord mapCoords)
        {
            if (CommonUtils.IsInRange(
                mapCoords.tileX,
                mapCoords.tileY,
                controlNodes.GetLength(0),
                controlNodes.GetLength(1)) &&
                controlNodes[mapCoords.tileX, mapCoords.tileY] != null)
            {
                return controlNodes[mapCoords.tileX, mapCoords.tileY].position;
            }
            else
            {
                return Vector3.negativeInfinity;

            }
        }
    }

    internal class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            if (topLeft.active)
                configuration += 8;

            if (topRight.active)
                configuration += 4;

            if (bottomRight.active)
                configuration += 2;

            if (bottomLeft.active)
                configuration += 1;
        }

    }

    internal class Node
    {
        public Vector3 position;

        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    internal class ControlNode : Node
    {
        public MapGenerator.Coord mapCoords;

        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, MapGenerator.Coord _mapCoords, bool _active, float squareSize) : base(_pos)
        {
            mapCoords = _mapCoords;
            active = _active;

            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }
    }
}
