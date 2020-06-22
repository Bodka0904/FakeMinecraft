using System.Collections;
using System.Collections.Generic;
using UnityEngine;


enum Type : byte
{
    Air,
    Grass,
    Bedrock,
    Stone,
    Dirt,
    NumTypes
}

[System.Serializable]
public class WorldSave
{
    public int m_Seed;
    public BlockType[] m_BlockTypes;
    public ChunkSave[] m_Chunks = new ChunkSave[VoxelData.m_WorldSizeInChunks * VoxelData.m_WorldSizeInChunks];
}


public class World : MonoBehaviour
{
    GameSaver m_Saver;
    public int m_Seed;
    public BiomeAttributes m_Biome;
    public Transform m_PlayerTransform;
    public Vector3 m_SpawnPosition;

    public Material m_Material;
    public BlockType[] m_BlockTypes;

    ChunkCoord m_PlayerLastChunkCoord;
    public Chunk[,] m_Chunks = new Chunk[VoxelData.m_WorldSizeInChunks, VoxelData.m_WorldSizeInChunks];
    List<ChunkCoord> m_ActiveChunks = new List<ChunkCoord>();
    List<ChunkCoord> m_ChunksToCreate = new List<ChunkCoord>();

    private bool m_IsCreatingChunks;

    private void Start()
    {
        m_PlayerLastChunkCoord = new ChunkCoord();
        Random.InitState(m_Seed);
        m_SpawnPosition = new Vector3(VoxelData.m_WorldSizeInVoxels / 2f, VoxelData.m_ChunkHeight-1, VoxelData.m_WorldSizeInVoxels / 2f);

        GenerateWorld();
        m_Saver = new GameSaver(this);
        m_PlayerTransform.position = m_SpawnPosition;
        m_PlayerLastChunkCoord = GetChunkCoordsFromPosition(m_PlayerTransform.position);
    }


    private void Update()
    {
        ChunkCoord newCoord = GetChunkCoordsFromPosition(m_PlayerTransform.position);
        // If chunk of player did not change do not check view distance
        if (!m_PlayerLastChunkCoord.Equals(newCoord))
            CheckViewDistance();

        if (m_ChunksToCreate.Count > 0 && !m_IsCreatingChunks)
            StartCoroutine(CreateChunks());

        m_PlayerLastChunkCoord = newCoord;

        m_Saver.Update();
    }

    public void GenerateWorld()
    {
        for (int x = (VoxelData.m_WorldSizeInChunks / 2) - VoxelData.m_ViewDistanceInChunks; x < (VoxelData.m_WorldSizeInChunks / 2) + VoxelData.m_ViewDistanceInChunks; ++x)
        {
            for (int z = (VoxelData.m_WorldSizeInChunks / 2) - VoxelData.m_ViewDistanceInChunks; z < (VoxelData.m_WorldSizeInChunks / 2) + VoxelData.m_ViewDistanceInChunks; ++z)
            {
                m_Chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                m_Chunks[x, z].IsActive = true;
                m_ActiveChunks.Add(m_Chunks[x, z].m_Coord);
            }
        }    
    }

    IEnumerator CreateChunks()
    {
        m_IsCreatingChunks = true;

        while (m_ChunksToCreate.Count > 0)
        {
            m_Chunks[m_ChunksToCreate[0].m_X, m_ChunksToCreate[0].m_Z].Init();
            m_ChunksToCreate.RemoveAt(0);
            yield return null;
        }

        m_IsCreatingChunks = false;
    }

    ChunkCoord GetChunkCoordsFromPosition(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.m_ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.m_ChunkWidth);

        return new ChunkCoord(x, z);
    }
    public Chunk GetChunkFromPosition(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.m_ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.m_ChunkWidth);

        return m_Chunks[x, z];
    }
    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordsFromPosition(m_PlayerTransform.position);
        ChunkCoord tmp = new ChunkCoord(0, 0);
       
        foreach (ChunkCoord c in m_ActiveChunks)
            m_Chunks[c.m_X, c.m_Z].IsActive = false;

        m_ActiveChunks.Clear();
        for (int x = coord.m_X - VoxelData.m_ViewDistanceInChunks; x < coord.m_X + VoxelData.m_ViewDistanceInChunks; ++x)
        {
            for (int z = coord.m_Z - VoxelData.m_ViewDistanceInChunks; z < coord.m_Z + VoxelData.m_ViewDistanceInChunks; ++z)
            {
                tmp.m_X = x;
                tmp.m_Z = z;
                if (IsChunkInWorld(tmp))
                {
                    // If chunk does not exist yet create new one
                    if (m_Chunks[x, z] == null)
                    {
                        m_Chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        m_ChunksToCreate.Add(m_Chunks[x, z].m_Coord);               
                    }
                    m_ActiveChunks.Add(m_Chunks[x, z].m_Coord);
                    m_Chunks[x, z].IsActive = true;
                }
            }
        }     
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord chunk = new ChunkCoord(pos);
        if (!IsVoxelInWorld(pos))
            return false;

        if (m_Chunks[chunk.m_X,chunk.m_Z] != null && m_Chunks[chunk.m_X, chunk.m_Z].m_IsVoxelMapPopulated)
        {
            byte index = m_Chunks[chunk.m_X, chunk.m_Z].GetVoxelFromGlobalVector3(pos);
            return m_BlockTypes[index].m_IsSolid;
        }

        return m_BlockTypes[GetVoxel(pos).m_Type].m_IsSolid;
    }

    public Voxel GetVoxel(Vector3 pos)
    {
        if (!IsVoxelInWorld(pos))
            return new Voxel((byte)m_BlockTypes[(int)Type.Air].m_Hardness, (byte)Type.Air);


        int yPos = Mathf.FloorToInt(pos.y);

        if (yPos == 0)
            return new Voxel((byte)m_BlockTypes[(int)Type.Bedrock].m_Hardness, (byte)Type.Bedrock);

        int terrainHeight = Mathf.FloorToInt(m_Biome.m_TerrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, m_Biome.m_TerrainScale)) + m_Biome.m_TerrainHeight;


        // Layer of dirt 5 blocks
        if (yPos < terrainHeight && yPos > terrainHeight - 5)
            return new Voxel((byte)m_BlockTypes[(int)Type.Dirt].m_Hardness,(byte)Type.Dirt);
        else if (yPos < terrainHeight)
            return new Voxel((byte)m_BlockTypes[(int)Type.Stone].m_Hardness, (byte)Type.Stone);
        else if (yPos == terrainHeight)
            return new Voxel((byte)m_BlockTypes[(int)Type.Grass].m_Hardness, (byte)Type.Grass);
       
        return new Voxel();
    }
   
    bool IsChunkInWorld(ChunkCoord coord)
    {
        return (coord.m_X > 0 && coord.m_X < VoxelData.m_WorldSizeInChunks - 1 
            && coord.m_Z > 0 && coord.m_Z < VoxelData.m_WorldSizeInChunks - 1);
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        return (
            pos.x >= 0f && pos.x < VoxelData.m_WorldSizeInVoxels
            && pos.y >= 0f && pos.y < VoxelData.m_ChunkHeight
            && pos.z >= 0f && pos.z < VoxelData.m_WorldSizeInVoxels);
    }
}


[System.Serializable]
public class BlockType
{

    public string m_BlockName;
    public bool m_IsSolid;
    public byte m_Hardness;
    
    [Header("Texture values")]

    public int m_BackFace;
    public int m_FrontFace;
    public int m_TopFace;
    public int m_BottomFace;
    public int m_LeftFace;
    public int m_RightFace;
   
  
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return m_BackFace;
            case 1:
                return m_FrontFace;
            case 2:
                return m_TopFace;
            case 3:
                return m_BottomFace;
            case 4:
                return m_LeftFace;
            case 5:
                return m_RightFace;
            default:
                Debug.Log("Error , invalid face index");
                return 0;
        }
    }
}