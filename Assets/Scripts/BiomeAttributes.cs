using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BiomeAttributes",menuName = "Minecraft/BiomeAttribute")]
[System.Serializable]
public class BiomeAttributes : ScriptableObject
{
    public string m_BiomeName;
    public int m_SolidGroundHeight;
    public int m_TerrainHeight;
    public float m_TerrainScale;
}

