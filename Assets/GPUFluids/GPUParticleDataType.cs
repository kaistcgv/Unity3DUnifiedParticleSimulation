using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct IVector3
{
    public int x;
    public int y;
    public int z;

    public IVector3(int ix, int iy, int iz)
    {
        x = ix;
        y = iy;
        z = iz;
    }
};

public struct UVector3
{
    public uint x;
    public uint y;
    public uint z;

    public UVector3(uint ux, uint uy, uint uz)
    {
        x = ux;
        y = uy;
        z = uz;
    }
};

public struct SimulationParameters
{
    public int maxParticles;
    public int numParticles;
    public float timestep;
    public float smoothingLength;
    public float smoothingLengthSq;
    public float invSmoothingLength;
    public float restDensity;
    public float artificialViscosity;
    public float velocityLimit;
    public float spacing;
    public Vector3 worldOrigin;
    public Vector3 cellSize;
    public UVector3 gridSize;
    public Vector3 boxSize;
    public int enableBox;
    public uint numCells;

    public int numSphereColliders;
    public int numCapsuleColliders;
    public int numBoxColliders;

    public float coeffWeightedVolume;
    public float coeffDensity;
    public float coeffPressure;
    public float coeffViscosity;
    public float coeffCSKernel;
    public float coeffCSGradient;
};

public struct SortData
{
    public uint key;
    public uint index;
}

public struct Cell
{
    public uint begin;
    public uint end;
}

public struct GPUAABB
{
    public Vector3 center;
    public Vector3 extents;
};

public struct GPUSphere
{
    public Vector3 center;
    public float radius;
};

public struct GPUCapsule
{
    public Vector3 pos1;
    public Vector3 pos2;
    public float radius;
};

public struct GPUPlane 
{
    public Vector3 normal;
    public float distance;
};

public struct GPUBox
{
    public Vector3 center;
    public GPUPlane plane0;
    public GPUPlane plane1;
    public GPUPlane plane2;
    public GPUPlane plane3;
    public GPUPlane plane4;
    public GPUPlane plane5;
};

public struct GPUSphereCollider
{
    public GPUAABB aabb;
    public GPUSphere shape;
};

public struct GPUCapsuleCollider
{
    public GPUAABB aabb;
    public GPUCapsule shape;
};

public struct GPUBoxCollider
{
    public GPUAABB aabb;
    public GPUBox shape;
};

public class ColliderImplementation
{
    static public void BuildAABB<T>(ref GPUAABB aabb, T col) where T: Collider
    {
        aabb.center = col.bounds.center;
        aabb.extents = col.bounds.extents;
    }

    static public void BuildSphereCollider(ref GPUSphereCollider col, Transform t, float radius)
    {
        col.shape.center = t.position;
        col.shape.radius = radius * t.localScale.x;
        col.aabb.center = t.position;
        col.aabb.extents = Vector3.one * col.shape.radius;
    }

    static public void BuildCapsuleCollider(ref GPUCapsuleCollider col, Transform t, float radius, float length, int dir)
    {
        Vector3 e = Vector3.zero;
        float h = Mathf.Max(0.0f, length - radius * 2.0f);
        float r = radius * t.localScale.x;
        switch (dir)
        {
            case 0: e.Set(h * 0.5f, 0.0f, 0.0f); break;
            case 1: e.Set(0.0f, h * 0.5f, 0.0f); break;
            case 2: e.Set(0.0f, 0.0f, h * 0.5f); break;
        }
        Vector4 pos1 = new Vector4(e.x, e.y, e.z, 1.0f);
        Vector4 pos2 = new Vector4(-e.x, -e.y, -e.z, 1.0f);
        pos1 = t.localToWorldMatrix * pos1;
        pos2 = t.localToWorldMatrix * pos2;
        col.shape.radius = r;
        col.shape.pos1 = pos1;
        col.shape.pos2 = pos2;
        col.aabb.center = t.position;
        col.aabb.extents = Vector3.one * (r + h);
    }

    static public void BuildBox(ref GPUBox shape, Matrix4x4 mat, Vector3 size)
    {
        size = 0.5f * size;
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(size.x, size.y, size.z),
            new Vector3(-size.x, size.y, size.z),
            new Vector3(-size.x, -size.y, size.z),
            new Vector3(size.x, -size.y, size.z),
            new Vector3(size.x, size.y, -size.z),
            new Vector3(-size.x, size.y, -size.z),
            new Vector3(-size.x, -size.y, -size.z),
            new Vector3(size.x, -size.y, -size.z)
        };

        for(int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = mat * vertices[i];
        }

        Vector3[] normals = new Vector3[6]
        {
            Vector3.Cross(vertices[3] - vertices[0], vertices[4] - vertices[0]).normalized,
            Vector3.Cross(vertices[5] - vertices[1], vertices[2] - vertices[1]).normalized,
            Vector3.Cross(vertices[7] - vertices[3], vertices[2] - vertices[3]).normalized,
            Vector3.Cross(vertices[1] - vertices[0], vertices[4] - vertices[0]).normalized,
            Vector3.Cross(vertices[1] - vertices[0], vertices[3] - vertices[0]).normalized,
            Vector3.Cross(vertices[7] - vertices[4], vertices[5] - vertices[4]).normalized
        };

        float[] distances = new float[6] {
            -Vector3.Dot(vertices[0], normals[0]),
            -Vector3.Dot(vertices[1], normals[1]),
            -Vector3.Dot(vertices[0], normals[2]),
            -Vector3.Dot(vertices[3], normals[3]),
            -Vector3.Dot(vertices[0], normals[4]),
            -Vector3.Dot(vertices[4], normals[5]),
        };

        shape.center = mat.GetColumn(3);
        shape.plane0.normal = normals[0];
        shape.plane0.distance = distances[0];
        shape.plane1.normal = normals[1];
        shape.plane1.distance = distances[1];
        shape.plane2.normal = normals[2];
        shape.plane2.distance = distances[2];
        shape.plane3.normal = normals[3];
        shape.plane3.distance = distances[3];
        shape.plane4.normal = normals[4];
        shape.plane4.distance = distances[4];
        shape.plane5.normal = normals[5];
        shape.plane5.distance = distances[5];
    }

    static public void BuildBoxCollider(ref GPUBoxCollider col, Transform t, Vector3 size)
    {
        BuildBox(ref col.shape, t.localToWorldMatrix, size);

        Vector3 scaled = new Vector3(
            size.x * t.localScale.x,
            size.y * t.localScale.y,
            size.z * t.localScale.z );
        float s = Mathf.Max(Mathf.Max(scaled.x, scaled.y), scaled.z);

        col.aabb.center = t.position;
        col.aabb.extents = Vector3.one * s * 1.415f;
    }
}

public class GPUParticleUtils
{
    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    public struct VertexT
    {
        public const int size = 48;

        public Vector3 vertex;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 texcoord;
    }

    public static void CreateVertexBuffer(Mesh mesh, ref ComputeBuffer ret, ref int num_vertices)
    {
        int[] indices = mesh.GetIndices(0);
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Vector2[] uv = mesh.uv;

        VertexT[] v = new VertexT[indices.Length];
        if (vertices != null) { for (int i = 0; i < indices.Length; ++i) { v[i].vertex = vertices[indices[i]]; } }
        if (normals != null) { for (int i = 0; i < indices.Length; ++i) { v[i].normal = normals[indices[i]]; } }
        if (tangents != null) { for (int i = 0; i < indices.Length; ++i) { v[i].tangent = tangents[indices[i]]; } }
        if (uv != null) { for (int i = 0; i < indices.Length; ++i) { v[i].texcoord = uv[indices[i]]; } }

        ret = new ComputeBuffer(indices.Length, VertexT.size);
        ret.SetData(v);
        num_vertices = v.Length;
    }
}
