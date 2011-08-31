using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class World : MonoBehaviour
{
    public enum GenerateType
    {
        FULL,
        HALF,
        HEIGHTMAP
    }

    public GenerateType generateType = GenerateType.HALF;

    //height map texture
    public Texture2D hmTex;
	
    public static World _main;

    public bool isReady = false;

    private int chunkSize = 16;

	public int gridSizeX = 16;
	public int gridSizeY = 16;
    public int gridSizeZ = 8;
    public int meshUpdateCap = 10;

    public Material atlasMap;

    private int meshesUpdated = 0;
	
    //all voxel meshes
    public GameObject[][][] voxelModels;

    private List<int[]> chunckUpdateList = new List<int[]>();
		
	private float tileSize;
    private byte[][][] worldArray;
    private int[] worldID;

    public int currentBlockType = 3;
    public int[] blocktypes;
    private int selectedBlock = 0;
	
	Vector3[] verticesList;
	Vector2[] uvList;

    //pre-initialise memory needed for building the most complex possible meshes
    private Vector3[] vertsArray = new Vector3[49152];
    private Color[] vertCols = new Color[49152];
    private Vector3[] normsArray = new Vector3[49152];
    private Vector2[] uvsArray = new Vector2[49152];
    private int[] trissArray = new int[73728];

    private int totalBlocks = 0;

    private Transform[] collideCubes = new Transform[27];
    private Transform[] blockCollideCubes = new Transform[27];
	
	private Transform chunkManager;
	

    void Awake()
    {
		chunkManager = new GameObject("chunkManager").transform;
        _main = this;
    }

	
	IEnumerator Start()
	{
		tileSize = 1f/16f;

        // make GameObjects for each chunck's mesh
        voxelModels = new GameObject[gridSizeX][][];

        for (int i = 0; i < voxelModels.Length; i++)
        {
            voxelModels[i] = new GameObject[gridSizeY][];

            for (int j = 0; j < voxelModels[i].Length; j++)
                voxelModels[i][j] = new GameObject[gridSizeZ];
        }


        worldArray = new byte[chunkSize * gridSizeX][][];
 
        for (int i = 0; i < worldArray.Length; i++)
        {
            worldArray[i] = new byte[chunkSize * gridSizeY][];

            for (int j = 0; j < worldArray[i].Length; j++)
                worldArray[i][j] = new byte[chunkSize * gridSizeZ];
        }
/*
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    GameObject voxelModel = new GameObject();

					voxelModel.parent = chunkManager;
                    voxelModel.transform.position = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
                    voxelModel.AddComponent<MeshRenderer>();
                    voxelModel.renderer.material = atlasMap;
                    voxelModel.AddComponent<MeshFilter>();
                    voxelModels[x][y][z] = voxelModel;
                }
            }

            yield return null;
        }
*/
        totalBlocks = 0;

        for (int x = 0; x < worldArray.GetLength(0); x++)
            for(int y = 0; y < worldArray[x].Length; y++)
                for(int z = 0; z < worldArray[x][y].Length; z++)
                    worldArray[x][y][z] = 0;


        switch (generateType)
        {
            case GenerateType.HALF:
                for (int x = 0; x < gridSizeX * chunkSize; x++)
                {
                    for (int y = 0; y < (gridSizeY * chunkSize) / 2; y++)
                    {
                        for (int z = 0; z < gridSizeZ * chunkSize; z++)
                        {
                            totalBlocks++;
                            worldArray[x][y][z] = 3;
                        }
                    }
                }
                break;

            case GenerateType.FULL:
                for (int x = 0; x < gridSizeX * chunkSize; x++)
                {
                    for (int y = 0; y < (gridSizeY * chunkSize); y++)
                    {
                        for (int z = 0; z < gridSizeZ * chunkSize; z++)
                        {
                            totalBlocks++;
                            worldArray[x][y][z] = 3;
                        }
                    }
                }
                break;

            case GenerateType.HEIGHTMAP:

                for (int x = 0; x < gridSizeX * chunkSize; x++)
                {
                    for (int y = 0; y < gridSizeY * chunkSize; y++)
                    {
                        for (int z = 0; z < gridSizeZ * chunkSize; z++)
                        {
                            //if (z==0) print(  );
                            if (hmTex.GetPixel((int)(Mathf.Round((((float)x) / (gridSizeX * chunkSize)) * hmTex.width)), (int)(Mathf.Round((((float)z) / (gridSizeZ * chunkSize)) * hmTex.width))).r * (gridSizeY * chunkSize) > y)
                            {
                                totalBlocks++;

                                if (hmTex.GetPixel((int)(Mathf.Round((((float)x) / (gridSizeX * chunkSize)) * hmTex.width)), (int)(Mathf.Round((((float)z) / (gridSizeZ * chunkSize)) * hmTex.width))).r > 0.6f)
                                {
                                    if (Random.Range(0, 10) < 7)
                                        worldArray[x][y][z] = 2;
                                    else
                                        worldArray[x][y][z] = 3;
                                }
                                else
                                {
                                    worldArray[x][y][z] = 3;
                                }
                            }
                        }
                    }
                }
                break;
        }


        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    BuildChunck(x, y, z);
                }
            }
        }

        yield return null;
	}


    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), "Total Blocks: " + totalBlocks);
        GUI.Label(new Rect(10, 30, 100, 20), "Updates: " + meshesUpdated);
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            selectedBlock++;

        selectedBlock = (int)Mathf.Repeat(selectedBlock, blocktypes.Length);

        currentBlockType = blocktypes[selectedBlock];

        if (chunckUpdateList.Count != 0)
        {
            meshesUpdated = 0;

            for (int i = 0; i < chunckUpdateList.Count; i++)
            {
                int[] thisChunck = (int[])chunckUpdateList[i];

                bool buildThis = true;

                if (Vector3.Distance(Camera.main.transform.position, new Vector3(thisChunck[0] * chunkSize, thisChunck[1] * chunkSize, thisChunck[2] * chunkSize)) > 100)
                {
					//chunck is out of draw distance, fuck updating it now, do it later
					buildThis = false;
				}
                else if (Vector3.Dot(Camera.main.transform.forward, Camera.main.transform.position - new Vector3(thisChunck[0] * chunkSize, thisChunck[1] * chunkSize, thisChunck[2] * chunkSize)) > 0)
                {
					//cam not even looking that way yet, fuck it.
					buildThis = false;

                    if (Vector3.Distance(Camera.main.transform.position, new Vector3(thisChunck[0] * chunkSize, thisChunck[1] * chunkSize, thisChunck[2] * chunkSize)) < chunkSize * 3)
                    {
						//pretty near I guess, the center of the chunck could be behind the cam but the thing still be visible
						//meh, guess we *can* update this or whatever,
						buildThis = true;
					}
				}
				
				if (meshesUpdated >= meshUpdateCap)
                {
					//updated enough chuncks this frame, leave it for now
					buildThis = false;
					continue;
				}

                if (buildThis)
                {
                    meshesUpdated++;
                    BuildChunckMesh((int)thisChunck[0], (int)thisChunck[1], (int)thisChunck[2]);
                    chunckUpdateList.RemoveAt(i);
                    i--;
                }
            }

            //chunckUpdateList = new ArrayList();
        }
    }


    //if there is a block here, remove it, otherwise add a block
    public void TogglePoint(Vector3 pointy)
    {
        int xx = (int)Mathf.Round(pointy.x);
        int yy = (int)Mathf.Round(pointy.y);
        int zz = (int)Mathf.Round(pointy.z);

        if (!isReallyBlockHere(xx, yy, zz))
        {
            //Add block
            worldArray[xx][yy][zz] = (byte)currentBlockType;
            totalBlocks++;
        }
        else
        {
            //Take Block
            worldArray[xx][yy][zz] = 0;
            totalBlocks--;
        }

        int[] changeVoxel = new int[3];
        changeVoxel[0] = xx; changeVoxel[1] = yy; changeVoxel[2] = zz;
        updateChuncks(changeVoxel);
    }

    //set the voxel at a point to be a specific type of voxel
    public void SetPoint(Vector3 pointy, int theBlocktype)
    {
        theBlocktype = currentBlockType;

        int xx = (int)Mathf.Round(pointy.x);
        int yy = (int)Mathf.Round(pointy.y);
        int zz = (int)Mathf.Round(pointy.z);

        if (!isReallyBlockHere(xx, yy, zz))
        {
            if (worldArray[xx][yy][zz] != 0 && theBlocktype == 0)
            {
                totalBlocks--;
            }
            if (worldArray[xx][yy][zz] == 0 && theBlocktype != 0)
            {
                totalBlocks++;
            }

            worldArray[xx][yy][zz] = (byte)theBlocktype;
        }

        int[] changeVoxel = new int[3];
        changeVoxel[0] = xx; changeVoxel[1] = yy; changeVoxel[2] = zz;

        updateChuncks(changeVoxel);
    }


    //returns co-ords of the first block that the line touches, in [x,y,z] format
    public int[] LineCheck(Vector3 startPos, Vector3 endPos, bool realCheck)
    {
        int[] lineResults = new int[6];
        int blockCounter = 0;

        lineResults[0] = -1;
        lineResults[1] = -1;
        lineResults[2] = -1;

        float lineLength = Vector3.Distance(startPos, endPos);
        Vector3 lineDirection = startPos - endPos;
        lineDirection.Normalize();

        foreach (Transform cube in blockCollideCubes)
            if (cube != null)
                cube.position = Vector3.zero;

        for (int i = 0; i < lineLength; i++)
        {
            int thisX = (int)Mathf.Round(startPos.x + (lineDirection.x * i));
            int thisY = (int)Mathf.Round(startPos.y + (lineDirection.y * i));
            int thisZ = (int)Mathf.Round(startPos.z + (lineDirection.z * i));

            if ((!realCheck && isBlockHere(thisX, thisY, thisZ)) || (realCheck && isReallyBlockHere(thisX, thisY, thisZ)))
            {
                lineResults[0] = thisX;
                lineResults[1] = thisY;
                lineResults[2] = thisZ;

                break;
            }            
        }


        for (int xness = lineResults[0] - 1; xness < lineResults[0] + 2; xness++)
        {
            for (int yness = lineResults[1] - 1; yness < lineResults[1] + 2; yness++)
            {
                for (int zness = lineResults[2] - 1; zness < lineResults[2] + 2; zness++)
                {
                    if ((isReallyBlockHere(xness, yness, zness)))
                    {
                        if (blockCollideCubes[blockCounter] == null)
                        {
                            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                            cube.transform.position = new Vector3(xness, yness, zness);
                            cube.renderer.enabled = false;
                            cube.layer = 20;
                            blockCollideCubes[blockCounter] = cube.transform;
                        }
                        else
                        {
                            blockCollideCubes[blockCounter].position = new Vector3(xness, yness, zness);
                        }

                        blockCounter++;
                    }
                }
            }
        }

        RaycastHit hit;

        if (Physics.Raycast(startPos, lineDirection, out hit, lineLength, 1<<20))
        {
            lineResults[0] = (int)hit.transform.position.x;
            lineResults[1] = (int)hit.transform.position.y;
            lineResults[2] = (int)hit.transform.position.z;

            lineResults[3] = (int)hit.normal.normalized.x;
            lineResults[4] = (int)hit.normal.normalized.y;
            lineResults[5] = (int)hit.normal.normalized.z;
        }

        return lineResults;
    }


    //returns co-ords of the last empty block that the line passes through before hitting something, in [x,y,z] format
    public int[] LastFreeLineCheck(Vector3 startPos, Vector3 endPos)
    {
        int[] lineResults = new int[3];

        lineResults[0] = -1;
        lineResults[1] = -1;
        lineResults[2] = -1;

        bool foundIt = false;

        float lineLength = Vector3.Distance(startPos, endPos);
        Vector3 lineDirection = startPos - endPos;
        lineDirection.Normalize();

        for (int i = 0; i < lineLength; i++)
        {
            int thisX = (int)Mathf.Round(startPos.x + (lineDirection.x * i));
            int thisY = (int)Mathf.Round(startPos.y + (lineDirection.y * i));
            int thisZ = (int)Mathf.Round(startPos.z + (lineDirection.z * i));
            if (isBlockHere(thisX, thisY, thisZ))
            {
                foundIt = true;
                return lineResults;
            }
            if (!foundIt)
            {
                lineResults[0] = thisX;
                lineResults[1] = thisY;
                lineResults[2] = thisZ;
            }
        }
        if (!foundIt)
        {
            lineResults[0] = -1;
            lineResults[1] = -1;
            lineResults[2] = -1;
        }
        return lineResults;
    }


    //returns true if there is a block here, or if the point is outside the horizontal boundries
    public bool isBlockHere(int theX, int theY, int theZ)
    {
        if (theX < 0) return true;
        if (theX >= chunkSize * gridSizeX) return true;
        if (theY < 0) return true;
        if (theY >= chunkSize * gridSizeY) return false;
        if (theZ < 0) return true;
        if (theZ >= chunkSize * gridSizeZ) return true;

        return (worldArray[theX][theY][theZ] >= 1);
    }
	

    //returns true if there is a block here
    public bool isReallyBlockHere(int theX, int theY, int theZ)
    {
        if (theX < 0) return false;
        if (theX >= chunkSize * gridSizeX) return false;
        if (theY < 0) return false;
        if (theY >= chunkSize * gridSizeY) return false;
        if (theZ < 0) return false;
        if (theZ >= chunkSize * gridSizeZ) return false;

        return (worldArray[theX][theY][theZ] >= 1);
    }



    int[] GetBlockID(int _id, int buildX, int buildY, int buildZ)
    {
        int[] faceIDs = new int[6];
        
        for(int i = 0; i < 6; i++)
            faceIDs[i] = _id;

        switch (_id)
        {
            case 3:
                // is block above is?
                if (!isReallyBlockHere(buildX, buildY + 1, buildZ))
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
        }

        return faceIDs;
    }



    //change all voxels withing a radius from a point
    public void Detonate(Vector3 pointy, int detRadius)
    {
        ArrayList changedBlocks = new ArrayList();
     
        int xx = (int)Mathf.Round(pointy.x);
        int yy = (int)Mathf.Round(pointy.y);
        int zz = (int)Mathf.Round(pointy.z);

        bool isAdditive = false;

        if (detRadius < 0)
        {
            isAdditive = true;
            detRadius *= -1;
        }

        for (int xness = xx - detRadius; xness < xx + detRadius; xness++)
        {
            for (int yness = yy - detRadius; yness < yy + detRadius; yness++)
            {
                for (int zness = zz - detRadius; zness < zz + detRadius; zness++)
                {
                    if (xness > -1 && xness < (gridSizeX * chunkSize))
                    {
                        if (yness > -1 && yness < (gridSizeY * chunkSize))
                        {
                            if (zness > -1 && zness < (gridSizeZ * chunkSize))
                            {
                                if (Vector3.Distance(new Vector3(xness, yness, zness), new Vector3(xx, yy, zz)) < detRadius)
                                {
                                    if (isAdditive)
                                    {
                                        if (worldArray[xness][yness][xness] == 0)
                                        {
                                            totalBlocks++;
                                        }

                                        worldArray[xness][yness][zness] = (byte)currentBlockType;
                                    }
                                    else
                                    {
                                        if (worldArray[xness][yness][zness] >= 1)
                                        {
                                            totalBlocks--;
                                        }

                                        worldArray[xness][yness][zness] = 0;
                                    }

                                    changedBlocks.Add(xness);
                                    changedBlocks.Add(yness);
                                    changedBlocks.Add(zness);
                                }
                            }
                        }
                    }
                }
            }
        }

        updateChuncksFast(changedBlocks);
    }


    //change all voxels withing a radius from a point
    public void CreateColliders(Vector3 pointy, int detRadius)
    {
        ArrayList changedBlocks = new ArrayList();

        int xx = (int)Mathf.Round(pointy.x);
        int yy = (int)Mathf.Round(pointy.y);
        int zz = (int)Mathf.Round(pointy.z);

        if (detRadius < 0)
        {
            detRadius *= -1;
        }

        int count = 0;

        foreach (Transform cube in collideCubes)
            if(cube != null)
                cube.position = Vector3.zero;

        for (int xness = xx - detRadius; xness < xx + detRadius; xness++)
        {
            for (int yness = yy - detRadius; yness < yy + detRadius; yness++)
            {
                for (int zness = zz - detRadius; zness < zz + detRadius; zness++)
                {
                    if (Vector3.Distance(new Vector3(xness, yness, zness), new Vector3(xx, yy, zz)) < detRadius)
                    {
                        if (isReallyBlockHere(xness, yness, zness))
                        {
                            if (collideCubes[count] == null)
                            {
                                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                cube.transform.position = new Vector3(xness, yness, zness);
                                cube.renderer.enabled = false;
                                collideCubes[count] = cube.transform;
                            }
                            else
                            {
                                collideCubes[count].position = new Vector3(xness, yness, zness);
                            }

                            count++;
                        }
                    }
                }
            }
        }
    }

	
	
    //add chuncks to the chunck update list that need rebuilding
    private void updateChuncks(int[] voxelChanges)
    {
        ArrayList chuncksList = new ArrayList();

        for (int i = 0; i < voxelChanges.Length; i += 3)
        {
            int[] chuncky = new int[3];
            chuncky[0] = voxelChanges[i] / chunkSize;
            chuncky[1] = voxelChanges[i + 1] / chunkSize;
            chuncky[2] = voxelChanges[i + 2] / chunkSize;
            chuncksList.Add(chuncky);

            //x-1
            chuncky = new int[3];
            chuncky[0] = (voxelChanges[i] - 1) / chunkSize;
            chuncky[1] = voxelChanges[i + 1] / chunkSize;
            chuncky[2] = voxelChanges[i + 2] / chunkSize;
            chuncksList.Add(chuncky);

            //x+1
            chuncky = new int[3];
            chuncky[0] = (voxelChanges[i] + 1) / chunkSize;
            chuncky[1] = voxelChanges[i + 1] / chunkSize;
            chuncky[2] = voxelChanges[i + 2] / chunkSize;
            chuncksList.Add(chuncky);

            //y-1
            chuncky = new int[3];
            chuncky[0] = voxelChanges[i] / chunkSize;
            chuncky[1] = (voxelChanges[i + 1] - 1) / chunkSize;
            chuncky[2] = voxelChanges[i + 2] / chunkSize;
            chuncksList.Add(chuncky);

            //y+1
            chuncky = new int[3];
            chuncky[0] = voxelChanges[i] / chunkSize;
            chuncky[1] = (voxelChanges[i + 1] + 1) / chunkSize;
            chuncky[2] = voxelChanges[i + 2] / chunkSize;
            chuncksList.Add(chuncky);

            //z-1
            chuncky = new int[3];
            chuncky[0] = voxelChanges[i] / chunkSize;
            chuncky[1] = voxelChanges[i + 1] / chunkSize;
            chuncky[2] = (voxelChanges[i + 2] - 1) / chunkSize;
            chuncksList.Add(chuncky);

            //z+1
            chuncky = new int[3];
            chuncky[0] = voxelChanges[i] / chunkSize;
            chuncky[1] = voxelChanges[i + 1] / chunkSize;
            chuncky[2] = (voxelChanges[i + 2] + 1) / chunkSize;
            chuncksList.Add(chuncky);
        }



        for (int i = 0; i < chuncksList.Count; i++)
        {
            int[] buildyChunck = (int[])(chuncksList[i]);

            bool doIt = true;

            if (doIt)
            {
                bool isDouble = false;

                for (int k = 0; k < chunckUpdateList.Count; k++)
                {
                    int[] thisChunck = (int[])(chunckUpdateList[k]);

                    if (thisChunck[0] == buildyChunck[0] && thisChunck[1] == buildyChunck[1] && thisChunck[2] == buildyChunck[2])
                    {
                        isDouble = true;
                    }


                }
                if (!isDouble)
                {
                    chunckUpdateList.Add(buildyChunck);
                }

            }
        }

    }
	


    //add chuncks to the chunck update list that need rebuilding (takes arraylist insteat of int[])
    private void updateChuncksFast(ArrayList voxelChanges)
    {
        ArrayList chuncksList = new ArrayList();

        int[] voxelNess = new int[3];

        for (int i = 0; i < voxelChanges.Count; i += 3)
        {

            voxelNess[0] = (int)voxelChanges[i];
            voxelNess[1] = (int)voxelChanges[i + 1];
            voxelNess[2] = (int)voxelChanges[i + 2];

            int[] chuncky = new int[3];
            chuncky[0] = voxelNess[0] / chunkSize;
            chuncky[1] = voxelNess[1] / chunkSize;
            chuncky[2] = voxelNess[2] / chunkSize;
            chuncksList.Add(chuncky);

            //x-1
            chuncky = new int[3];
            chuncky[0] = (voxelNess[0] - 1) / chunkSize;
            chuncky[1] = voxelNess[1] / chunkSize;
            chuncky[2] = voxelNess[2] / chunkSize;
            chuncksList.Add(chuncky);

            //x+1
            chuncky = new int[3];
            chuncky[0] = (voxelNess[0] + 1) / chunkSize;
            chuncky[1] = voxelNess[1] / chunkSize;
            chuncky[2] = voxelNess[2] / chunkSize;
            chuncksList.Add(chuncky);

            //y-1
            chuncky = new int[3];
            chuncky[0] = voxelNess[0] / chunkSize;
            chuncky[1] = (voxelNess[1] - 1) / chunkSize;
            chuncky[2] = voxelNess[2] / chunkSize;
            chuncksList.Add(chuncky);

            //y+1
            chuncky = new int[3];
            chuncky[0] = voxelNess[0] / chunkSize;
            chuncky[1] = (voxelNess[1] + 1) / chunkSize;
            chuncky[2] = voxelNess[2] / chunkSize;
            chuncksList.Add(chuncky);

            //z-1
            chuncky = new int[3];
            chuncky[0] = voxelNess[0] / chunkSize;
            chuncky[1] = voxelNess[1] / chunkSize;
            chuncky[2] = (voxelNess[2] - 1) / chunkSize;
            chuncksList.Add(chuncky);

            //z+1
            chuncky = new int[3];
            chuncky[0] = voxelNess[0] / chunkSize;
            chuncky[1] = voxelNess[1] / chunkSize;
            chuncky[2] = (voxelNess[2] + 1) / chunkSize;
            chuncksList.Add(chuncky);
        }



        for (int i = 0; i < chuncksList.Count; i++)
        {
            int[] buildyChunck = (int[])(chuncksList[i]);

            bool doIt = true;
            /*
            if (buildyChunck[0]<0) doIt = false;
            if (buildyChunck[0]>cCount) doIt = false;
            if (buildyChunck[1]<0) doIt = false;
            if (buildyChunck[1]>cCount) doIt = false;
            if (buildyChunck[2]<0) doIt = false;
            if (buildyChunck[2]>cCount) doIt = false;
            */
            if (doIt)
            {
                //BuildChunck(buildyChunck[0], buildyChunck[1], buildyChunck[2]);
                //chunckUpdateList.Add(buildyChunck);

                bool isDouble = false;
                for (int k = 0; k < chunckUpdateList.Count; k++)
                {
                    int[] thisChunck = (int[])(chunckUpdateList[k]);

                    if (thisChunck[0] == buildyChunck[0] && thisChunck[1] == buildyChunck[1] && thisChunck[2] == buildyChunck[2])
                    {
                        isDouble = true;
                    }


                }
                if (!isDouble)
                {
                    chunckUpdateList.Add(buildyChunck);
                }

            }
        }

    }


    //add a specific chunck to the update list
    void BuildChunck(int chunckX, int chunckY, int chunckZ)
    {
        int[] chuncky = new int[3];
        chuncky[0] = chunckX;
        chuncky[1] = chunckY;
        chuncky[2] = chunckZ;
        chunckUpdateList.Add(chuncky);
    }



    void BuildChunckMesh(int chunckX, int chunckY, int chunckZ)
    {
        int cIndex = (((chunckZ * gridSizeZ) + chunckY) * gridSizeY) + chunckX;

        if (chunckX >= gridSizeX || chunckY >= gridSizeY || chunckZ >= gridSizeZ)
        {
            //if the chunck doesn't exist, we don't gotta build a mesh for it, woo
            return;
        }
		
		if(voxelModels[chunckX][chunckY][chunckZ] == null)
		{
			GameObject voxelModel = new GameObject();

			voxelModel.transform.parent = chunkManager;
			voxelModel.transform.position = new Vector3(chunckX * chunkSize, chunckY * chunkSize, chunckZ * chunkSize);
			voxelModel.AddComponent<MeshRenderer>();
			voxelModel.renderer.material = atlasMap;
			voxelModel.AddComponent<MeshFilter>();
			voxelModels[chunckX][chunckY][chunckZ] = voxelModel;
		}
		
        int xOffset = chunckX * chunkSize;
        int yOffset = chunckY * chunkSize;
        int zOffset = chunckZ * chunkSize;
        int xOffsetB = -xOffset;
        int yOffsetB = -yOffset;
        int zOffsetB = -zOffset;

        Mesh meshy = voxelModels[chunckX][chunckY][chunckZ].GetComponent<MeshFilter>().mesh;
        meshy.Clear();

        int vertIndex = 0;
        int triIndex = 0;

        Vector2 leftBottom = new Vector2();
        Vector2 leftTop = new Vector2();
        Vector2 rightTop = new Vector2();
        Vector2 rightBottom = new Vector2();

        float x = 0f, y = 16f;


        for (int buildX = xOffset; buildX < xOffset + chunkSize; buildX++)
        {
            for (int buildY = yOffset; buildY < yOffset + chunkSize; buildY++)
            {
                for (int buildZ = zOffset; buildZ < zOffset + chunkSize; buildZ++)
                {
                    if (isReallyBlockHere(buildX, buildY, buildZ))
                    {
                        int blockID = worldArray[buildX][buildY][buildZ];
                        int[] faceIDs = GetBlockID(blockID, buildX, buildY, buildZ);

                        for (int iFace = 0; iFace < 6; iFace++)
                        {
                            faceIDs[iFace]--;
                        }   

                        if (!isReallyBlockHere(buildX - 1, buildY, buildZ))
                        {
                            x = (float)faceIDs[0];
                            y = 16f;

                            while (faceIDs[0] >= 16)
                            {
                                faceIDs[0] -= 16;
                                y--;
                            }

                            x = (float)faceIDs[0];

                            leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
                            leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
                            rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
                            rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));
                           
                            //make left face
                            vertsArray[vertIndex] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 1] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 2] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 3] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ + 0.5f));
                            normsArray[vertIndex] = (-Vector3.right);
                            normsArray[vertIndex + 1] = (-Vector3.right);
                            normsArray[vertIndex + 2] = (-Vector3.right);
                            normsArray[vertIndex + 3] = (-Vector3.right);

                            uvsArray[vertIndex] = rightBottom;
                            uvsArray[vertIndex + 1] = rightTop;
                            uvsArray[vertIndex + 2] = leftTop;
                            uvsArray[vertIndex + 3] = leftBottom;

                            trissArray[triIndex] = (vertIndex);
                            trissArray[triIndex + 1] = (vertIndex + 2);
                            trissArray[triIndex + 2] = (vertIndex + 1);
                            trissArray[triIndex + 3] = (vertIndex);
                            trissArray[triIndex + 4] = (vertIndex + 3);
                            trissArray[triIndex + 5] = (vertIndex + 2);
                            vertIndex += 4;
                            triIndex += 6;
                        }

                        if (!isReallyBlockHere(buildX + 1, buildY, buildZ))
                        {
                            x = (float)faceIDs[1];
                            y = 16f;

                            while (faceIDs[1] >= 16)
                            {
                                faceIDs[1] -= 16;
                                y--;
                            }

                            x = (float)faceIDs[1];

                            leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
                            leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
                            rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
                            rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));

                            //make right face
                            vertsArray[vertIndex] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 1] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 2] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 3] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ + 0.5f));

                            normsArray[vertIndex] = (Vector3.right);
                            normsArray[vertIndex + 1] = (Vector3.right);
                            normsArray[vertIndex + 2] = (Vector3.right);
                            normsArray[vertIndex + 3] = (Vector3.right);

                            uvsArray[vertIndex] = leftBottom;
                            uvsArray[vertIndex + 1] = leftTop;
                            uvsArray[vertIndex + 2] = rightTop;
                            uvsArray[vertIndex + 3] = rightBottom;

                            trissArray[triIndex] = (vertIndex);
                            trissArray[triIndex + 1] = (vertIndex + 1);
                            trissArray[triIndex + 2] = (vertIndex + 2);
                            trissArray[triIndex + 3] = (vertIndex);
                            trissArray[triIndex + 4] = (vertIndex + 2);
                            trissArray[triIndex + 5] = (vertIndex + 3);
                            vertIndex += 4;
                            triIndex += 6;
                        }

                        if (!isReallyBlockHere(buildX, buildY - 1, buildZ))
                        {
                            x = (float)faceIDs[2];
                            y = 16f;

                            while (faceIDs[2] >= 16)
                            {
                                faceIDs[2] -= 16;
                                y--;
                            }

                            x = (float)faceIDs[2];

                            leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
                            leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
                            rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
                            rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));

                            //make bottom face
                            vertsArray[vertIndex] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 1] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 2] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 3] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ + 0.5f));
                            normsArray[vertIndex] = (-Vector3.up);
                            normsArray[vertIndex + 1] = (-Vector3.up);
                            normsArray[vertIndex + 2] = (-Vector3.up);
                            normsArray[vertIndex + 3] = (-Vector3.up);
                            uvsArray[vertIndex] = leftBottom;
                            uvsArray[vertIndex + 1] = rightBottom;
                            uvsArray[vertIndex + 2] = rightTop;
                            uvsArray[vertIndex + 3] = leftTop;
                            trissArray[triIndex] = (vertIndex);
                            trissArray[triIndex + 1] = (vertIndex + 1);
                            trissArray[triIndex + 2] = (vertIndex + 2);
                            trissArray[triIndex + 3] = (vertIndex);
                            trissArray[triIndex + 4] = (vertIndex + 2);
                            trissArray[triIndex + 5] = (vertIndex + 3);
                            vertIndex += 4;
                            triIndex += 6;
                        }

                        if (!isReallyBlockHere(buildX, buildY + 1, buildZ))
                        {
                            x = (float)faceIDs[3];
                            y = 16f;

                            while (faceIDs[3] >= 16)
                            {
                                faceIDs[3] -= 16;
                                y--;
                            }

                            x = (float)faceIDs[3];

                            leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
                            leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
                            rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
                            rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));

                            //make top face
                            vertsArray[vertIndex] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 1] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 2] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 3] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ + 0.5f));
                            normsArray[vertIndex] = (Vector3.up);
                            normsArray[vertIndex + 1] = (Vector3.up);
                            normsArray[vertIndex + 2] = (Vector3.up);
                            normsArray[vertIndex + 3] = (Vector3.up);
                            uvsArray[vertIndex] = leftBottom;
                            uvsArray[vertIndex + 1] = rightBottom;
                            uvsArray[vertIndex + 2] = rightTop;
                            uvsArray[vertIndex + 3] = leftTop;
                            trissArray[triIndex] = (vertIndex);
                            trissArray[triIndex + 1] = (vertIndex + 2);
                            trissArray[triIndex + 2] = (vertIndex + 1);
                            trissArray[triIndex + 3] = (vertIndex);
                            trissArray[triIndex + 4] = (vertIndex + 3);
                            trissArray[triIndex + 5] = (vertIndex + 2);
                            vertIndex += 4;
                            triIndex += 6;
                        }

                        if (!isReallyBlockHere(buildX, buildY, buildZ - 1))
                        {
                            x = (float)faceIDs[4];
                            y = 16f;

                            while (faceIDs[4] >= 16)
                            {
                                faceIDs[4] -= 16;
                                y--;
                            }

                            x = (float)faceIDs[4];

                            leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
                            leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
                            rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
                            rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));

                            //make back face
                            vertsArray[vertIndex] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 1] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 2] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ - 0.5f));
                            vertsArray[vertIndex + 3] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ - 0.5f));
                            normsArray[vertIndex] = (-Vector3.forward);
                            normsArray[vertIndex + 1] = (-Vector3.forward);
                            normsArray[vertIndex + 2] = (-Vector3.forward);
                            normsArray[vertIndex + 3] = (-Vector3.forward);
                            uvsArray[vertIndex] = leftBottom;
                            uvsArray[vertIndex + 1] = leftTop;
                            uvsArray[vertIndex + 2] = rightTop;
                            uvsArray[vertIndex + 3] = rightBottom;
                            trissArray[triIndex] = (vertIndex);
                            trissArray[triIndex + 1] = (vertIndex + 1);
                            trissArray[triIndex + 2] = (vertIndex + 2);
                            trissArray[triIndex + 3] = (vertIndex);
                            trissArray[triIndex + 4] = (vertIndex + 2);
                            trissArray[triIndex + 5] = (vertIndex + 3);
                            vertIndex += 4;
                            triIndex += 6;
                        }

                        if (!isReallyBlockHere(buildX, buildY, buildZ + 1))
                        {
                            x = (float)faceIDs[5];
                            y = 16f;

                            while (faceIDs[5] >= 16)
                            {
                                faceIDs[5] -= 16;
                                y--;
                            }

                            x = (float)faceIDs[5];

                            leftBottom = new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f));
                            leftTop = new Vector2(Mathf.Abs(x * tileSize), tileSize * y);
                            rightTop = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * y);
                            rightBottom = new Vector2(Mathf.Abs((x + 1) * tileSize), tileSize * (y - 1f));

                            //make front face
                            vertsArray[vertIndex] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 1] = (new Vector3(xOffsetB + buildX - 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 2] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY + 0.5f, zOffsetB + buildZ + 0.5f));
                            vertsArray[vertIndex + 3] = (new Vector3(xOffsetB + buildX + 0.5f, yOffsetB + buildY - 0.5f, zOffsetB + buildZ + 0.5f));
                            normsArray[vertIndex] = (Vector3.forward);
                            normsArray[vertIndex + 1] = (Vector3.forward);
                            normsArray[vertIndex + 2] = (Vector3.forward);
                            normsArray[vertIndex + 3] = (Vector3.forward);
                            uvsArray[vertIndex] = rightBottom;
                            uvsArray[vertIndex + 1] = rightTop;
                            uvsArray[vertIndex + 2] = leftTop;
                            uvsArray[vertIndex + 3] = leftBottom;
                            trissArray[triIndex] = (vertIndex);
                            trissArray[triIndex + 1] = (vertIndex + 2);
                            trissArray[triIndex + 2] = (vertIndex + 1);
                            trissArray[triIndex + 3] = (vertIndex);
                            trissArray[triIndex + 4] = (vertIndex + 3);
                            trissArray[triIndex + 5] = (vertIndex + 2);
                            vertIndex += 4;
                            triIndex += 6;
                        }
						
                    }
                }
            }
        }


        Vector3[] verts = new Vector3[vertIndex];
        Vector3[] norms = new Vector3[verts.Length];
        Vector2[] uvs = new Vector2[verts.Length];
        Color[] cols = new Color[verts.Length];
        int[] triss = new int[triIndex];

        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = vertsArray[i];
            norms[i] = normsArray[i];
            uvs[i] = uvsArray[i];
            cols[i] = vertCols[i];
        }

        for (int i = 0; i < triss.Length; i++)
        {
            triss[i] = trissArray[i];
        }

        meshy.vertices = verts;
        meshy.uv = uvs;
        meshy.normals = norms;
        meshy.triangles = triss;

        isReady = true;
    }
	
	
	
	// Old Stuff
	
	/*
	private Vector2 GetTileUV(int x, int y)
	{
		Vector2 uvPos = new Vector2(0 * tileSize, 1 * tileSize);
		Debug.Log(uvPos);
		
		return uvPos;
	}
	
	
    void GenerateFront() 
	{
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
		
		for(int x = 0; x < gridSizeX + 1; x++)
		{
			for(int y = 0; y < gridSizeY + 1; y++)
			{
				verticesList.Add(new Vector3(x, y, 0));
				uvList.Add(GetTileUV(x, y));
			}
		}
		
		mesh.vertices = verticesList.ToArray();
		mesh.uv = uvList.ToArray();
		
		int tempGrid = 0;
		
		if(gridSizeX > gridSizeY)
		{
			tempGrid = gridSizeX;
			gridSizeX = gridSizeY;
			gridSizeY = tempGrid;
		}
		
		
		for(int x = 0; x < gridSizeX; x++)
		{
			for(int y = 0; y < gridSizeY; y++)
			{
				triangleList.Add(x * (gridSizeY + 1) + y);
				triangleList.Add(x * (gridSizeY + 1) + y + 1);
				triangleList.Add(x * (gridSizeY + 1) + y + (gridSizeY + 2));
				
				triangleList.Add(x * (gridSizeY + 1) + y + (gridSizeY + 2));
				triangleList.Add(x * (gridSizeY + 1) + y + (gridSizeY + 1));
				triangleList.Add(x * (gridSizeY + 1) + y);
			}
		}
		
		mesh.triangles = triangleList.ToArray();	
		renderer.material = testMat;
		
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
    }
		*/
	/*
	void Start () 
	{
		List<MeshFilter> meshFilters = new List<MeshFilter>();
		
		foreach(Transform obj in transform)
		{
			if(obj.GetComponent<MeshFilter>())
			{
				meshFilters.Add(obj.GetComponent<MeshFilter>());
			}
		}
		
		//MeshFilter[] meshFilters = gameObject.GetComponentsInChildren(typeof(MeshFilter)) as MeshFilter[];
		CombineInstance[] combine = new CombineInstance[meshFilters.Count];
		
		for (int i = 0; i < meshFilters.Count; i++)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			meshFilters[i].gameObject.active = false;
		}
		
		transform.GetComponent<MeshFilter>().mesh = new Mesh();
		transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
		transform.gameObject.active = true;
		
		gameObject.AddComponent(typeof(BoxCollider));
	}*/
}
