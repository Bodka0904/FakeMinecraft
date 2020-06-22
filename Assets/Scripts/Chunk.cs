using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Voxel
{
    public Voxel()
    {
        m_HP = 0;
        m_Type = 0;
    }
    public Voxel(byte hp,byte type)
    {
        m_HP = hp;
        m_Type = type;
    }
    public byte m_HP;
    public byte m_Type;
}

[System.Serializable]
public class ChunkSave
{
    public ChunkCoord m_Coord;
    public Voxel[] m_VoxelMap = new Voxel[VoxelData.m_ChunkWidth* VoxelData.m_ChunkHeight* VoxelData.m_ChunkWidth];
}


public class Chunk
{
    public ChunkCoord m_Coord;

    GameObject m_ChunkObject;
    MeshRenderer m_MeshRenderer;
    MeshFilter m_MeshFilter;
    MeshCollider m_MeshCollider;

    int m_VertexIndex = 0;
    List<Vector3> m_Vertices = new List<Vector3>();
    List<int> m_Triangles = new List<int>();
    List<Vector2> m_UVS = new List<Vector2>();


    public Voxel[,,] m_VoxelMap = new Voxel[VoxelData.m_ChunkWidth, VoxelData.m_ChunkHeight, VoxelData.m_ChunkWidth];
    Voxel m_LastModified;

    World m_World;

    public bool m_IsVoxelMapPopulated = false;

    public Chunk(ChunkCoord coord,World world,bool generateOnLoad)
    {
        m_Coord = coord;
        m_World = world;
        m_ChunkObject = new GameObject();
        if (generateOnLoad)
            Init();
        
    }

    public void Init()
    {    
        m_MeshFilter = m_ChunkObject.AddComponent<MeshFilter>();
        m_MeshRenderer = m_ChunkObject.AddComponent<MeshRenderer>();
        m_MeshCollider = m_ChunkObject.AddComponent<MeshCollider>();

        m_MeshRenderer.material = m_World.m_Material;
        m_ChunkObject.transform.SetParent(m_World.transform);
        m_ChunkObject.transform.position = new Vector3(m_Coord.m_X * VoxelData.m_ChunkWidth, 0f, m_Coord.m_Z * VoxelData.m_ChunkWidth);
        m_ChunkObject.name = "Chunk " + m_Coord.m_X + ", " + m_Coord.m_Z;

        PopulateVoxelMap();
        UpdateMeshData();     
    }

    public void UpdateMeshData()
    {
        ClearMeshData();
        for (int y = 0; y < VoxelData.m_ChunkHeight; ++y)
        {
            for (int x = 0; x < VoxelData.m_ChunkWidth; ++x)
            {
                for (int z = 0; z < VoxelData.m_ChunkWidth; ++z)
                {
                    if (m_World.m_BlockTypes[m_VoxelMap[x,y,z].m_Type].m_IsSolid)
                        AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }
        CreateMesh();
    }


    void ClearMeshData()
    {
        m_VertexIndex = 0;
        m_Vertices.Clear();
        m_Triangles.Clear();
        m_UVS.Clear();
    }
   

    public bool IsActive
    {
        get { return m_ChunkObject.activeSelf; }
        set { m_ChunkObject.SetActive(value); }
    }


    public Vector3 Position
    {
        get { return m_ChunkObject.transform.position; }
    }

    bool IsVoxelInChunk(int x,int y,int z)
    {
        return !((x < 0 || x > VoxelData.m_ChunkWidth - 1) || (y < 0 || y > VoxelData.m_ChunkHeight - 1) || (z < 0 || z > VoxelData.m_ChunkWidth - 1));
    }

    public void EditVoxel(Vector3 pos,byte blockType)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(m_ChunkObject.transform.position.x);
        z -= Mathf.FloorToInt(m_ChunkObject.transform.position.z);

        
        m_VoxelMap[x, y, z].m_Type = blockType;

        UpdateSurroundingVoxels(x, y, z);
        UpdateMeshData();
        m_LastModified = m_VoxelMap[x, y, z];
    }

    public void DamageVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(m_ChunkObject.transform.position.x);
        z -= Mathf.FloorToInt(m_ChunkObject.transform.position.z);

        if (m_LastModified != null && m_LastModified != m_VoxelMap[x,y,z])
            m_VoxelMap[x, y, z].m_HP = m_World.m_BlockTypes[(int)m_LastModified.m_Type].m_Hardness;

        m_VoxelMap[x, y, z].m_HP--;
        if (m_VoxelMap[x, y, z].m_HP == 0)
        {
            m_VoxelMap[x, y, z].m_Type = (byte)Type.Air;
            UpdateSurroundingVoxels(x, y, z);
            UpdateMeshData();
        }

        m_LastModified = m_VoxelMap[x, y, z];
    }
 
    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);
        for (int p = 0; p < 6; ++p)
        {
            Vector3 current = thisVoxel + VoxelData.FaceCheck[p];

            if (!IsVoxelInChunk((int)current.x,(int)current.y,(int)current.z))
            {
                m_World.GetChunkFromPosition(current + Position).UpdateMeshData();
            }
        }
    }
    bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return m_World.CheckForVoxel(pos + Position);

        return m_World.m_BlockTypes[ m_VoxelMap[x, y, z].m_Type].m_IsSolid;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        x -= Mathf.FloorToInt(m_ChunkObject.transform.position.x);
        z -= Mathf.FloorToInt(m_ChunkObject.transform.position.z);
        
        return m_VoxelMap[x, y, z].m_Type;
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.m_ChunkHeight; ++y)
        {
            for (int x = 0; x < VoxelData.m_ChunkWidth; ++x)
            {
                for (int z = 0; z < VoxelData.m_ChunkWidth; ++z)
                {
                    m_VoxelMap[x, y, z] = m_World.GetVoxel(new Vector3(x, y, z) + Position);
                }
            }
        }
        m_IsVoxelMapPopulated = true;
    }

    void AddVoxelDataToChunk(Vector3 pos)
    {
        for (int p = 0; p < 6;++p)
        {
            if (!CheckVoxel(pos + VoxelData.FaceCheck[p]))
            {
                byte blockID = m_VoxelMap[(int)pos.x, (int)pos.y, (int)pos.z].m_Type;
                m_Vertices.Add(pos + VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 0]]);
                m_Vertices.Add(pos + VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 1]]);
                m_Vertices.Add(pos + VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 2]]);
                m_Vertices.Add(pos + VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 3]]);

                AddTexture(m_World.m_BlockTypes[blockID].GetTextureID(p));

                m_Triangles.Add(m_VertexIndex);
                m_Triangles.Add(m_VertexIndex + 1);
                m_Triangles.Add(m_VertexIndex + 2);
                m_Triangles.Add(m_VertexIndex + 2);
                m_Triangles.Add(m_VertexIndex + 1);
                m_Triangles.Add(m_VertexIndex + 3);
                m_VertexIndex += 4;
            }
        }
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = m_Vertices.ToArray();
        mesh.triangles = m_Triangles.ToArray();
        mesh.uv = m_UVS.ToArray();
        mesh.RecalculateNormals();
        m_MeshFilter.mesh = mesh;
    }

    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.m_TextureAtlasSize;
        float x = textureID - (y * VoxelData.m_TextureAtlasSize);

        x *= VoxelData.m_NormalizedBlockTextureSize;
        y *= VoxelData.m_NormalizedBlockTextureSize;

        m_UVS.Add(new Vector2(x, y));
        m_UVS.Add(new Vector2(x, y + VoxelData.m_NormalizedBlockTextureSize));
        m_UVS.Add(new Vector2(x + VoxelData.m_NormalizedBlockTextureSize, y));
        m_UVS.Add(new Vector2(x + VoxelData.m_NormalizedBlockTextureSize, y + VoxelData.m_NormalizedBlockTextureSize));
    }
}

[System.Serializable]
public class ChunkCoord
{
    public int m_X;
    public int m_Z;

    public ChunkCoord()
    {
        m_X = 0;
        m_Z = 0;
    }
    public ChunkCoord ( int x, int z)
    {
        m_X = x;
        m_Z = z;
    }

    public ChunkCoord(Vector3 pos)
    {
        m_X = Mathf.FloorToInt(pos.x) / VoxelData.m_ChunkWidth;
        m_Z = Mathf.FloorToInt(pos.z) / VoxelData.m_ChunkWidth;
    }

    public bool Equals(ChunkCoord other)
    {
        return (other != null && other.m_X == m_X && other.m_Z == m_Z);
    }
}