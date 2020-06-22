using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    public static readonly int m_ChunkWidth = 16;
    public static readonly int m_ChunkHeight = 128;
    public static readonly int m_WorldSizeInChunks = 100;

    public static int m_WorldSizeInVoxels
    {
        get { return m_ChunkWidth * m_WorldSizeInChunks; }
    }

    public static readonly int m_ViewDistanceInChunks = 2;
   
    public static readonly int m_TextureAtlasSize = 4;

    public static float m_NormalizedBlockTextureSize
    {
        get { return 1f / (float)m_TextureAtlasSize; }
    }

    public static readonly Vector3[] VoxelVertices = new Vector3[8]
    {
        new Vector3(0.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f),
        new Vector3(1.0f,1.0f,0.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(1.0f,0.0f,1.0f),
        new Vector3(1.0f,1.0f,1.0f),
        new Vector3(0.0f,1.0f,1.0f),

    };

    public static readonly Vector3[] FaceCheck = new Vector3[6]
    {
        new Vector3(0.0f,0.0f,-1.0f),
        new Vector3(0.0f,0.0f,1.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,-1.0f,0.0f),
        new Vector3(-1.0f,0.0f,0.0f),
        new Vector3(1.0f,0.0f,0.0f),

    };


    public static readonly int[,] VoxelTris = new int[6, 4]
    {
        {0,3,1,2 }, // Back
        {5,6,4,7 }, // Front
        {3,7,2,6 }, // Top
        {1,5,0,4 }, // Bottom
        {4,7,0,3 }, // Left
        {1,2,5,6 } // Right

    };

    public static readonly Vector2[] VoxelUvs = new Vector2[4]
    {
        new Vector2(0.0f,0.0f),
        new Vector2(0.0f,1.0f),
        new Vector2(1.0f,0.0f),
        new Vector2(1.0f,1.0f),
    };
}
