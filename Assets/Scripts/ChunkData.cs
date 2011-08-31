using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ChunkData : MonoBehaviour 
{
    private byte[, ,] _blocks;
    private List<int> _indices;
    private List<Vector2> _uvs;
    private List<Vector3> _verts;
    private List<Color> _colors;
    private Vector3 _myPos;

    public List<int> indices
    {
        get { return _indices; }
        set { _indices = value; }
    }

    public List<Vector2> uvs
    {
        get { return _uvs; }
        set { _uvs = value; }
    }

    public List<Vector3> verts
    {
        get { return _verts; }
        set { _verts = value; }
    }

    public List<Color> colors
    {
        get { return _colors; }
        set { _colors = value; }
    }

    public Vector3 myPos
    {
        get { return _myPos; }
        set { _myPos = value; }
    }

    public IntCoords myChunkPos
    {
        get { return new IntCoords((int)_myPos.x, 0, (int)_myPos.z); }
    }

    public byte[, ,] blocks
    {
        get { return _blocks; }
        set { _blocks = value; }
    }
}
