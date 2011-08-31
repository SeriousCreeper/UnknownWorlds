using UnityEngine;
using System.Collections;


public class Chunk : ChunkData 
{
    void Awake()
    {
        myPos = transform.position;

        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshCollider>();

        MeshFilter m_meshFilter = gameObject.GetComponent<MeshFilter>();

        Mesh newMesh = new Mesh();
        m_meshFilter.mesh = newMesh;

        renderer.material.shader = Shader.Find("Vertex Color Unlit");

        renderer.material.mainTexture = TerrainBrain.Instance().getGroundTexture(); //groundTexture;

        this.enabled = true;
    }


    public void RegenerateChunk()
    {
        Mesh subMesh = gameObject.GetComponent<MeshFilter>().mesh;
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();

        transform.position = myPos;

        subMesh.Clear();

        subMesh.vertices = verts.ToArray();
        subMesh.triangles = indices.ToArray();
        subMesh.uv = uvs.ToArray();
        subMesh.colors = colors.ToArray();

        subMesh.RecalculateNormals();

        meshCollider.sharedMesh = new Mesh();
        meshCollider.sharedMesh = subMesh;
    }
	
	public void RelightChunk()
    {
        gameObject.GetComponent<MeshFilter>().mesh.colors = colors.ToArray();

        //subMesh.RecalculateNormals();
    }
}
