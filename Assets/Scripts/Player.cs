using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour
{
    public bool m_IsGrounded;
    public float m_Speed = 3f;
    public float m_JumpForce = 5f;
    public float m_Gravity = -9.8f;

    public float m_PlayerWidth = 0.15f;
    public float m_PlayerHeight = 1.8f;

    private Transform m_Camera;
    private World m_World;

    private float m_Horizontal;
    private float m_Vertical;
    private float m_MouseHorizontal;
    private float m_MouseVertical;
    private Vector3 m_Velocity;
    private float m_VerticalMomentum = 0;
    private bool m_Jump;

    public Transform m_HighlightBlock;
    public Transform m_PlaceBlock;
    public GameObject m_PlaceBlockObject;

    Mesh m_PlaceBlockMesh;
    public float m_CheckIncrement = 0.1f;
    public float m_Reach = 8f;
    public byte m_SelectedBlock = (byte)Type.Grass;

    private void Start()
    {
        m_Camera = GameObject.Find("Main Camera").transform;
        m_World = GameObject.Find("World").GetComponent<World>();
        m_PlaceBlockMesh = m_PlaceBlockObject.GetComponent<MeshFilter>().mesh;
        InitHighlightBlock();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandlePlayerInputs();

        PlaceCursorBlocks();
    }
    private void FixedUpdate()
    {     
        CalculateVelocity();
        if (m_Jump)
            Jump();

        transform.Rotate(Vector3.up * m_MouseHorizontal);
        m_Camera.Rotate(Vector3.right * -m_MouseVertical);
        transform.Translate(m_Velocity, Space.World);
    }
    void Jump()
    {
        m_VerticalMomentum = m_JumpForce;
        m_IsGrounded = false;
        m_Jump = false;
    }
    private void CalculateVelocity()
    {
        //Affect vertical momentum with gravity
        if (m_VerticalMomentum > m_Gravity)
        {
            m_VerticalMomentum += Time.fixedDeltaTime * m_Gravity;
        }
        // Handle walking
        m_Velocity = ((transform.forward * m_Vertical) + (transform.right * m_Horizontal)) * Time.fixedDeltaTime * m_Speed;

        // Apply vertical momentum
        m_Velocity += Vector3.up * m_VerticalMomentum * Time.fixedDeltaTime;

        if ((m_Velocity.z > 0 && Front) || (m_Velocity.z < 0 && Back))
            m_Velocity.z = 0;
        if ((m_Velocity.x > 0 && Right) || (m_Velocity.x < 0 && Left))
            m_Velocity.x = 0;
        if (m_Velocity.y < 0)
            m_Velocity.y = CheckDownSpeed(m_Velocity.y);
        else if (m_Velocity.y > 0)
            m_Velocity.y = CheckUpSpeed(m_Velocity.y);
    }

    private void HandlePlayerInputs()
    {
        m_Horizontal = Input.GetAxis("Horizontal");
        m_Vertical = Input.GetAxis("Vertical");
        m_MouseHorizontal = Input.GetAxis("Mouse X");
        m_MouseVertical = Input.GetAxis("Mouse Y");

        if (m_IsGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            m_Jump = true;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0)
        {
            if (scroll > 0)
            {
                m_SelectedBlock++;
                if (m_SelectedBlock == (byte)Type.NumTypes)
                    m_SelectedBlock = (byte)Type.Grass;
            }
            else
            {
                m_SelectedBlock--;
                if(m_SelectedBlock == (byte)Type.Air)
                    m_SelectedBlock = (byte)Type.Dirt;
            }

            SetPlaceBlockTexture();
        }

        if (m_HighlightBlock.gameObject.activeSelf)
        {
            // Destroy block
            if (Input.GetMouseButton(0))
            {
                m_World.GetChunkFromPosition(m_HighlightBlock.position).DamageVoxel(m_HighlightBlock.position);
            }
            
            // Place block
            else if (Input.GetMouseButton(1))
                m_World.GetChunkFromPosition(m_PlaceBlock.position).EditVoxel(m_PlaceBlock.position, m_SelectedBlock);
        }
    }


    private void InitHighlightBlock()
    {
        int m_VertexIndex = 0;
        
        List<Vector3> m_Vertices = new List<Vector3>();
        List<int> m_Triangles = new List<int>();
        for (int p = 0; p < 6; ++p)
        {
            m_Vertices.Add(VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 0]]);
            m_Vertices.Add(VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 1]]);
            m_Vertices.Add(VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 2]]);
            m_Vertices.Add(VoxelData.VoxelVertices[VoxelData.VoxelTris[p, 3]]);        

            m_Triangles.Add(m_VertexIndex);
            m_Triangles.Add(m_VertexIndex + 1);
            m_Triangles.Add(m_VertexIndex + 2);
            m_Triangles.Add(m_VertexIndex + 2);
            m_Triangles.Add(m_VertexIndex + 1);
            m_Triangles.Add(m_VertexIndex + 3);
            m_VertexIndex += 4;
        }

        SetPlaceBlockTexture();
        m_PlaceBlockMesh.triangles = m_Triangles.ToArray();
        m_PlaceBlockMesh.vertices = m_Vertices.ToArray();      
    }


    private void SetPlaceBlockTexture()
    {
        List<Vector2> m_UVS = new List<Vector2>();

        for (int p = 0; p < 6; ++p)
        {
            int textureID = m_World.m_BlockTypes[m_SelectedBlock].GetTextureID(p);
            float y = textureID / VoxelData.m_TextureAtlasSize;
            float x = textureID - (y * VoxelData.m_TextureAtlasSize);

            x *= VoxelData.m_NormalizedBlockTextureSize;
            y *= VoxelData.m_NormalizedBlockTextureSize;


            m_UVS.Add(new Vector2(x, y));
            m_UVS.Add(new Vector2(x, y + VoxelData.m_NormalizedBlockTextureSize));
            m_UVS.Add(new Vector2(x + VoxelData.m_NormalizedBlockTextureSize, y));
            m_UVS.Add(new Vector2(x + VoxelData.m_NormalizedBlockTextureSize, y + VoxelData.m_NormalizedBlockTextureSize));
        }
        m_PlaceBlockMesh.uv = m_UVS.ToArray();
        m_PlaceBlockMesh.RecalculateNormals();
    }

    private void PlaceCursorBlocks()
    {
        float step = m_CheckIncrement;
        Vector3 lastPos = new Vector3();

        while (step < m_Reach)
        {
            Vector3 pos = m_Camera.position + (m_Camera.forward * step);
            if (m_World.CheckForVoxel(pos))
            {
                m_HighlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                m_PlaceBlock.position = lastPos;
                m_HighlightBlock.gameObject.SetActive(true);
                m_PlaceBlock.gameObject.SetActive(true);

                return;
            }
            lastPos.x = Mathf.FloorToInt(pos.x);
            lastPos.y = Mathf.FloorToInt(pos.y);
            lastPos.z = Mathf.FloorToInt(pos.z);

            step += m_CheckIncrement;
        }

        m_HighlightBlock.gameObject.SetActive(false);
        m_PlaceBlock.gameObject.SetActive(false);
    }

    private float CheckDownSpeed(float speed)
    {
        if (
            m_World.CheckForVoxel(new Vector3(transform.position.x - m_PlayerWidth, transform.position.y + speed, transform.position.z - m_PlayerWidth))
         || m_World.CheckForVoxel(new Vector3(transform.position.x + m_PlayerWidth, transform.position.y + speed, transform.position.z - m_PlayerWidth))
         || m_World.CheckForVoxel(new Vector3(transform.position.x + m_PlayerWidth, transform.position.y + speed, transform.position.z + m_PlayerWidth))
         || m_World.CheckForVoxel(new Vector3(transform.position.x - m_PlayerWidth, transform.position.y + speed, transform.position.z + m_PlayerWidth))
            )
        {
            m_IsGrounded = true;
            return 0;
        }
        else
        {
            m_IsGrounded = false;
            return speed;
        }
    }

    private float CheckUpSpeed(float speed)
    {
        if (
            m_World.CheckForVoxel(new Vector3(transform.position.x - m_PlayerWidth, transform.position.y + m_PlayerHeight +0.2f + speed, transform.position.z - m_PlayerWidth))
         || m_World.CheckForVoxel(new Vector3(transform.position.x + m_PlayerWidth, transform.position.y + m_PlayerHeight +0.2f + speed, transform.position.z - m_PlayerWidth))
         || m_World.CheckForVoxel(new Vector3(transform.position.x + m_PlayerWidth, transform.position.y + m_PlayerHeight +0.2f + speed, transform.position.z + m_PlayerWidth))
         || m_World.CheckForVoxel(new Vector3(transform.position.x - m_PlayerWidth, transform.position.y + m_PlayerHeight +0.2f + speed, transform.position.z + m_PlayerWidth))
            )
        {
            return 0;
        }
        else
        {
            return speed;
        }
    }

    public bool Front
    {
        get
        {
            if (
                m_World.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + m_PlayerWidth)) 
               ||
                m_World.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + m_PlayerWidth))
                )
                return true;
            else
                return false;
        }
    }
    public bool Back
    {
        get
        {
            if (
                m_World.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - m_PlayerWidth)) ||
                m_World.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - m_PlayerWidth))
                )
                return true;
            else
                return false;
        }
    }
    public bool Left
    {
        get
        {
            if (
                m_World.CheckForVoxel(new Vector3(transform.position.x - m_PlayerWidth, transform.position.y, transform.position.z)) ||
                m_World.CheckForVoxel(new Vector3(transform.position.x - m_PlayerWidth, transform.position.y + 1f, transform.position.z))
                )
                return true;
            else
                return false;
        }
    }
    public bool Right
    {
        get
        {
            if (
                m_World.CheckForVoxel(new Vector3(transform.position.x + m_PlayerWidth, transform.position.y, transform.position.z)) ||
                m_World.CheckForVoxel(new Vector3(transform.position.x + m_PlayerWidth, transform.position.y + 1f, transform.position.z))
                )
                return true;
            else
                return false;
        }
    }
}
