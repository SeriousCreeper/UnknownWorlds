using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;



class Light
{
	public byte strength;
	public Vector3 color;
	public Vector3 deltaColor
	{
		get
		{
			return color * ((float)strength / 16f);
		}
	}
	
	public Light(Vector3 _color, byte _strength)
	{
		strength = _strength;
		color = _color;
	}
}


class ChunkInfo
{
	public int x;
	public int z;
	public byte[,,] data = new byte[16, 128, 16];
}


public struct IntCoords : IEquatable<IntCoords>
{
    int X;
    int Y;
    int Z;

    public int x { get { return X; } }
    public int y { get { return Y; } }
    public int z { get { return Z; } }

    public IntCoords(int x, int y, int z)
    {
        X = x; Y = y; Z = z;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() * 27644437 ^ Z.GetHashCode() * 1073676287;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        return Equals((IntCoords)obj);
    }

    public bool Equals(IntCoords other)
    {
        return other.X.Equals(X) && other.Y.Equals(Y) && other.Z.Equals(Z);
    }

    public override string ToString()
    {
        return "" + X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }
}

class TerrainCache
{
    // Caches the density for terrain blocks, will also store updates eventually
    //Dictionary<Vector3, int[,,]> m_data = new Dictionary<Vector3,int[,,]>();
    //public Dictionary<IntCoords, byte[, ,]> m_data = new Dictionary<IntCoords, byte[, ,]>();
	public byte[,,] m_data = new byte[640, 128, 640];
	public List<ChunkInfo> m_changed = new List<ChunkInfo>();
	public List<Vector2> generatedChunks = new List<Vector2>();
    Dictionary<IntCoords, byte[, ,]> changedVoxels = new Dictionary<IntCoords, byte[, ,]>();
	
    public Light[,,] lightData = new Light[640, 128, 640];

    IntCoords m_lastCoords;
    int[, ,] m_lastData;
    /*
    public int[,,] getChunk(int x, int y, int z)
    {
        IntCoords coords = new IntCoords(x, y, z);
        if (coords.Equals(m_lastCoords))
            return m_lastData;

        m_lastCoords = coords;

        if (m_data.ContainsKey(coords))
        {
            m_lastData = m_data[coords];
            return m_lastData;
        }

        IntCoords chunkSize = TerrainBrain.chunkSize;

        m_lastData = new int[chunkSize.x, chunkSize.y, chunkSize.z];

        TerrainBrain tb = TerrainBrain.Instance();

        for (int t = 0; t < chunkSize.x; t++)
        {
            for (int u = 0; u < chunkSize.y; u++)
            {
                for (int v = 0; v < chunkSize.z; v++)
                {
                    Vector3 loc = new Vector3((float)(x+t), (float)(y+u), (float)(z+v)) / TerrainBrain.noiseMultiplier;

                    m_lastData[t, u, v] = tb.getDensity(loc);
                }
            }
        }

        m_data[coords] = m_lastData;
        return m_lastData;
    }
    */
    /*
    public void SaveChangedChunks()
    {
        TextWriter tw = new StreamWriter("data.txt");

        foreach (KeyValuePair<IntCoords, int[, ,]> entry in changedVoxels)
        {
            tw.WriteLine(entry.Key.ToString());

            for(int x = 0; x < entry.Value.GetLength(0); x++)
                for (int y = 0; y < entry.Value.GetLength(1); y++)
                    for (int z = 0; z < entry.Value.GetLength(2); z++)
                        tw.WriteLine(entry.Value[x,y,z]);
        }

        // close the stream
        tw.Close();
    }*/

//	
//    public void LoadChangedChunks()
//    {
//        try
//        {
//            TextReader tw = new StreamReader("data.txt");
//
//            if (tw != null)
//            {
//                string S = tw.ReadLine();
//
//                while (S != null)
//                {
//                    IntCoords newCoords = new IntCoords(int.Parse(S), int.Parse(tw.ReadLine()), int.Parse(tw.ReadLine()));
//                    /*
//                    if (!changedVoxels.ContainsKey(newCoords))
//                        changedVoxels.Add(newCoords, new int[16, 128, 16]);
//
//                    if (!m_data.ContainsKey(newCoords))
//                        m_data.Add(newCoords, new int[16, 128, 16]);
//                    */
//                    for (int x = 0; x < 16; x++)
//                    {
//                        for (int y = 0; y < 128; y++)
//                        {
//                            for (int z = 0; z < 16; z++)
//                            {
//                                int blockType = int.Parse(tw.ReadLine());
//
//                                if(blockType != -1)
//                                {
//                                    m_data[newCoords][x, y, z] = (byte)blockType;
//
//                                    Debug.Log(m_data[newCoords][x, y, z]);
//                                }
//
//                                changedVoxels[newCoords][x, y, z] = (byte)blockType;
//                            }
//                        }
//                    }
//
//                    S = tw.ReadLine();
//                }
//            }
//
//            // close the stream
//            tw.Close();
//        }
//        catch
//        {
//        }
//    }
//
//
//    public void setDensity(int x, int y, int z, int blockType)
//    {
//        IntCoords chunkSize = TerrainBrain.chunkSize;
//
//        int absX = Math.Abs(x);
//        int absY = Math.Abs(y);
//        int absZ = Math.Abs(z);
//
//        int modX = x < 0 ? (chunkSize.x - absX % chunkSize.x) % chunkSize.x : x % chunkSize.x;
//        int modY = y < 0 ? (chunkSize.y - absY % chunkSize.y) % chunkSize.y : y % chunkSize.y;
//        int modZ = z < 0 ? (chunkSize.z - absZ % chunkSize.z) % chunkSize.z : z % chunkSize.z;
//
//        int ciX = x - modX;
//        int ciY = y - modY;
//        int ciZ = z - modZ;
//
//        IntCoords coords = new IntCoords(ciX, ciY, ciZ);
//
//        // Shouldn't be altering the density of unloaded chunks anyway, eh?
//        if (!m_data.ContainsKey(coords))
//        {
//            Debug.Log("Couldn't find coords: " + coords.ToString());
//            return;
//        }
//
//        //Debug.Log("Setting chunk " + coords.ToString() + ", index (" + modX.ToString() + ", " + modY.ToString() + ", " + modZ.ToString() + ") to nil.");
//        m_data[coords][modX, modY, modZ] = (byte)blockType;
//
//        /*
//        if (!changedVoxels.ContainsKey(coords))
//        {
//            changedVoxels.Add(coords, new int[16, 16, 16]);
//
//            for (int xx = 0; xx < 16; xx++)
//                for (int yy = 0; yy < 16; yy++)
//                    for (int zz = 0; zz < 16; zz++)
//                        changedVoxels[coords][xx, yy, zz] = -1;
//        }
//
//        changedVoxels[coords][modX, modY, modZ] = blockType;
//        
//        SaveChangedChunks();
//        */
//        Debug.Log("Dictionary Size: " + m_data.Count);
//    }
//
}


public enum BlockTypes
{
    EMPTY = 0,
    STONE = 2,
    DIRT = 3,
    WOOD = 5,
    FLATSTONE = 7,
    WALLSTONE1 = 8,
    WALLSTONE2 = 17,
    ADMINIUM = 18,
    SAND = 19,
    GRAVEL = 20
}



public class TerrainBrain : MonoBehaviour 
{
    private byte[] blockTypeArray;
    public byte currentBlockType;
    private byte selectedBlock = 0;
    private Light lastLightValue;

    private static bool EndProcessingThreads = false;

	public GameObject prefab;
	public static TerrainBrain m_instance;

    public static float noiseMultiplier = 50f;
    //public static int chunkSize = 16;
    public static IntCoords chunkSize = new IntCoords(16, 128, 16);

    Vector3 playerStart = new Vector3(0.5f, 60.5f, 0.5f);
    TerrainCache m_tcache = new TerrainCache();

	public Texture2D[] textures;
	public Texture2D atlasMap;
	
	Texture2D groundTexture;
	Rect[] groundUVs;
	
	Vector3 currentPos = Vector3.zero;
	Vector3 lastPos = Vector3.zero;
	
	// Distance in blocks to load terrain
	float viewDistance = 60; // 140
	int blockLoadDistance = 0;

    Queue<ChunkData> chunksToProcess = new Queue<ChunkData>();
    Queue<ChunkData> chunksToCreate = new Queue<ChunkData>();
	Queue<ChunkData> chunksToLightUp = new Queue<ChunkData>();
	
	public bool fastLighting = true;
	
	
	
	//Queue<GameObject> m_freePool = new Queue<GameObject>();
	Queue<Vector3> m_terrainToCreate = new Queue<Vector3>();
	//float loadDelay = 0.02f;
	//float lastTerrainUpdate = 0.0f;
	
	GameObject[,,] m_meshCache;
	
	public GUIText distText;
	Vector3 scanStart = new Vector3(0, 100, 0);

    private Transform chunkManager;
    private Transform playerPtr;


	public static TerrainBrain Instance()
	{
		if (m_instance == null)
		{
			Debug.LogWarning("Lost terrain brain, reaquiring.");
			GameObject ob = GameObject.Find("TerrainManager");
			if (ob == null)
			{
				Debug.LogError("Could not reaquire terrain brain.");
				return null;
			}
			
			m_instance = ob.GetComponent<TerrainBrain>();
			if (m_instance == null)
			{
				Debug.LogError("Could not reaquire terrain brain component.");
				return null;
			}
		}
		
		return m_instance;
	}
	
	public Texture2D getGroundTexture() { return atlasMap; }
	public Rect[] getUVs() { return groundUVs; }

    int m_loaded = 5;


    //Dictionary<IntCoords, int[, ,]> mapData = new Dictionary<IntCoords, int[, ,]>();
    public IntCoords nextCoords, lastCoords;
    public byte[, ,] lastData;
    bool gettingMapData = false;
    bool startLoad = true;



    void OnApplicationQuit()
    {
        EndProcessingThreads = true;
		getMapThread.Abort();
		createMapThread.Abort();
		generateMesh.Abort();
//		lightUpMeshThread.Abort();
        EndProcessingThreads = true;
    }
	
	
	public void PlaceBlock(int x, int y, int z)
	{
        int absX = Math.Abs(x);
        int absY = Math.Abs(y);
        int absZ = Math.Abs(z);

        int modX = x < 0 ? (chunkSize.x - absX % chunkSize.x) % chunkSize.x : x % chunkSize.x;
        int modY = y < 0 ? (chunkSize.y - absY % chunkSize.y) % chunkSize.y : y % chunkSize.y;
        int modZ = z < 0 ? (chunkSize.z - absZ % chunkSize.z) % chunkSize.z : z % chunkSize.z;

        int ciX = x - modX;
        int ciY = 0;
        int ciZ = z - modZ;

//        m_tcache.m_data[new IntCoords(ciX, ciY, ciZ)][modX, modY + 1, modZ] = 2;
//		m_tcache.lightData[new IntCoords(ciX, ciY, ciZ)][modX, modY + 1, modZ] = new Light(new Vector3(0.3f, 0.3f, 1f), 20);
		
//		SetLightBlock(ciX + modX, ciY + modY + 1, ciZ + modZ, m_tcache.lightData[new IntCoords(ciX, ciY, ciZ)][modX, modY + 1, modZ]);
		chunksToCreate.Enqueue(GetChunkOnPos(ciX, 0, ciZ));
		
		for(int i = -1; i <= 1; i++)
		{
			for(int j = -1; j <= 1; j++)
			{
				if(i == 0 && j == 0)
					continue;
				
				if(modX + i < 0 || modX + i > 15 || modZ + j < 0 || modZ + j > 15)
				{
					enqueueChunk(GetChunkOnPos(ciX + i * chunkSize.x, 0, ciZ + j * chunkSize.x));
				}
			}
		}
	}
	
	
	public void RemoveBlock(int x, int y, int z)
	{
        int absX = Math.Abs(x);
        int absY = Math.Abs(y);
        int absZ = Math.Abs(z);

        int modX = x < 0 ? (chunkSize.x - absX % chunkSize.x) % chunkSize.x : x % chunkSize.x;
        int modY = y < 0 ? (chunkSize.y - absY % chunkSize.y) % chunkSize.y : y % chunkSize.y;
        int modZ = z < 0 ? (chunkSize.z - absZ % chunkSize.z) % chunkSize.z : z % chunkSize.z;

        int ciX = x - modX;
        int ciY = 0;
        int ciZ = z - modZ;
		
		int[] newPos = getBlockPos(x, y, z);

        m_tcache.m_data[newPos[0], y, newPos[2]] = 0;
		
		chunksToCreate.Enqueue(GetChunkOnPos(ciX, 0, ciZ));
		
		for(int i = -1; i <= 1; i++)
		{
			for(int j = -1; j <= 1; j++)
			{
				if(i == 0 && j == 0)
					continue;
				
				if(modX + i < 0 || modX + i > 15 || modZ + j < 0 || modZ + j > 15)
				{
					enqueueChunk(GetChunkOnPos(ciX + i * chunkSize.x, 0, ciZ + j * chunkSize.x));
				}
			}
		}
	}
	

    public byte getDensity(int x, int y, int z)
    {
		return GetDensity(new Vector3(x, y, z));
		/*
        IntCoords chunkSize = TerrainBrain.chunkSize;

        int absX = Math.Abs(x);
        int absY = Math.Abs(y);
        int absZ = Math.Abs(z);

        int modX = x < 0 ? (chunkSize.x - absX % chunkSize.x) % chunkSize.x : x % chunkSize.x;
        int modY = y < 0 ? (chunkSize.y - absY % chunkSize.y) % chunkSize.y : y % chunkSize.y;
        int modZ = z < 0 ? (chunkSize.z - absZ % chunkSize.z) % chunkSize.z : z % chunkSize.z;

        int ciX = x - modX;
        int ciY = y - modY;
        int ciZ = z - modZ;

        if (m_tcache.m_data.ContainsKey(new IntCoords(ciX, ciY, ciZ)))
        {
            lastLightValue = m_tcache.lightData[new IntCoords(ciX, ciY, ciZ)][modX, modY, modZ];
            return m_tcache.m_data[new IntCoords(ciX, ciY, ciZ)][modX, modY, modZ];
        }
        else
        {
            return 1;
        }
        */
    }


	
	// TODO: -THOUGHTS-
	// - Is it an endless loop? If the array is circular, it could wrap around endlessly when applying light..
	// - Things become weird when i use Return, does it break the whole thread?
	
	
	// Calculate light for block. x = chunkPos.x + blockPos.x, ...
    void SetLightBlock(int x, int y, int z, Light light)
    {
		//Debug.Log("0");
		// Check if the light is bright enough to continue
		if(light.strength - 2 > 2)
		{
			int[] newPos = getBlockPos(x, y, z);
			x = newPos[0];
			y = newPos[1];
			z = newPos[2];
			
			if(light.strength != 16)
			{
				if(GetLightBlock(x, y, z).strength >= light.strength)
					return;
			}	
			
			// Check if this block is AIR or not
			if (GetDensity(x, y, z) == 0)
			{
				//Debug.Log("3");
				// Calculate next light value with a lower strength
			  	Light nextLight = new Light(light.color, (byte)(light.strength - 2));
				
				// Apply light to our light dictionary 
				m_tcache.lightData[x, y, z] = light;
		
				if(x > 16 && x < 624 && z > 16 && z < 624)
				{
					Debug.Log("4");
					// Spread lower light to neighbours
		            SetLightBlock(x + 1, y, z, nextLight);
		            SetLightBlock(x - 1, y, z, nextLight);
		
		            SetLightBlock(x, y, z + 1, nextLight);
		            SetLightBlock(x, y, z - 1, nextLight);
		
		           	SetLightBlock(x, y + 1, z, nextLight);
		           	SetLightBlock(x, y - 1, z, nextLight);
				}
			}
		}
    }



    Light GetLightBlock(int x, int y, int z)
    {
			return m_tcache.lightData[x, y, z];
		try {
			return m_tcache.lightData[x, y, z];
		} catch {
			return new Light(new Vector3(0.5f, 0.5f, 0.5f), 16);
			int chunkX = (x - (x % 16)) / 16; 
			int chunkZ = (z - (z % 16)) / 16; 
			
			ApplySunlight(chunkX, chunkZ);
			
			return m_tcache.lightData[x, y, z];
		}
    }


    public void AddChunkToQueue(Vector3 pos)
    {
        pos.x = (pos.x - (pos.x % 16)) / 16;
        pos.z = (pos.z - (pos.z % 16)) / 16;

        m_terrainToCreate.Enqueue(new Vector3(pos.x, 0, pos.z));
    }

    
    void addUV(int density, ref List<Vector2> uvs)
    {
        density--;

        float tileSize = 1 / 16f;
        float x = density;
        float y = 16f;

        while (x >= 16)
        {
            x -= 16;
            y--;
        }

        Vector2 leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
        Vector2 leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
        Vector2 rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
        Vector2 rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));

        uvs.Add(leftBottom);
        uvs.Add(rightTop);
        uvs.Add(rightBottom);
        uvs.Add(leftTop);
    }


    private byte[] GetBlockID(Vector3 blockPos, ChunkData chunk, Vector3 myPos, int blockType)
    {
        /*
        0 = back
        1 = front
        2 = bottom
        3 = top
        4 = left
        5 = right
        */

        byte[] faceIDs = new byte[6];

        for (int i = 0; i < 6; i++)
            faceIDs[i] = (byte)blockType;

        switch (blockType)
        {
            // Dirt
            case 1:
                for (int i = 0; i < 6; i++)
                    faceIDs[i] = 3;

                if (GetDensity(blockPos) == 0)
                {
                    // if yes, apply meadow on top
                    faceIDs[3] = 1;

                    // and meadow on the first row
                    faceIDs[0] = 4;
                    faceIDs[1] = 4;
                    faceIDs[4] = 4;
                    faceIDs[5] = 4;
                }
                break;


            // Gravel
            case 2:
                for (int i = 0; i < 6; i++)
                    faceIDs[i] = (int)BlockTypes.GRAVEL;
                break;


            // Stone
            case 3:
                for (int i = 0; i < 6; i++)
                    faceIDs[i] = (int)BlockTypes.STONE;
                break;


            // Stone 2
            case 4:
                for (int i = 0; i < 6; i++)
                    faceIDs[i] = (int)BlockTypes.STONE;
                break;
        }

        return faceIDs;
    }
	
	
	void SpreadLight(ChunkData chunk)
	{
        ApplySunlight(chunk);
	
		if(!fastLighting)
		{
	        for (int x = 0; x < chunkSize.x; x++)
	        {
	            for (int z = 0; z < chunkSize.z; z++)
	            {
	                for (int y = chunkSize.y - 1; y >= 0; y--)
	                {
						int[] newPos = getBlockPos(x + chunk.myChunkPos.x, y, z + chunk.myChunkPos.z);
						
	                    if (m_tcache.lightData[newPos[0], newPos[1], newPos[2]].strength == 16)
	                    {
							SetLightBlock(newPos[0], newPos[1], newPos[2], m_tcache.lightData[newPos[0], newPos[1], newPos[2]]);
	                    }
	                }
	            }
	        }
		}
	}


    void ApplySunlight(ChunkData chunk)
    {
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int z = 0; z < chunkSize.z; z++)
            {
                for (int y = chunkSize.y - 1; y >= 0; y--)
                {
					int[] newPos = getBlockPos(x + chunk.myChunkPos.x, y, z + chunk.myChunkPos.z);
					
                    if (m_tcache.m_data[newPos[0], newPos[1], newPos[2]] == 0)
                    {
                        m_tcache.lightData[newPos[0], newPos[1], newPos[2]] = new Light(new Vector3(0.5f, 0.5f, 0.5f), 16);
                    }
                    else
                    {
						break;
                    }
                }
            }
        }
    }
	
	
	void ApplySunlight(int chunkX, int chunkZ)
    {
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int z = 0; z < chunkSize.z; z++)
            {
                for (int y = chunkSize.y - 1; y >= 0; y--)
                {
					int[] newPos = getBlockPos(x + chunkX * 16, y, z + chunkZ * 16);
					
                    if (m_tcache.m_data[newPos[0], newPos[1], newPos[2]] == 0)
                    {
                        m_tcache.lightData[newPos[0], newPos[1], newPos[2]] = new Light(new Vector3(0.5f, 0.5f, 0.5f), 16);
                    }
                    else
                    {
						break;
                    }
                }
            }
        }
    }
	
	
	int[] getBlockPos(int x, int y, int z) 
	{
		int bx, bz;
		
	    bx = x % 640;
		
	    if(bx < 0)
	      bx += 640;
	    		
	    bz = z % 640;
				
	    if(bz < 0)
	      bz += 640;
		
		y = y % 128;
		
	    if(y < 0)
	      y += 128;
		
		return new int[]{bx, y, bz};
	}
	
	
	void GetDensity(ChunkData c)
	{
		int x = c.myChunkPos.x / 16;
		int z = c.myChunkPos.z / 16;
		
		if(!m_tcache.generatedChunks.Contains(new Vector2(x, z)))
		{
			m_tcache.generatedChunks.Add(new Vector2(x, z));
			CreateLandscape(x, z);
		}
		
		
		/*
	  	if (m_tcache.m_data.ContainsKey(new IntCoords(c.myChunkPos.x, c.myChunkPos.y, c.myChunkPos.z)))
		{
			return m_tcache.m_data[new IntCoords(c.myChunkPos.x, c.myChunkPos.y, c.myChunkPos.z)];
		}
		else
		{
	        m_tcache.m_data[new IntCoords(c.myChunkPos.x, c.myChunkPos.y, c.myChunkPos.z)] = lastData;
			return lastData;
		}
		*/
		
		//GetDensity(new Vector3(c.myPos.x, c.myPos.y, c.myPos.z));
	}
	
	
	byte GetDensity(int x, int y, int z)
	{
		return GetDensity(new Vector3(x, y, z));
	}
	
	
	// Chunk + Block Pos
    byte GetDensity(Vector3 pos)
    {	
		int x = (int)pos.x;
		int y = (int)pos.y;
		int z = (int)pos.z;
		
		int[] newpos = getBlockPos(x, y, z);
		
		int chunkX = (x - (x % 16)) / 16; 
		int chunkZ = (z - (z % 16)) / 16; 
		
		if(!m_tcache.generatedChunks.Contains(new Vector2(chunkX, chunkZ)))
		{
			//Debug.Log("Block: " + x + " | " + z);
			//Debug.Log("Chunk: " + chunkX + " | " + chunkZ);
	
			m_tcache.generatedChunks.Add(new Vector2(chunkX, chunkZ));
			CreateLandscape(chunkX, chunkZ);
		}
	
		return m_tcache.m_data[newpos[0], newpos[1], newpos[2]];
	
		
		/*
        int absX = Math.Abs(x);
        int absY = Math.Abs(y);
        int absZ = Math.Abs(z);

        int modX = x < 0 ? (chunkSize.x - absX % chunkSize.x) % chunkSize.x : x % chunkSize.x;
        int modY = y < 0 ? (chunkSize.y - absY % chunkSize.y) % chunkSize.y : y % chunkSize.y;
        int modZ = z < 0 ? (chunkSize.z - absZ % chunkSize.z) % chunkSize.z : z % chunkSize.z;

        int ciX = x - modX;
        int ciY = 0;
        int ciZ = z - modZ;
		
        if (m_tcache.m_data.ContainsKey(new IntCoords(ciX, ciY, ciZ)))
        {
			lastLightValue = m_tcache.lightData[new IntCoords(ciX, ciY, ciZ)][modX, modY, modZ];

            return m_tcache.m_data[new IntCoords(ciX, ciY, ciZ)][modX, modY, modZ];
        }
        else
        {
			byte[,,] lastData = CreateLandscape(ciX / 16, ciZ / 16);
			//Light[,,] lightInfo = new Light[chunkSize.x, chunkSize.y, chunkSize.z];
			//Light dLight = new Light(new Vector3(0.5f, 0.5f, 0.5f), 16);
	
			m_tcache.m_data[new IntCoords(ciX, ciY, ciZ)] = lastData;
	       // m_tcache.lightData[new IntCoords(ciX, ciY, ciZ)] = lightInfo;
				
			lastLightValue = new Light(new Vector3(0.5f, 0.5f, 0.5f), 1);
			return lastData[modX, modY, modZ];
        }
        */
    }
	
	
    void ProcessChunks()
    {
        Debug.Log("Start processing Chunks...");
		
		
        ThreadPool.QueueUserWorkItem(state =>
        {
            while (EndProcessingThreads == false)
            {
                while (chunkQueues[0].Count > 0)
                {
					ChunkData chunk = chunkQueues[0].Dequeue();
					
					GetDensity(chunk);
					
					SpreadLight(chunk);
					
					chunksToProcess.Enqueue(chunk);
                }
				
				//Debug.Log("Processed: " + chunks);
            }
			
        });
    }
	
	void CreateChunks()
    {
        ThreadPool.QueueUserWorkItem(state =>
        {
            while (EndProcessingThreads == false)
            {
				while (chunksToProcess.Count > 0 && EndProcessingThreads == false)
                {
                    ChunkData chunk = chunksToProcess.Dequeue();
					
					GenerateMesh(chunk);
                }
            }
        });
    }
	
	
	void LightUpChunks()
    {
        ThreadPool.QueueUserWorkItem(state =>
        {
            while (EndProcessingThreads == false)
            {
				while (chunksToLightUp.Count > 0 && EndProcessingThreads == false)
                {
                    ChunkData chunk = chunksToLightUp.Dequeue();
					
					//SpreadLight(chunk);
					
				    //LightMesh(chunk);
                }
				//Debug.Log("Done");
            }
        });
    }

	
	
	
	
	
	void ProcessUserChunks()
    {
        Debug.Log("Start processing user chunks...");
		
        ThreadPool.QueueUserWorkItem(state =>
        {
            while (EndProcessingThreads == false)
            {
                while (chunksToCreate.Count > 0)
                {
                    ChunkData chunk = chunksToCreate.Dequeue();
					
					GenerateMesh(chunk);
                }
            }
        });
    }
	
	int chunks = 0;
	
	
	
	
    Queue<ChunkData> finishedChunks = new Queue<ChunkData>();
    Queue<ChunkData> refreshLightOnChunks = new Queue<ChunkData>();

    public ChunkData GetAFinishedChunk()
    {
        if (finishedChunks.Count > 0)
        {
            return finishedChunks.Dequeue();
        }

        return null;
    }
	
	
	public ChunkData GetAChunkToLightUp()
    {
        if (refreshLightOnChunks.Count > 0)
        {
            return refreshLightOnChunks.Dequeue();
        }

        return null;
    }


    void GenerateMesh(ChunkData chunk)
    {
        Vector3 offset = chunk.myPos;
	
        int curVert = 0;
        int density = 0;
        byte[] faceIDs;

        List<int> indices = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> verts = new List<Vector3>();
        List<Color> colors = new List<Color>();
        
        indices.Clear();
        verts.Clear();
        uvs.Clear();

        Vector3 curVec = new Vector3(0, 0, 0);

        Vector3 shade;
		
		lastLightValue = new Light(new Vector3(0.5f, 0.5f, 0.5f), 16);
		
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    curVec.x = x;
                    curVec.y = y;
                    curVec.z = z;

                    density = GetDensity(curVec + offset);
					
                    if (density == 0)
                        continue;

                    faceIDs = GetBlockID(new Vector3(x, y + 1, z) + offset, chunk, new Vector3(x, y, z) + offset, density);
					
                    curVec.z--;
					
                    // Back
                    if (GetDensity(curVec + offset) == 0)
                    {
                        verts.Add(new Vector3(x, y, z));
                        verts.Add(new Vector3(x + 1, y + 1, z));
                        verts.Add(new Vector3(x + 1, y, z));
                        verts.Add(new Vector3(x, y + 1, z));

                        addUV(faceIDs[0], ref uvs);
						
                        indices.Add(curVert);
                        indices.Add(curVert + 1);
                        indices.Add(curVert + 2);

                        indices.Add(curVert);
                        indices.Add(curVert + 3);
                        indices.Add(curVert + 1);

                        shade = GetLight(curVec + offset);
						
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
						
                        curVert += 4;
                    }
					
                    curVec.z += 2;

                    // Front
                    if (GetDensity(curVec + offset) == 0)
                    {
                        verts.Add(new Vector3(x, y, z + 1));
                        verts.Add(new Vector3(x + 1, y + 1, z + 1));
                        verts.Add(new Vector3(x + 1, y, z + 1));
                        verts.Add(new Vector3(x, y + 1, z + 1));

                        addUV(faceIDs[1], ref uvs);

                        indices.Add(curVert);
                        indices.Add(curVert + 2);
                        indices.Add(curVert + 1);

                        indices.Add(curVert);
                        indices.Add(curVert + 1);
                        indices.Add(curVert + 3);

                        shade = GetLight(curVec + offset);

                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }

                    curVec.z = z;

                    curVec.y--;
					
                    // Bottom
                    if (GetDensity(curVec + offset) == 0)
                    {
                        verts.Add(new Vector3(x, y, z));
                        verts.Add(new Vector3(x + 1, y, z + 1));
                        verts.Add(new Vector3(x + 1, y, z));
                        verts.Add(new Vector3(x, y, z + 1));

                        addUV(faceIDs[2], ref uvs);

                        indices.Add(curVert);
                        indices.Add(curVert + 2);
                        indices.Add(curVert + 1);

                        indices.Add(curVert);
                        indices.Add(curVert + 1);
                        indices.Add(curVert + 3);

                        shade = GetLight(curVec + offset) * 0.8f;

                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }

                    curVec.y += 2;

                    // Top
                    if (GetDensity(curVec + offset) == 0)
                    {
                        verts.Add(new Vector3(x, y + 1, z));
                        verts.Add(new Vector3(x + 1, y + 1, z + 1));
                        verts.Add(new Vector3(x + 1, y + 1, z));
                        verts.Add(new Vector3(x, y + 1, z + 1));

                        addUV(faceIDs[3], ref uvs);

                        indices.Add(curVert);
                        indices.Add(curVert + 1);
                        indices.Add(curVert + 2);

                        indices.Add(curVert);
                        indices.Add(curVert + 3);
                        indices.Add(curVert + 1);

                        shade = GetLight(curVec + offset) * 1.2f;

                        /*
                        G      B
                         +----+
                         |    |
                         |    |
                         +----+
                        W      R
                        */

                        Vector3 avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, -1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, -1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, -1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, -1)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        curVert += 4;
                    }

                    curVec.y = y;

                    curVec.x--;

                    // Left
                    if (GetDensity(curVec + offset) == 0)
                    {
                        verts.Add(new Vector3(x, y, z + 1));
                        verts.Add(new Vector3(x, y + 1, z));
                        verts.Add(new Vector3(x, y, z));
                        verts.Add(new Vector3(x, y + 1, z + 1));

                        addUV(faceIDs[4], ref uvs);

                        indices.Add(curVert);
                        indices.Add(curVert + 3);
                        indices.Add(curVert + 1);

                        indices.Add(curVert);
                        indices.Add(curVert + 1);
                        indices.Add(curVert + 2);
						
                        shade = GetLight(curVec + offset);

                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }

                    curVec.x += 2;

                    // Right
                    if (GetDensity(curVec + offset) == 0)
                    {
                        verts.Add(new Vector3(x + 1, y, z + 1));
                        verts.Add(new Vector3(x + 1, y + 1, z));
                        verts.Add(new Vector3(x + 1, y, z));
                        verts.Add(new Vector3(x + 1, y + 1, z + 1));

                        addUV(faceIDs[5], ref uvs);

                        indices.Add(curVert);
                        indices.Add(curVert + 1);
                        indices.Add(curVert + 3);

                        indices.Add(curVert);
                        indices.Add(curVert + 2);
                        indices.Add(curVert + 1);
						
                        shade = GetLight(curVec + offset);

                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }
                }
            }
        }
		
		chunk.indices = indices;
        chunk.verts = verts;
        chunk.uvs = uvs;
        chunk.colors = colors;
        //chunk.myPos = offset;
        
		//chunk.GetComponent<Chunk>().RegenerateChunk();
        finishedChunks.Enqueue(chunk);
    }
	
	
	Vector3 GetLight(Vector3 blockPos)
	{
		int[] newPos = getBlockPos((int)blockPos.x, (int)blockPos.y, (int)blockPos.z);
		
		try
		{
        	return m_tcache.lightData[newPos[0], newPos[1], newPos[2]].deltaColor;
		}
		catch
		{
			return new Vector3(0.5f, 0.5f, 0.5f);
		}
	}
	
	
	
	void LightMesh(ChunkData chunk)
    {
        Vector3 offset = chunk.myPos;

        int curVert = 0;
        int density = 0;
        byte[] faceIDs;

        List<Color> colors = new List<Color>();
        
        Vector3 curVec = new Vector3(0, 0, 0);

        Vector3 shade;
		Vector3 lastLight;

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    curVec.x = x;
                    curVec.y = y;
                    curVec.z = z;

                    density = GetDensity(curVec + offset);
					
                    if (density == 0)
                        continue;
					
                    curVec.z--;

                    // Back
                    if (GetDensity(curVec + offset) == 0)
                    {
                        shade = GetLight(chunk.myPos + curVec);

                        Vector3 avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, -1, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, -1, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 1, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 1, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, -1, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, -1, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 1, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 1, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        curVert += 4;
                    }

                    curVec.z += 2;

                    // Front
                    if (GetDensity(curVec + offset) == 0)
                    {
                        shade = GetLight(chunk.myPos + curVec);

                        colors.Add(new Color(1, 0, 0));
                        colors.Add(new Color(0, 1, 0));
                        colors.Add(new Color(0, 0, 1));
                        colors.Add(new Color(1, 1, 1));

                        curVert += 4;
                    }

                    curVec.z = z;

                    curVec.y--;

                    // Bottom
                    if (GetDensity(curVec + offset) == 0)
                    {
                        shade = GetLight(chunk.myPos + curVec) * 0.8f;
                    
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }

                    curVec.y += 2;

                    // Top
                    if (GetDensity(curVec + offset) == 0)
                    {
                        shade = GetLight(chunk.myPos + curVec) * 1.2f;

                        /*
                        G      B
                         +----+
                         |    |
                         |    |
                         +----+
                        W      R
                        */

                        Vector3 avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, -1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, -1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, 0)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(1, 0, -1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, -1)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        avgCol = shade;
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(0, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 1)));
                        avgCol += GetLight(chunk.myPos + (curVec + new Vector3(-1, 0, 0)));
                        avgCol /= 4f;
                        colors.Add(new Color(avgCol.x, avgCol.y, avgCol.z));

                        curVert += 4;
                    }

                    curVec.y = y;

                    curVec.x--;

                    // Left
                    if (GetDensity(curVec + offset) == 0)
                    {
                        shade = GetLight(chunk.myPos + curVec);

                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }

                    curVec.x += 2;

                    // Right
                    if (GetDensity(curVec + offset) == 0)
                    {
                        shade = GetLight(chunk.myPos + curVec);

                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));
                        colors.Add(new Color(shade.x, shade.y, shade.z));

                        curVert += 4;
                    }
                }
            }
        }


        Debug.Log("Light Chunk");   

		
		chunk.colors = colors;
		
        refreshLightOnChunks.Enqueue(chunk);
    }
	
	

    Thread getMapThread, createMapThread, generateMesh, lightUpMeshThread;
	List<Thread> mapThreads = new List<Thread>();
	List<Queue<ChunkData>> chunkQueues = new List<Queue<ChunkData>>();
	
	private int _nextQueueID = 0;
	
	private int nextQueueID
	{
		get {
			_nextQueueID++; 
			_nextQueueID %= 5;
			return _nextQueueID; 
		}
	}
	
	// Use this for initialization
	void Start ()
    {
		//ThreadPool.SetMaxThreads(20, 20);
		noiseButton_Click();
        playerPtr = GameObject.Find("Player").transform;
        chunkManager = new GameObject("ChunkManager").transform;

        blockTypeArray = new byte[Enum.GetValues(typeof(BlockTypes)).Length];

        int pos = 0;

        foreach (BlockTypes type in Enum.GetValues(typeof(BlockTypes)))
        {
            blockTypeArray[pos] = (byte)type;
            Debug.Log("New Block: " + blockTypeArray[pos]);
            pos++;
        }

        currentBlockType = blockTypeArray[0];

        StartCoroutine(UpdateBrain());
        m_instance = this;
		
		//Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
		
		blockLoadDistance = Mathf.CeilToInt((viewDistance - chunkSize.x) / chunkSize.x);
		//Debug.Log("Load distance = " + blockLoadDistance.ToString());
		m_meshCache = new GameObject[blockLoadDistance * 2+1, 1, blockLoadDistance*2+1];
		
		currentPos = playerStart;
		currentPos.x = Mathf.Floor(currentPos.x / chunkSize.x);
		currentPos.y = Mathf.Floor(currentPos.y / chunkSize.y);
		currentPos.z = Mathf.Floor(currentPos.z / chunkSize.z);

		lastPos = currentPos;
		
		//Debug.Log("groundUV size = " + groundUVs.Length.ToString());
		//Debug.Log("Ground UVs = " + groundUVs.ToString());

        //GameObject.Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
        //GameObject.Instantiate(prefab, new Vector3(0, -10, 0), Quaternion.identity);
		
		GUIText status = GameObject.Find("StatusDisplay").GetComponent<GUIText>();
		status.text = "Loading...";
		
		//Camera.main.GetComponent<FPSWalker>().gravity=0;		
		
        //stepLoad();
		
            if (activatePlayer)
            {
                activatePlayer = false;
				Debug.Log("BLA");
                playerPtr.SendMessage("StartGravity");
            }
	}
	
	
	private void enqueueChunk(ChunkData chunk)
	{
		chunkQueues[0].Enqueue(chunk);
	}
	
	
	Chunk GetChunkOnPos(int x, int y, int z)
	{
		foreach(GameObject chunk in m_meshCache)
		{
			if(chunk.transform.position == new Vector3(x, 0, z))
				return chunk.GetComponent<Chunk>();
		}
		
		return null;
	}
	
	
	int[] getCachedChunkPos(int x, int y, int z)
	{
		// returns position in the cache array based on chunk's world location
		int size = blockLoadDistance * 2 + 1;
		
		int[] pos = new int[3];

		pos[0] = x < 0 ? (size - (-x % size)) % size : x % size;
        pos[1] = 0;
		//pos[1] = y < 0 ? (size - (-y % size)) % size : y % size;
		pos[2] = z < 0 ? (size - (-z % size)) % size : z % size;
		
		return pos;
	}
	
    IEnumerator stepLoad()
    {
		//Debug.Log("Step loading from " + currentPos.ToString());
        int startX = Mathf.RoundToInt(currentPos.x) - blockLoadDistance;
        int endX = Mathf.RoundToInt(currentPos.x) + blockLoadDistance;
        int startY = Mathf.RoundToInt(currentPos.y) - blockLoadDistance;
        int endY = Mathf.RoundToInt(currentPos.y) + blockLoadDistance;
        int startZ = Mathf.RoundToInt(currentPos.z)  -blockLoadDistance;
        int endZ = Mathf.RoundToInt(currentPos.z) + blockLoadDistance;
		
		//Debug.Log("Initial Generation from " + new Vector3(startX, startY, startZ).ToString() + " to " + new Vector3(endX, endY, endZ));
        
        GUIText status = GameObject.Find("StatusDisplay").GetComponent<GUIText>();

        status.text = "Generating World...";

        status.text = "Creating Chunks...";


        yield return new WaitForSeconds(1);

        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
               // m_terrainToCreate.Enqueue(new Vector3(x, 0, z));
            }
        }

        yield return null;

		
		List<Chunk> chunkList = new List<Chunk>();
		
        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                //m_terrainToCreate.Enqueue(new Vector3(x,y,z));
                GameObject newObj = GameObject.Instantiate(prefab, new Vector3(x * chunkSize.x, 0, z * chunkSize.z), Quaternion.identity) as GameObject;
                newObj.name = "TerrainChunk (" + x.ToString() + ", 0, " + z.ToString() + ")";
                int[] cachePos = getCachedChunkPos(x, 0, z);
                m_meshCache[cachePos[0], 0, cachePos[2]] = newObj;
                newObj.transform.parent = chunkManager;
				
				chunkList.Add(newObj.GetComponent<Chunk>());
				
                //enqueueChunk(newObj.GetComponent<Chunk>());
            }
        }
		
		
		chunkList.Sort(delegate(Chunk a, Chunk b)
		{
			float distA = Vector3.Distance(a.myPos + new Vector3(8, 0, 8), new Vector3(playerStart.x, 0, playerStart.z));
			float distB = Vector3.Distance(b.myPos + new Vector3(8, 0, 8), new Vector3(playerStart.x, 0, playerStart.z));
			
			if(distA > distB)
				return 1;
			else if(distA < distB)
				return -1;
			else
				return 0;
		});
		
		
		for(int x = 0; x < 640; x++)
			for(int y = 0; y < 128; y++)
				for(int z = 0; z < 640; z++)
					m_tcache.m_data[x, y, z] = 1;
		
		
		foreach(Chunk c in chunkList)
			chunkQueues[0].Enqueue(c);
		
		status.text = "Checking for Level File...";
		
        /*
        m_tcache.LoadChangedChunks();

        status.text = "Applyin changes to world...";

        foreach (GameObject chunk in m_meshCache)
        {
            if(chunk != null)
                chunk.SendMessage("regenerateMesh");
        }
        */
		
        Camera.main.SendMessage("startRunningFPS");

        getMapThread = new Thread(new ThreadStart(ProcessChunks));
        getMapThread.Start();
		
        createMapThread = new Thread(new ThreadStart(ProcessUserChunks));
        createMapThread.Start();
		
        generateMesh = new Thread(new ThreadStart(CreateChunks));
        generateMesh.Start();
		
		lightUpMeshThread = new Thread(new ThreadStart(LightUpChunks));
        lightUpMeshThread.Start();
		
		
		/*
		for(int i = 0; i < 1; i++)
		{
			mapThreads.Add(new Thread(new ThreadStart(ProcessChunks)));
			mapThreads[i].Start();
			
		}
		*/

        /*
        int d = 0;

        while (d == 0)
        {
            scanStart.y--;
            d = GetDensity(scanStart, lastData);
        }

        scanStart.y += 3;
        scanStart.x += 0.5f;
        scanStart.z += 0.5f;
        playerStart = scanStart;
        Debug.Log("Start pos = " + scanStart.ToString());

        playerPtr.position = playerStart;

*/

        yield return null;
    }

	
	private bool UpdateChunk(Vector3 pos)
	{
        bool buildThis = true;

        pos *= 16;
        /*
        if (Vector3.Distance(Camera.main.transform.position, new Vector3(pos.x, pos.y, pos.z)) > 100)
        {
            //chunck is out of draw distance, fuck updating it now, do it later
            buildThis = false;
        }
        else if (Vector3.Dot(Camera.main.transform.forward, Camera.main.transform.position - new Vector3(pos.x, pos.y, pos.z)) > 0)
		{
			//cam not even looking that way yet, fuck it.
			buildThis = false;

			if (Vector3.Distance(Camera.main.transform.position, new Vector3(pos.x, pos.y, pos.z)) < chunkSize)
			{
				//pretty near I guess, the center of the chunck could be behind the cam but the thing still be visible
				//meh, guess we *can* update this or whatever,
				buildThis = true;
			}
		}
        */
        buildThis = true;
        return buildThis;
	}

    GameObject newObj;


    private bool activatePlayer = true;

	IEnumerator UpdateBody()
	{
		yield break;

		while(true)
		{
            // If chunks of terrain are waiting to be loaded, generate whatever can be done in less than 1/60 second
            float curTime = Time.realtimeSinceStartup;
			
            while (m_terrainToCreate.Count > 0)
            {
                Vector3 loc = m_terrainToCreate.Dequeue();
                int[] pos = getCachedChunkPos(Mathf.RoundToInt(loc.x), Mathf.RoundToInt(loc.y), Mathf.RoundToInt(loc.z));

                loc *= chunkSize.x;

                if (m_meshCache[pos[0], 0, pos[2]] == null)
                {
                    newObj = GameObject.Instantiate(prefab, new Vector3(loc.x, 0, loc.z), Quaternion.identity) as GameObject;
                    m_meshCache[pos[0], 0, pos[2]] = newObj;
                    newObj.transform.parent = chunkManager;
                }
                else
                {
                    newObj = m_meshCache[pos[0], 0, pos[2]]; 
                }

                newObj.active = true;
                newObj.name = "TerrainChunk (" + loc.x.ToString() + ", 0, " + loc.z.ToString() + ")";

                Vector3 oldPos = newObj.transform.position;

                nextCoords = new IntCoords((int)loc.x, 0, (int)loc.z);

                newObj.transform.position = new Vector3(loc.x, 0, loc.z);

                //getMapThread = new Thread(new ThreadStart(GetMapThread));
                //getMapThread.Start();
				//createMapThread.Start();

                //generateMesh = new Thread(new ThreadStart(GenerateMesh));
                //generateMesh.Start();

                //while (generateMesh.ThreadState != ThreadState.Stopped)
                //    yield return null;

                //newObj.GetComponent<TerrainPrefabBrain>().regenerateMesh(indices, uvs, verts);
            }

			yield return null;
		}
	}


    ChunkData GetChunk(Vector3 loc)
    {
        ChunkData chunk = null;
		
		//Debug.Log("Need chunk at: " + loc.x + " | " + loc.z);
		
		List<Chunk> chunkList = new List<Chunk>();
		
		foreach (GameObject obj in m_meshCache)
        {
			chunkList.Add(obj.GetComponent<Chunk>());
		}
		
		chunkList.Sort(delegate(Chunk a, Chunk b)
		{
			float distA = Vector3.Distance(a.myPos + new Vector3(8, 0, 8), new Vector3(loc.x * 16, 0, loc.z * 16));
			float distB = Vector3.Distance(b.myPos + new Vector3(8, 0, 8), new Vector3(loc.x * 16, 0, loc.z * 16));
			
			if(distA > distB)
				return -1;
			else if(distA < distB)
				return 1;
			else
				return 0;
		});
		
		chunkList[0].myPos = new Vector3(loc.x * 16, 0, loc.z * 16);
		
		return chunkList[0];
		
        foreach (GameObject obj in m_meshCache)
        {
            int[] newPos = new int[3];
			newPos[0] = (int)obj.transform.position.x / 16;
			newPos[1] = 0;
			newPos[2] = (int)obj.transform.position.z / 16;
			
			Debug.Log("This chunk is at: " + newPos[0] + " | " + newPos[2]);
			//Debug.Log(newPos[0]);

            if (loc == new Vector3(newPos[0], loc.z, newPos[2]))
            {
				Debug.Log("Found Chunk!");
				Destroy(obj);
                //chunk = obj.GetComponent<ChunkData>();
                break;
            }
        }

        return chunk;
    }
	
	
	/*
	
	chunkX = 3
	chunkZ = 3
	
	12345678
	
	12
	34
	vv
	56
	78
	
	
	*/
	
	
	
	private bool ContainsChunk(int x, int z) 
	{
		foreach(ChunkInfo c in m_tcache.m_changed)
		{
			if(c.x == x && c.z == z)
				return true;
		}
		
		return false;
	}
	
	
	
	private void CreateLandscape(int chunkX, int chunkZ)
	{
		//Debug.Log("check");
		if(ContainsChunk(chunkX, chunkZ))
			return;
		//Debug.Log("continue");
		PerlinNoise perlinNoise = new PerlinNoise(12345678);
	
		Light lightInfo = new Light(new Vector3(0.5f, 0.5f, 0.5f), 2);
			
		ChunkNoise noise = new ChunkNoise(seed: 12345);
		
		
		
		/*
		
		      +--+--+--+
		 	 /  /  /  /|
		    +--+--+--+
		   /  /  /  /|  
		  +--+--+--+
		 /  /  /  /|
		+--+--+--+ +
		|  |  |  |/|
		+--+--+--+ +
		|  |  |  |/| 
		+--+--+--+ +
		|  |  |  |/
		+--+--+--+
		
		
		worldSize = 
		{3, 3, 3}
		
		Array =
		{xxxyyyzzz xxxyyyzzz xxxyyyzzz}
		||
		{xyzxyzxyz xyzxyzxyz xyzxyzxyz}
		
		Access = 
		x: (chunkPos.x * worldSize.x) + x
		y: y
		z: (chunkPos.z * worldSize.z) + z
		
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				for (int z = 0; z < 3; z++)
				{
					array[ z + ( (y * (x * 3) ) ) ] = 1;
				}
			}
		}
	
		111 111 ------------------------------------
		
		*/
		
		int absX = (chunkX * 16) % 640;
		if(absX < 0)
			absX += 640;
		
		int absZ = (chunkZ * 16) % 640;
		if(absZ < 0)
			absZ += 640;
	
		// Calculate heightmap
		float[,] heightmap = new float[16, 16];
		noise.FillMap2D(heightmap, chunkX, chunkZ, octaves: 6, startFrequency: .1f, startAmplitude: 5);
		
		ChunkInfo chunkInfo = new ChunkInfo();
		chunkInfo.x = chunkX;
		chunkInfo.z = chunkZ;
		
		// Fill chunk with blocks
		for (int localX = 0; localX < 16; localX++)
		{
			for (int localY = 0; localY < 128; localY++)
			{
				for (int localZ = 0; localZ < 16; localZ++)
				{
					// Create ground
					int height = Mathf.RoundToInt(40 + (heightmap[localX, localZ] * 3f));
					
					lightInfo.strength = 1;
					
					if(localY < height)
					{
						if(localY < 40 && localY >= 30)
							m_tcache.m_data[localX + absX, localY, localZ + absZ] = (byte)BlockTypes.SAND;
						else if(localY < 30)
							m_tcache.m_data[localX + absX, localY, localZ + absZ] = 3;
						else
							m_tcache.m_data[localX + absX, localY, localZ + absZ] = 1;
					}
					else
					{
						// Create mountains1
						int worldX = localX + chunkX * 16;
						int worldZ = localZ + chunkZ * 16;
			
						float noiseValue3D = noise.GetValue3D(worldX, localY, worldZ, octaves: 6, startFrequency: 0.05f, startAmplitude: 2f) * 4f;
						
						int d = Mathf.Clamp(Mathf.RoundToInt(noiseValue3D), 0, 4);
						
						if (d > 0)
						{
							m_tcache.m_data[localX + absX, localY, localZ + absZ] = (byte)d;
						}
						else
						{
							//lightInfo.strength = 16;
							m_tcache.m_data[localX + absX, localY, localZ + absZ] = 0;
						}
					}
					
					chunkInfo.data[localX, localY, localZ] = m_tcache.m_data[localX + absX, localY, localZ + absZ];
					
					m_tcache.lightData[localX + absX, localY, localZ + absZ] = lightInfo;
				}
			}
		}
		
		m_tcache.m_changed.Add(chunkInfo);
	}
	
	
	IEnumerator workOnChunks()
	{
		while(true)
		{
			for(int i = 0; i < 1; i++)
			{
				//ThreadPool.QueueUserWorkItem(ProcessChunks, (chunkQueues[0] as Queue<ChunkData>));
			}
		
			yield return null;
		}
	}
	
	
	// Update is called once per frame
	IEnumerator UpdateBrain ()
    {
		//StartCoroutine(UpdateBody());
		
		chunkQueues.Add(new Queue<ChunkData>());
		
		//StartCoroutine(workOnChunks());
		ChunkData chunk = null;
		
        while (true)
        {
           // if (Input.GetKeyDown(KeyCode.R))
                //getMapThread.Start();
                //selectedBlock++;
			chunk = GetAFinishedChunk();
			
			while(chunk != null)
			{
	            if (chunk != null)
	            {
	                chunk.GetComponent<Chunk>().RegenerateChunk();
					chunksToLightUp.Enqueue(chunk);
	            }
				
				chunk = GetAFinishedChunk();
			}
			
			chunk = GetAChunkToLightUp();
			
			while(chunk != null)
			{
	            if (chunk != null)
	            {
	                chunk.GetComponent<Chunk>().RelightChunk();
	            }
				
				chunk = GetAChunkToLightUp();
			}

            //if(getMapThread != null)
//            Debug.Log(getMapThread.ThreadState);

            selectedBlock = (byte)Mathf.Repeat(selectedBlock, blockTypeArray.Length);

            if (selectedBlock == 0)
                selectedBlock++;

            currentBlockType = blockTypeArray[selectedBlock];

            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
                    Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
                {
                    Screen.lockCursor = true;
                }
            }

            // Current workaround to get loading GUI up before generating the terrain
            if (m_loaded > 1)
            {
                m_loaded--;
                yield return null;
                continue;
            }
            else if (m_loaded == 1)
            {
                yield return StartCoroutine(stepLoad());
                m_loaded = 0;
                GUIText status = GameObject.Find("StatusDisplay").GetComponent<GUIText>();
                status.text = "";
                playerPtr.position = playerStart;
            }

            currentPos = playerPtr.position;

            currentPos.x = Mathf.Floor(currentPos.x / chunkSize.x);
            currentPos.y = Mathf.Floor(currentPos.y / chunkSize.x);
            currentPos.z = Mathf.Floor(currentPos.z / chunkSize.x);


            if (currentPos != lastPos) 
            {
                Vector3 movement = currentPos - lastPos;

                int moveX = Mathf.RoundToInt(movement.x);
                int moveY = Mathf.RoundToInt(movement.y);
                int moveZ = Mathf.RoundToInt(movement.z);

                if (moveX != 0)
                {
                    int newX = Mathf.RoundToInt(lastPos.x) + moveX * (blockLoadDistance + 1);

                    for (int z = Mathf.RoundToInt(lastPos.z) - blockLoadDistance; z <= Mathf.RoundToInt(lastPos.z) + blockLoadDistance; z++)
                    {
                        if (UpdateChunk(new Vector3(newX, 0, z)))
                            enqueueChunk(GetChunk(new Vector3(newX, 0, z)));
                    }
                }

                if (moveZ != 0)
                {
                    //int z = Mathf.RoundToInt(lastPos.z) - moveZ*blockLoadDistance;
                    int newZ = Mathf.RoundToInt(lastPos.z) + moveZ * (blockLoadDistance + 1);

                    for (int x = Mathf.RoundToInt(lastPos.x) - blockLoadDistance; x <= Mathf.RoundToInt(lastPos.x) + blockLoadDistance; x++)
                    {
                        if (UpdateChunk(new Vector3(x, 0, newZ)))
                            enqueueChunk(GetChunk(new Vector3(x, 0, newZ)));
                    }
                }
            }
            
            lastPos = currentPos;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
			

            yield return null;
        }
	}

    public void generateTerrainChunk(int x, int y, int z)
    {
       // m_tcache.getChunk(x, y, z);
    }

	
	private void noiseButton_Click()
	{
	    
	
	}
	

    public byte getDensity(Vector3 loc)
    {
		/*
		ChunkNoise noise = new ChunkNoise(seed: 12345);
		float val = noise.GetValue3D((int)loc.x, (int)loc.y, (int)loc.z, octaves: 6, startFrequency: .05f, startAmplitude: 1);

		if (val * 4 > 0.5f)
		{
			Debug.Log("Stone");
			return 1;
		}
		else
		{
			return 0;
		}
        	*/
		
        float offsetFactor = 500000.0f;
        Vector3 offset = new Vector3(loc.x + offsetFactor, loc.y + offsetFactor, loc.z + offsetFactor);
        float n = noise(loc.x * 0.03f, loc.y, loc.z * 0.03f) * 1f;
		n += noise(loc.x * 0.7f, loc.y, loc.z * 0.7f) * 0.2f;
		n -= noise(loc.x * 2, loc.y, loc.z * 2) * 0.3f;
        int d = Mathf.Clamp((int)((1.2f - loc.y + n) * 32.0f), 0, 4);
        d = (1.0f + noise(offset.x, offset.y, offset.z)) > 1.2f ? 0 : d;
        
		if (d < 0) d = 0;
        	return (byte)d;
    }
   
	
	public byte[,,] getDensity(int x, int z)
    {
		/*
		ChunkNoise noise = new ChunkNoise(seed: 12345);
		float val = noise.GetValue3D((int)loc.x, (int)loc.y, (int)loc.z, octaves: 6, startFrequency: .05f, startAmplitude: 1);

		if (val * 4 > 0.5f)
		{
			Debug.Log("Stone");
			return 1;
		}
		else
		{
			return 0;
		}
        	*/
		float offsetFactor = 500000.0f;
			        
		byte[,,] blocks = new byte[16, 128, 16];
		
		for (int localX = 0; localX < 16; localX++)
		{
			for (int localZ = 0; localZ < 16; localZ++)
			{
				for (int y = 0; y < 128; y++)
				{
					Vector3 offset = new Vector3((x * 16) + localX + offsetFactor, y + offsetFactor, (z * 16) + localZ + offsetFactor);
			        float n = noise(localX * 0.03f, y, localZ * 0.03f) * 1f;
					n += noise(localX * 0.7f, y, localZ * 0.7f) * 0.2f;
					n -= noise(localX * 2, y, localZ * 2) * 0.3f;
			        int d = Mathf.Clamp((int)((1.2f - y + n) * 32.0f), 0, 4);
			        d = (1.0f + noise(offset.x, offset.y, offset.z)) > 1.2f ? 0 : d;
			        
					if (d < 0) 
						d = 0;
			        
					blocks[localX, y, localZ] = (byte)d;
				}
			}
		}
		
		return blocks;
    }
   
	
	
    private int[] A = new int[3];
    private float s, u, v, w;
    private int i, j, k;
    private float onethird = 0.333333333f;
    private float onesixth = 0.166666667f;
    private int[] T;
    //private int[] T = {0x15, 0x38, 0x32, 0x2c, 0x0d, 0x13, 0x07, 0x2a};
    // Simplex Noise Generator
    float noise(float x, float y, float z)
    {
        if (T == null)
        {
            System.Random rand = new System.Random(123456789);
            T = new int[8];
            for (int q = 0; q < 8; q++)
                T[q] = rand.Next();
        }

        s = (x + y + z) * onethird;
        i = fastfloor(x + s);
        j = fastfloor(y + s);
        k = fastfloor(z + s);

        s = (i + j + k) * onesixth;
        u = x - i + s;
        v = y - j + s;
        w = z - k + s;

        A[0] = 0; A[1] = 0; A[2] = 0;

        int hi = u >= w ? u >= v ? 0 : 1 : v >= w ? 1 : 2;
        int lo = u < w ? u < v ? 0 : 1 : v < w ? 1 : 2;

        return kay(hi) + kay(3 - hi - lo) + kay(lo) + kay(0);
    }

    float kay(int a)
    {
        s = (A[0] + A[1] + A[2]) * onesixth;
        float x = u - A[0] + s;
        float y = v - A[1] + s;
        float z = w - A[2] + s;
        float t = 0.6f - x * x - y * y - z * z;
        int h = shuffle(i + A[0], j + A[1], k + A[2]);
        A[a]++;
        if (t < 0) return 0;
        int b5 = h >> 5 & 1;
        int b4 = h >> 4 & 1;
        int b3 = h >> 3 & 1;
        int b2 = h >> 2 & 1;
        int b1 = h & 3;

        float p = b1 == 1 ? x : b1 == 2 ? y : z;
        float q = b1 == 1 ? y : b1 == 2 ? z : x;
        float r = b1 == 1 ? z : b1 == 2 ? x : y;

        p = b5 == b3 ? -p : p;
        q = b5 == b4 ? -q : q;
        r = b5 != (b4 ^ b3) ? -r : r;
        t *= t;
        return 8 * t * t * (p + (b1 == 0 ? q + r : b2 == 0 ? q : r));
    }

    int shuffle(int i, int j, int k)
    {
        return b(i, j, k, 0) + b(j, k, i, 1) + b(k, i, j, 2) + b(i, j, k, 3) + b(j, k, i, 4) + b(k, i, j, 5) + b(i, j, k, 6) + b(j, k, i, 7);
    }

    int b(int i, int j, int k, int B)
    {
        return T[b(i, B) << 2 | b(j, B) << 1 | b(k, B)];
    }

    int b(int N, int B)
    {
        return N >> B & 1;
    }

    int fastfloor(float n)
    {
        return n > 0 ? (int)n : (int)n - 1;
    }

}

