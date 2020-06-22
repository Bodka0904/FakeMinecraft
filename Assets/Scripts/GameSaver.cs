using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GameSaver
{
    public World m_World;

    public GameSaver(World world)
    {
        m_World = world;
        RestoreGame();
    }

    // Update is called once per frame
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SaveGame();
        }
    }

    void RestoreGame()
    {
        WorldSave save = new WorldSave();

        if (System.IO.File.Exists("data.dat"))
        {
            byte[] data = File.ReadAllBytes("data.dat");
            Debug.Log(data.Length);
            if (data.Length > 0)
            {
                save = ObjectSerializationExtension.Deserialize<WorldSave>(data);
                int index = 0;
                for (int i = 0; i < VoxelData.m_WorldSizeInChunks; ++i)
                {
                    for (int j = 0; j < VoxelData.m_WorldSizeInChunks; ++j)
                    {
                        if (save.m_Chunks[index] != null)
                        {
                            if (m_World.m_Chunks[i, j] == null)
                                m_World.m_Chunks[i, j] = new Chunk(save.m_Chunks[index].m_Coord, m_World, true);

                            int voxelIndex = 0;
                            for (int k = 0; k < VoxelData.m_ChunkWidth; ++k)
                            {
                                for (int d = 0; d < VoxelData.m_ChunkHeight; ++d)
                                {
                                    for (int c = 0; c < VoxelData.m_ChunkWidth; ++c)
                                    {
                                        m_World.m_Chunks[i, j].m_VoxelMap[k, d, c] = save.m_Chunks[index].m_VoxelMap[voxelIndex];

                                        voxelIndex++;
                                    }
                                }
                            }
                            m_World.m_Chunks[i, j].UpdateMeshData();
                            m_World.m_Chunks[i, j].m_Coord = save.m_Chunks[index].m_Coord;
                        }
                        index++;
                    }
                }
            }
        }
    }

    void SaveGame()
    {
        WorldSave save = new WorldSave();
        save.m_Seed = m_World.m_Seed;
        save.m_BlockTypes = m_World.m_BlockTypes;

        int index = 0;
        for (int i = 0; i < VoxelData.m_WorldSizeInChunks; ++i)
        {
            for (int j = 0; j < VoxelData.m_WorldSizeInChunks;++j)
            {
                if (m_World.m_Chunks[i, j] != null)
                {
                    save.m_Chunks[index] = new ChunkSave();
                    int voxelIndex = 0;
                    for (int k = 0; k < VoxelData.m_ChunkWidth; ++k)
                    {
                        for (int d = 0; d < VoxelData.m_ChunkHeight; ++d)
                        {
                            for (int c = 0; c < VoxelData.m_ChunkWidth; ++c)
                            {
                                
                                save.m_Chunks[index].m_VoxelMap[voxelIndex] = m_World.m_Chunks[i, j].m_VoxelMap[k, d, c];
                                voxelIndex++;
                            }
                        }
                    }
                    save.m_Chunks[index].m_Coord = m_World.m_Chunks[i, j].m_Coord;
                }
                index++;
            }
        }

        byte[] bytes = ObjectSerializationExtension.SerializeToByteArray(save);
        File.WriteAllBytes("data.dat", bytes);
    }
}


public static class ObjectSerializationExtension
{

    public static byte[] SerializeToByteArray(this object obj)
    {
        if (obj == null)
        {
            return null;
        }
        var bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static T Deserialize<T>(this byte[] byteArray) where T : class
    {
        if (byteArray == null)
        {
            return null;
        }
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = (T)binForm.Deserialize(memStream);
            return obj;
        }
    }
}
