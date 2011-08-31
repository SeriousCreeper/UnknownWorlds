using UnityEngine;
using System.Collections;

public class VoxeLoxel : MonoBehaviour 
{
	//just used for positioning the player after generating the level, probably shouldnt happen in this script
	private GameObject playerObj;
	
	//we optimize building meshes by not building stuff the camera cant see yet
	private GameObject mainCam;
	
	//how many voxels per chunck (cubed)
	public int cSize = 16;
	//how many chuncks total (cubed)
	public int cCount = 16;
	
	//contains voxel types 0-255
	public byte[] voxelArray;
	
	//contains voxel colours (may need to kill if I want bigger areas and need the memory)
	private byte[] voxelCol_R;
	private byte[] voxelCol_G;
	private byte[] voxelCol_B;
	
	//all voxel meshes
	public GameObject[] voxelModels;
	
	//for stat display
	public int totalBlocks = 0;
	public int meshesUpdated = 0;
	
	//pre-initialise memory needed for building the most complex possible meshes
	private Vector3[] vertsArray = new Vector3[49152];
	private Color[] vertCols = new Color[49152];
	private Vector3[] normsArray = new Vector3[49152];
	private Vector2[] uvsArray = new Vector2[49152];
	private int[] trissArray = new int[73728];
	
	//list of chuncks that have changed and need their meshes rebuilt
	private ArrayList chunckUpdateList = new ArrayList();
	
	//height map texture
	public Texture2D hmTex;
	
	//gradient map texture
	public Texture2D gradMap;
	
	
	//cap how many meshes can be generated per frame
	//(I think there is a limit anyway, which is causing 'blank' meshes to appear)
	public int meshUpdateCap = 10;
	
	// Use this for initialization
	IEnumerator Start () {
		
		playerObj = GameObject.Find("playerObj");
		mainCam = playerObj.transform.Find("Main Camera").gameObject;
		
		//initialize voxel space in memory
		voxelArray = new byte[(cSize*cCount*cSize*cCount*cSize*cCount)];
		voxelCol_R = new byte[(cSize*cCount*cSize*cCount*cSize*cCount)];
		voxelCol_G = new byte[(cSize*cCount*cSize*cCount*cSize*cCount)];
		voxelCol_B = new byte[(cSize*cCount*cSize*cCount*cSize*cCount)];
		
		
		// make GameObjects for each chunck's mesh
		voxelModels = new GameObject[cCount*cCount*cCount];
		for (int x=0; x<cCount; x++){
			for (int y=0; y<cCount; y++){
				for (int z=0; z<cCount; z++){
					GameObject voxelModel = new GameObject();
					voxelModel.transform.position = new Vector3(x*cSize,y*cSize,z*cSize);
					voxelModel.AddComponent<MeshRenderer>();
					voxelModel.renderer.material = renderer.material;
					voxelModel.AddComponent<MeshFilter>();
					voxelModels[( ( (z*cCount) + y )*cCount ) + x] = voxelModel;
				}
			}
			yield return null;
		}
		
		
		//lets start with a gradient coloured, half filled level :)
		//Generate("GRADIENT");
		Generate("SINCOLOUR");
		Generate("HALF");
		
		
	}
	
	// Update is called once per frame
	void Update () {
		if (chunckUpdateList.Count!=0){
            //if(Input.GetMouseButtonDown(1))
            //{
            //    int[] tempInt = LastFreeLineCheck(playerObj.transform.position, Vector3.zero);
				
            //    Debug.Log(tempInt[0] + " | " + tempInt[1] + " | " + tempInt[2]);
            //    SetPoint(new Vector3(tempInt[0], tempInt[1], tempInt[2]), 1);
            //}
			meshesUpdated = 0;
			
			for (int i=0; i<chunckUpdateList.Count; i++){
				int[] thisChunck = (int[])chunckUpdateList[i];
				
				bool buildThis = true;
				
				if (Vector3.Distance(mainCam.transform.position, new Vector3(thisChunck[0]*cSize,thisChunck[1]*cSize,thisChunck[2]*cSize))>200){
					//chunck is out of draw distance, fuck updating it now, do it later
					buildThis = false;
				}else if (Vector3.Dot(mainCam.transform.forward, mainCam.transform.position-new Vector3(thisChunck[0]*cSize,thisChunck[1]*cSize,thisChunck[2]*cSize))>0){
					//cam not even looking that way yet, fuck it.
					buildThis = false;
					if (Vector3.Distance(mainCam.transform.position, new Vector3(thisChunck[0]*cSize,thisChunck[1]*cSize,thisChunck[2]*cSize))<cSize*3){
						//pretty near I guess, the center of the chunck could be behind the cam but the thing still be visible
						//meh, guess we *can* update this or whatever,
						buildThis = true;
					}
				}
				
				if (meshesUpdated>=meshUpdateCap){
					//updated enough chuncks this frame, leave it for now
					buildThis = false;
					continue;
				}
				
				if (buildThis){
					meshesUpdated++;
					BuildChunckMesh((int)thisChunck[0],(int)thisChunck[1],(int)thisChunck[2]);
					chunckUpdateList.RemoveAt(i);
					i--;
				}
			}
			
			//chunckUpdateList = new ArrayList();
		}
	}
	
	//returns the colour of a voxel at (x,y,z)
	Color GetColFromPos(int theX, int theY, int theZ)
	{
		Color color = new Color();
		color.r = ((float)(voxelCol_R[( ( (theZ*cSize*cCount) + theY )*cSize*cCount ) + theX]))/256;
		color.g = ((float)(voxelCol_G[( ( (theZ*cSize*cCount) + theY )*cSize*cCount ) + theX]))/256;
		color.b = ((float)(voxelCol_B[( ( (theZ*cSize*cCount) + theY )*cSize*cCount ) + theX]))/256;
		return color;
	}
	
	//destroy every block along a line
	public void FullLineDestroy(Vector3 startPos, Vector3 endPos){
		ArrayList changedBlocks = new ArrayList();
		float lineLength = Vector3.Distance(startPos,endPos);
		Vector3 lineDirection = startPos-endPos;
		lineDirection.Normalize();
		for (int i=0; i<lineLength; i++){
			int thisX = (int)Mathf.Round( startPos.x + (lineDirection.x*i) );
			int thisY = (int)Mathf.Round( startPos.y + (lineDirection.y*i) );
			int thisZ = (int)Mathf.Round( startPos.z + (lineDirection.z*i) );
			if (isReallyBlockHere(thisX,thisY,thisZ)){
				totalBlocks--;
				voxelArray[( ( (thisZ*cSize*cCount) + thisY )*cSize*cCount ) + thisX] = 0;
				changedBlocks.Add(thisX);
				changedBlocks.Add(thisY);
				changedBlocks.Add(thisZ);
			}
		}
		int[] voxelChanges = new int[changedBlocks.Count];
		for (int i=0; i<changedBlocks.Count; i++){
			voxelChanges[i] = (int)(changedBlocks[i]);
		}
		
		updateChuncks(voxelChanges);
	}
	
	//returns co-ords of blocks that the line touches, in [x1,y1,z1,x2,y2,z2,x3...] format
	public int[] FullLineCheck(Vector3 startPos, Vector3 endPos){
		
		ArrayList blocksHit = new ArrayList();
		float lineLength = Vector3.Distance(startPos,endPos);
		Vector3 lineDirection = startPos-endPos;
		lineDirection.Normalize();
		for (int i=0; i<lineLength; i++){
			int thisX = (int)Mathf.Round( startPos.x + (lineDirection.x*i) );
			int thisY = (int)Mathf.Round( startPos.y + (lineDirection.y*i) );
			int thisZ = (int)Mathf.Round( startPos.z + (lineDirection.z*i) );
			if (isReallyBlockHere(thisX,thisY,thisZ)){
				blocksHit.Add(thisX);
				blocksHit.Add(thisY);
				blocksHit.Add(thisZ);
			}
		}
		
		int[] lineResults = new int[blocksHit.Count];
		for (int i=0; i<lineResults.Length; i++){
			lineResults[i] = (int)blocksHit[i];
		}
		
		return lineResults;
		
	}
	
	//returns co-ords of the first block that the line touches, in [x,y,z] format
	public int[] LineCheck(Vector3 startPos, Vector3 endPos, bool realCheck){
		
		int[] lineResults = new int[3];
		lineResults[0] = -1;
		lineResults[1] = -1;
		lineResults[2] = -1;
		
		float lineLength = Vector3.Distance(startPos,endPos);
		Vector3 lineDirection = startPos-endPos;
		lineDirection.Normalize();
		for (int i=0; i<lineLength; i++){
			int thisX = (int)Mathf.Round( startPos.x + (lineDirection.x*i) );
			int thisY = (int)Mathf.Round( startPos.y + (lineDirection.y*i) );
			int thisZ = (int)Mathf.Round( startPos.z + (lineDirection.z*i) );
			if ((!realCheck && isBlockHere(thisX,thisY,thisZ)) || (realCheck && isReallyBlockHere(thisX,thisY,thisZ))){
				lineResults[0] = thisX;
				lineResults[1] = thisY;
				lineResults[2] = thisZ;
				return lineResults;
			}
		}
		return lineResults;
	}
	
	//returns co-ords of the last empty block that the line passes through before hitting something, in [x,y,z] format
	public int[] LastFreeLineCheck(Vector3 startPos, Vector3 endPos){
		
		int[] lineResults = new int[3];
		lineResults[0] = -1;
		lineResults[1] = -1;
		lineResults[2] = -1;
		
		bool foundIt = false;
		
		float lineLength = Vector3.Distance(startPos,endPos);
		Vector3 lineDirection = startPos-endPos;
		lineDirection.Normalize();
		for (int i=0; i<lineLength; i++){
			int thisX = (int)Mathf.Round( startPos.x + (lineDirection.x*i) );
			int thisY = (int)Mathf.Round( startPos.y + (lineDirection.y*i) );
			int thisZ = (int)Mathf.Round( startPos.z + (lineDirection.z*i) );
			if (isBlockHere(thisX,thisY,thisZ)){
				foundIt = true;
				return lineResults;
			}
			if (!foundIt){
				lineResults[0] = thisX;
				lineResults[1] = thisY;
				lineResults[2] = thisZ;
			}
		}
		if (!foundIt){
			lineResults[0] = -1;
			lineResults[1] = -1;
			lineResults[2] = -1;
		}
		return lineResults;
	}
	
	//returns true if there is a block here, or if the point is outside the horizontal boundries
	public bool isBlockHere(int theX, int theY, int theZ){
		if (theX<0) return true;
		if (theX>=cSize*cCount) return true;
		if (theY<0) return true;
		if (theY>=cSize*cCount) return false; 
		if (theZ<0) return true;
		if (theZ>=cSize*cCount) return true;
		
		return (voxelArray[( ( (theZ*cSize*cCount) + theY )*cSize*cCount ) + theX] == 1);
	}
	
	//returns true if there is a block here
	public bool isReallyBlockHere(int theX, int theY, int theZ){
		if (theX<0) return false;
		if (theX>=cSize*cCount) return false;
		if (theY<0) return false;
		if (theY>=cSize*cCount) return false;
		if (theZ<0) return false;
		if (theZ>=cSize*cCount) return false;
		
		return (voxelArray[( ( (theZ*cSize*cCount) + theY )*cSize*cCount ) + theX] == 1);
	}
	
	//change all voxels withing a radius from a point
	public void Detonate(Vector3 pointy, int detRadius){
		
		ArrayList changedBlocks = new ArrayList();
		int xx = (int)Mathf.Round(pointy.x);
		int yy = (int)Mathf.Round(pointy.y);
		int zz = (int)Mathf.Round(pointy.z);
		
		bool isAdditive = false;
		if (detRadius<0){
			isAdditive = true;
			detRadius*=-1;
		}
		
		int tempIndexInt = 0;
		
		for (int xness = xx-detRadius; xness<xx+detRadius; xness++){
			for (int yness = yy-detRadius; yness<yy+detRadius; yness++){
				for (int zness = zz-detRadius; zness<zz+detRadius; zness++){
					
					tempIndexInt = ( ( (zness*cSize*cCount) + yness )*cSize*cCount ) + xness;
					
					if (xness>-1 && xness<(cCount*cSize)){
						if (yness>-1 && yness<(cCount*cSize)){
							if (zness>-1 && zness<(cCount*cSize)){
								if (Vector3.Distance(new Vector3(xness,yness,zness),new Vector3(xx,yy,zz))<detRadius){
									if (isAdditive){
										if (voxelArray[tempIndexInt]==0){
											totalBlocks++;
										}
										voxelArray[tempIndexInt] = 1;
									}else{
										if (voxelArray[tempIndexInt]==1){
											totalBlocks--;
										}
										voxelArray[tempIndexInt] = 0;
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
		
		//int[] voxelChanges = new int[changedBlocks.Count];
		//for (int i=0; i<changedBlocks.Count; i++){
		//	voxelChanges[i] = (int)(changedBlocks[i]);
		//}
		
		//updateChuncks(voxelChanges);
		updateChuncksFast(changedBlocks);
		
	}
	
	//change colours of all voxels withing a radius from a point
	public void Paint(Vector3 pointy, int detRadius, Color theColor){
		
		ArrayList changedBlocks = new ArrayList();
		int xx = (int)Mathf.Round(pointy.x);
		int yy = (int)Mathf.Round(pointy.y);
		int zz = (int)Mathf.Round(pointy.z);
		
		
		
		for (int xness = xx-detRadius; xness<xx+detRadius; xness++){
			for (int yness = yy-detRadius; yness<yy+detRadius; yness++){
				for (int zness = zz-detRadius; zness<zz+detRadius; zness++){
					if (Vector3.Distance(new Vector3(xness,yness,zness),new Vector3(xx,yy,zz))<detRadius){
						if (isReallyBlockHere(xness,yness,zness)){
							voxelCol_R[( ( (zness*cSize*cCount) + yness )*cSize*cCount ) + xness] = (byte)Mathf.Round(theColor.r*256);
							voxelCol_G[( ( (zness*cSize*cCount) + yness )*cSize*cCount ) + xness] = (byte)Mathf.Round(theColor.g*256);
							voxelCol_B[( ( (zness*cSize*cCount) + yness )*cSize*cCount ) + xness] = (byte)Mathf.Round(theColor.b*256);
							changedBlocks.Add(xness);
							changedBlocks.Add(yness);
							changedBlocks.Add(zness);
						}
					}	
				}
			}
		}
		
		int[] voxelChanges = new int[changedBlocks.Count];
		for (int i=0; i<changedBlocks.Count; i++){
			voxelChanges[i] = (int)(changedBlocks[i]);
		}
		
		updateChuncks(voxelChanges);
		
	}
	
	//if there is a block here, remove it, otherwise add a block
	public void TogglePoint(Vector3 pointy){
		int xx = (int)Mathf.Round(pointy.x);
		int yy = (int)Mathf.Round(pointy.y);
		int zz = (int)Mathf.Round(pointy.z);
		if ( !isReallyBlockHere(xx,yy,zz) ){
			//Add block
			voxelArray[( ( (zz*cSize*cCount) + yy )*cSize*cCount ) + xx]=1;
			totalBlocks++;
		}else{
			//Take Block
			voxelArray[( ( (zz*cSize*cCount) + yy )*cSize*cCount ) + xx]=0;
			totalBlocks--;
		}
		
		int[] changeVoxel = new int[3];
		changeVoxel[0] = xx; changeVoxel[1] = yy; changeVoxel[2] = zz;
		updateChuncks(changeVoxel);
		
	}
	
	//set the voxel at a point to be a specific type of voxel
	public void SetPoint(Vector3 pointy, byte theBlocktype){
		int xx = (int)Mathf.Round(pointy.x);
		int yy = (int)Mathf.Round(pointy.y);
		int zz = (int)Mathf.Round(pointy.z);
		if ( !isReallyBlockHere(xx,yy,zz) ){
			if (voxelArray[( ( (zz*cSize*cCount) + yy )*cSize*cCount ) + xx]!=0 && theBlocktype ==0){
				totalBlocks--;
			}
			if (voxelArray[( ( (zz*cSize*cCount) + yy )*cSize*cCount ) + xx]==0 && theBlocktype !=0){
				totalBlocks++;
			}
			voxelArray[( ( (zz*cSize*cCount) + yy )*cSize*cCount ) + xx]=theBlocktype;
		}
		
		int[] changeVoxel = new int[3];
		changeVoxel[0] = xx; changeVoxel[1] = yy; changeVoxel[2] = zz;
		updateChuncks(changeVoxel);
		
	}
	
	//add chuncks to the chunck update list that need rebuilding
	private void updateChuncks(int[] voxelChanges){
		ArrayList chuncksList = new ArrayList();
		
		for (int i=0; i<voxelChanges.Length; i+=3){
			int[] chuncky = new int[3];
			chuncky[0] = voxelChanges[i]/cSize;
			chuncky[1] = voxelChanges[i+1]/cSize;
			chuncky[2] = voxelChanges[i+2]/cSize;
			chuncksList.Add(chuncky);
			
			//x-1
			chuncky = new int[3];
			chuncky[0] = (voxelChanges[i]-1)/cSize;
			chuncky[1] = voxelChanges[i+1]/cSize;
			chuncky[2] = voxelChanges[i+2]/cSize;
			chuncksList.Add(chuncky);
			
			//x+1
			chuncky = new int[3];
			chuncky[0] = (voxelChanges[i]+1)/cSize;
			chuncky[1] = voxelChanges[i+1]/cSize;
			chuncky[2] = voxelChanges[i+2]/cSize;
			chuncksList.Add(chuncky);
			
			//y-1
			chuncky = new int[3];
			chuncky[0] = voxelChanges[i]/cSize;
			chuncky[1] = (voxelChanges[i+1]-1)/cSize;
			chuncky[2] = voxelChanges[i+2]/cSize;
			chuncksList.Add(chuncky);
			
			//y+1
			chuncky = new int[3];
			chuncky[0] = voxelChanges[i]/cSize;
			chuncky[1] = (voxelChanges[i+1]+1)/cSize;
			chuncky[2] = voxelChanges[i+2]/cSize;
			chuncksList.Add(chuncky);
			
			//z-1
			chuncky = new int[3];
			chuncky[0] = voxelChanges[i]/cSize;
			chuncky[1] = voxelChanges[i+1]/cSize;
			chuncky[2] = (voxelChanges[i+2]-1)/cSize;
			chuncksList.Add(chuncky);
			
			//z+1
			chuncky = new int[3];
			chuncky[0] = voxelChanges[i]/cSize;
			chuncky[1] = voxelChanges[i+1]/cSize;
			chuncky[2] = (voxelChanges[i+2]+1)/cSize;
			chuncksList.Add(chuncky);
		}
		
		
		
		for (int i=0; i<chuncksList.Count; i++){
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
			if (doIt){
				//BuildChunck(buildyChunck[0], buildyChunck[1], buildyChunck[2]);
				//chunckUpdateList.Add(buildyChunck);
				
				bool isDouble = false;
				for (int k=0; k<chunckUpdateList.Count; k++){
					int[] thisChunck = (int[])(chunckUpdateList[k]);
					
					if (thisChunck[0]==buildyChunck[0] && thisChunck[1]==buildyChunck[1] && thisChunck[2]==buildyChunck[2]){
						isDouble=true;
					}
					
					
				}
				if (!isDouble){
					chunckUpdateList.Add(buildyChunck);
				}
			
			}
		}
		
	}
	
	//add chuncks to the chunck update list that need rebuilding (takes arraylist insteat of int[])
	private void updateChuncksFast(ArrayList voxelChanges){
		ArrayList chuncksList = new ArrayList();
		
		int[] voxelNess = new int[3];
		
		for (int i=0; i<voxelChanges.Count; i+=3){
			
			voxelNess[0] = (int)voxelChanges[i];
			voxelNess[1] = (int)voxelChanges[i+1];
			voxelNess[2] = (int)voxelChanges[i+2];
			
			int[] chuncky = new int[3];
			chuncky[0] = voxelNess[0]/cSize;
			chuncky[1] = voxelNess[1]/cSize;
			chuncky[2] = voxelNess[2]/cSize;
			chuncksList.Add(chuncky);
			
			//x-1
			chuncky = new int[3];
			chuncky[0] = (voxelNess[0]-1)/cSize;
			chuncky[1] = voxelNess[1]/cSize;
			chuncky[2] = voxelNess[2]/cSize;
			chuncksList.Add(chuncky);
			
			//x+1
			chuncky = new int[3];
			chuncky[0] = (voxelNess[0]+1)/cSize;
			chuncky[1] = voxelNess[1]/cSize;
			chuncky[2] = voxelNess[2]/cSize;
			chuncksList.Add(chuncky);
			
			//y-1
			chuncky = new int[3];
			chuncky[0] = voxelNess[0]/cSize;
			chuncky[1] = (voxelNess[1]-1)/cSize;
			chuncky[2] = voxelNess[2]/cSize;
			chuncksList.Add(chuncky);
			
			//y+1
			chuncky = new int[3];
			chuncky[0] = voxelNess[0]/cSize;
			chuncky[1] = (voxelNess[1]+1)/cSize;
			chuncky[2] = voxelNess[2]/cSize;
			chuncksList.Add(chuncky);
			
			//z-1
			chuncky = new int[3];
			chuncky[0] = voxelNess[0]/cSize;
			chuncky[1] = voxelNess[1]/cSize;
			chuncky[2] = (voxelNess[2]-1)/cSize;
			chuncksList.Add(chuncky);
			
			//z+1
			chuncky = new int[3];
			chuncky[0] = voxelNess[0]/cSize;
			chuncky[1] = voxelNess[1]/cSize;
			chuncky[2] = (voxelNess[2]+1)/cSize;
			chuncksList.Add(chuncky);
		}
		
		
		
		for (int i=0; i<chuncksList.Count; i++){
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
			if (doIt){
				//BuildChunck(buildyChunck[0], buildyChunck[1], buildyChunck[2]);
				//chunckUpdateList.Add(buildyChunck);
				
				bool isDouble = false;
				for (int k=0; k<chunckUpdateList.Count; k++){
					int[] thisChunck = (int[])(chunckUpdateList[k]);
					
					if (thisChunck[0]==buildyChunck[0] && thisChunck[1]==buildyChunck[1] && thisChunck[2]==buildyChunck[2]){
						isDouble=true;
					}
					
					
				}
				if (!isDouble){
					chunckUpdateList.Add(buildyChunck);
				}
			
			}
		}
		
	}
	
	
	//add a specific chunck to the update list
	void BuildChunck(int chunckX, int chunckY, int chunckZ){
		int[] chuncky = new int[3];
		chuncky[0] = chunckX;
		chuncky[1] = chunckY;
		chuncky[2] = chunckZ;
		chunckUpdateList.Add(chuncky);
	}
	
	//build the mesh for a specific chunck
	void BuildChunckMesh(int chunckX, int chunckY, int chunckZ){
		int cIndex = ( ( (chunckZ*cCount) + chunckY )*cCount ) + chunckX;
		
		if (cIndex>=voxelModels.Length || cIndex<0){
			//if the chunck doesn't exist, we don't gotta build a mesh for it, woo
			return;
		}
		
		int xOffset = chunckX*cSize;
		int yOffset = chunckY*cSize;
		int zOffset = chunckZ*cSize;
		int xOffsetB = -xOffset;
		int yOffsetB = -yOffset;
		int zOffsetB = -zOffset;
		
		Mesh meshy = voxelModels[cIndex].GetComponent<MeshFilter>().mesh;
		meshy.Clear();
		
		int vertIndex = 0;
		int triIndex = 0;
		
		
		for (int buildX=xOffset; buildX<xOffset+cSize; buildX++){
			for (int buildY=yOffset; buildY<yOffset+cSize; buildY++){
				for (int buildZ=zOffset; buildZ<zOffset+cSize; buildZ++){
					
					if ( isReallyBlockHere(buildX,buildY,buildZ) ){
						//found block, now build faces as needed
					
						if (!isReallyBlockHere(buildX-1,buildY,buildZ)){
							//make left face
							vertsArray[vertIndex] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+1] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+2] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+3] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ+0.5f));
							normsArray[vertIndex] = (-Vector3.right);
							normsArray[vertIndex+1] = (-Vector3.right);
							normsArray[vertIndex+2] = (-Vector3.right);
							normsArray[vertIndex+3] = (-Vector3.right);
							uvsArray[vertIndex] = (new Vector2(1,0));
							uvsArray[vertIndex+1] = (new Vector2(1,1));
							uvsArray[vertIndex+2] = (new Vector2(0,1));
							uvsArray[vertIndex+3] = (new Vector2(0,0));
							vertCols[vertIndex] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+1] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+2] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+3] = GetColFromPos(buildX,buildY,buildZ);
							trissArray[triIndex] = (vertIndex);
							trissArray[triIndex+1] = (vertIndex+2);
							trissArray[triIndex+2] = (vertIndex+1);
							trissArray[triIndex+3] = (vertIndex);
							trissArray[triIndex+4] = (vertIndex+3);
							trissArray[triIndex+5] = (vertIndex+2);
							vertIndex+=4;
							triIndex+=6;
						}
						
						if (!isReallyBlockHere(buildX+1,buildY,buildZ)){
							//make right face
							vertsArray[vertIndex] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+1] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+2] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+3] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ+0.5f));
							normsArray[vertIndex] = (Vector3.right);
							normsArray[vertIndex+1] = (Vector3.right);
							normsArray[vertIndex+2] = (Vector3.right);
							normsArray[vertIndex+3] = (Vector3.right);
							
							uvsArray[vertIndex] = (new Vector2(0,0));
							uvsArray[vertIndex+1] = (new Vector2(0,1));
							uvsArray[vertIndex+2] = (new Vector2(1,1));
							uvsArray[vertIndex+3] = (new Vector2(1,0));
							vertCols[vertIndex] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+1] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+2] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+3] = GetColFromPos(buildX,buildY,buildZ);
							
							trissArray[triIndex] = (vertIndex);
							trissArray[triIndex+1] = (vertIndex+1);
							trissArray[triIndex+2] = (vertIndex+2);
							trissArray[triIndex+3] = (vertIndex);
							trissArray[triIndex+4] = (vertIndex+2);
							trissArray[triIndex+5] = (vertIndex+3);
							vertIndex+=4;
							triIndex+=6;
						}
						
						if (!isReallyBlockHere(buildX,buildY-1,buildZ)){
							//make bottom face
							vertsArray[vertIndex] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+1] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+2] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+3] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ+0.5f));
							normsArray[vertIndex] = (-Vector3.up);
							normsArray[vertIndex+1] = (-Vector3.up);
							normsArray[vertIndex+2] = (-Vector3.up);
							normsArray[vertIndex+3] = (-Vector3.up);
							uvsArray[vertIndex] = (new Vector2(0,0));
							uvsArray[vertIndex+1] = (new Vector2(1,0));
							uvsArray[vertIndex+2] = (new Vector2(1,1));
							uvsArray[vertIndex+3] = (new Vector2(0,1));
							vertCols[vertIndex] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+1] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+2] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+3] = GetColFromPos(buildX,buildY,buildZ);
							trissArray[triIndex] = (vertIndex);
							trissArray[triIndex+1] = (vertIndex+1);
							trissArray[triIndex+2] = (vertIndex+2);
							trissArray[triIndex+3] = (vertIndex);
							trissArray[triIndex+4] = (vertIndex+2);
							trissArray[triIndex+5] = (vertIndex+3);
							vertIndex+=4;
							triIndex+=6;
						}
						
						if (!isReallyBlockHere(buildX,buildY+1,buildZ)){
							//make top face
							vertsArray[vertIndex] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+1] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+2] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+3] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ+0.5f));
							normsArray[vertIndex] = (Vector3.up);
							normsArray[vertIndex+1] = (Vector3.up);
							normsArray[vertIndex+2] = (Vector3.up);
							normsArray[vertIndex+3] = (Vector3.up);
							uvsArray[vertIndex] = (new Vector2(0,0));
							uvsArray[vertIndex+1] = (new Vector2(1,0));
							uvsArray[vertIndex+2] = (new Vector2(1,1));
							uvsArray[vertIndex+3] = (new Vector2(0,1));
							vertCols[vertIndex] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+1] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+2] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+3] = GetColFromPos(buildX,buildY,buildZ);
							trissArray[triIndex] = (vertIndex);
							trissArray[triIndex+1] = (vertIndex+2);
							trissArray[triIndex+2] = (vertIndex+1);
							trissArray[triIndex+3] = (vertIndex);
							trissArray[triIndex+4] = (vertIndex+3);
							trissArray[triIndex+5] = (vertIndex+2);
							vertIndex+=4;
							triIndex+=6;
						}
						
						if (!isReallyBlockHere(buildX,buildY,buildZ-1)){
							//make back face
							vertsArray[vertIndex] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+1] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+2] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ-0.5f));
							vertsArray[vertIndex+3] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ-0.5f));
							normsArray[vertIndex] = (-Vector3.forward);
							normsArray[vertIndex+1] = (-Vector3.forward);
							normsArray[vertIndex+2] = (-Vector3.forward);
							normsArray[vertIndex+3] = (-Vector3.forward);
							vertCols[vertIndex] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+1] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+2] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+3] = GetColFromPos(buildX,buildY,buildZ);
							uvsArray[vertIndex] = (new Vector2(0,0));
							uvsArray[vertIndex+1] = (new Vector2(0,1));
							uvsArray[vertIndex+2] = (new Vector2(1,1));
							uvsArray[vertIndex+3] = (new Vector2(1,0));
							trissArray[triIndex] = (vertIndex);
							trissArray[triIndex+1] = (vertIndex+1);
							trissArray[triIndex+2] = (vertIndex+2);
							trissArray[triIndex+3] = (vertIndex);
							trissArray[triIndex+4] = (vertIndex+2);
							trissArray[triIndex+5] = (vertIndex+3);
							vertIndex+=4;
							triIndex+=6;
						}
						
						if (!isReallyBlockHere(buildX,buildY,buildZ+1)){
							//make front face
							vertsArray[vertIndex] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+1] = (new Vector3(xOffsetB+buildX-0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+2] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY+0.5f, zOffsetB+buildZ+0.5f));
							vertsArray[vertIndex+3] = (new Vector3(xOffsetB+buildX+0.5f, yOffsetB+buildY-0.5f, zOffsetB+buildZ+0.5f));
							normsArray[vertIndex] = (Vector3.forward);
							normsArray[vertIndex+1] = (Vector3.forward);
							normsArray[vertIndex+2] = (Vector3.forward);
							normsArray[vertIndex+3] = (Vector3.forward);
							uvsArray[vertIndex] = (new Vector2(1,0));
							uvsArray[vertIndex+1] = (new Vector2(1,1));
							uvsArray[vertIndex+2] = (new Vector2(0,1));
							uvsArray[vertIndex+3] = (new Vector2(0,0));
							vertCols[vertIndex] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+1] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+2] = GetColFromPos(buildX,buildY,buildZ);
							vertCols[vertIndex+3] = GetColFromPos(buildX,buildY,buildZ);
							trissArray[triIndex] = (vertIndex);
							trissArray[triIndex+1] = (vertIndex+2);
							trissArray[triIndex+2] = (vertIndex+1);
							trissArray[triIndex+3] = (vertIndex);
							trissArray[triIndex+4] = (vertIndex+3);
							trissArray[triIndex+5] = (vertIndex+2);
							vertIndex+=4;
							triIndex+=6;
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
		for (int i=0; i<verts.Length;i++){
			verts[i] = vertsArray[i];
			norms[i] = normsArray[i];
			uvs[i] = uvsArray[i];
			cols[i] = vertCols[i];
		}
		for (int i=0; i<triss.Length; i++){
			triss[i] = trissArray[i];
		}
		
		meshy.vertices = verts;
		meshy.normals = norms;
		meshy.triangles = triss;
		meshy.uv = uvs;
		meshy.colors = cols;
		
//		voxelModels[cIndex].AddComponent(typeof(BoxCollider));
	}
	
	
	
	//function to generate a few preset levels/colour schemes
	public void Generate(string levelType){
		
		switch (levelType){
			case "REBUILDALL":
				//just rebuild all chunck meshes, dont change the voxel data
				for (int x=0; x<cCount; x++){
					for (int y=0; y<cCount; y++){
						for (int z=0; z<cCount; z++){
							BuildChunck(x,y,z);
						}
					}
				}
			break;
			case "RANDCOL":
				//colour each voxel randomly
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							Color theCol = new Color(Random.Range(0.01f, 0.95f),Random.Range(0.01f, 0.95f),Random.Range(0.01f, 0.95f));
							//theCol = new Color(1,0,0);
							//voxelColours[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = ColorToInt(theCol.r,theCol.g,theCol.b);
							voxelCol_R[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.r*256);
							voxelCol_G[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.g*256);
							voxelCol_B[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.b*256);
						}
					}
				}
			break;
			case "GRADIENT":
				//colour each voxel according to height on gradient map
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							Color theCol =gradMap.GetPixel( 0 , y );
							//theCol = new Color(1,0,0);
							//voxelColours[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = ColorToInt(theCol.r,theCol.g,theCol.b);
							voxelCol_R[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.r*256);
							voxelCol_G[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.g*256);
							voxelCol_B[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.b*256);
						}
					}
				}
			break;
			case "GREY":
				//colour each voxel grey
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							
							//theCol = new Color(1,0,0);
							//voxelColours[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = ColorToInt(theCol.r,theCol.g,theCol.b);
							voxelCol_R[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(0.9f*256);
							voxelCol_G[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(0.9f*256);
							voxelCol_B[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(0.9f*256);
						}
					}
				}
			break;
			case "SINCOLOUR":
				//colour each voxel based on its position through Sine
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							
							Color theCol = new Color(0,1,1);
							theCol.r = (Mathf.Sin(x*0.2f)*0.5f)+0.5f;
							theCol.g = (Mathf.Sin(y*0.2f)*0.5f)+0.5f;
							theCol.b = (Mathf.Sin(z*0.2f)*0.5f)+0.5f;
							
							if (theCol.r<=0.01f) theCol.r = 0.01f;
							if (theCol.r>=0.99f) theCol.r = 0.99f;
							if (theCol.g<=0.01f) theCol.g = 0.01f;
							if (theCol.g>=0.99f) theCol.g = 0.99f;
							if (theCol.b<=0.01f) theCol.b = 0.01f;
							if (theCol.b>=0.99f) theCol.b = 0.99f;
							
							voxelCol_R[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.r*256);
							voxelCol_G[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.g*256);
							voxelCol_B[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.b*256);
						}
					}
				}
			break;
			case "CHECKER":
				//colour voxels with a checker pattern
				bool isCheck = false;
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							
							Color theCol = new Color(0.9f,0.9f,0.9f);
							
							if (isCheck){
								theCol = new Color(0.6f,0.6f,0.6f);
							}
							//theCol = new Color(1,0,0);
							//voxelColours[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = ColorToInt(theCol.r,theCol.g,theCol.b);
							voxelCol_R[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.r*256);
							voxelCol_G[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.g*256);
							voxelCol_B[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = (byte)Mathf.Round(theCol.b*256);
							isCheck = !isCheck;
						}
						isCheck = !isCheck;
					}
					isCheck = !isCheck;
				}
			break;
			case "CLEAR":
				//clear all voxels
				for (int i=0; i<voxelArray.Length; i++){
					voxelArray[i] = 0;
				}
				for (int x=0; x<cCount; x++){
					for (int y=0; y<cCount; y++){
						for (int z=0; z<cCount; z++){
							BuildChunck(x,y,z);
						}
					}
				}
				totalBlocks=0;
				playerObj.transform.position = new Vector3(40,90,40);
			break;
			case "HALF":
				//create a level with voxels filled up halfway
				totalBlocks=0;
				for (int i=0; i<voxelArray.Length; i++){
					voxelArray[i] = 0;
				}
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<(cCount*cSize)/2; y++){
						for (int z=0; z<cCount*cSize; z++){
							totalBlocks++;
							voxelArray[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = 1;
						}
					}
				}
				for (int x=0; x<cCount; x++){
					for (int y=0; y<cCount; y++){
						for (int z=0; z<cCount; z++){
							BuildChunck(x,y,z);
						}
					}
				}
				playerObj.transform.position = new Vector3((cCount*cSize)/2,(cCount*cSize)+10,(cCount*cSize)/2);
			break;
			case "HM":
				//generate from heightmap
				totalBlocks = 0;
				for (int i=0; i<voxelArray.Length; i++){
					voxelArray[i] = 0;
				}
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							//if (z==0) print(  );
							if (hmTex.GetPixel( (int)(Mathf.Round((((float)x)/(cCount*cSize))*hmTex.width)) , (int)(Mathf.Round((((float)z)/(cCount*cSize))*hmTex.width)) ).r*(cCount*cSize)>y){
								totalBlocks++;
								voxelArray[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = 1;
							}
						}
					}
				}
				for (int x=0; x<cCount; x++){
					for (int y=0; y<cCount; y++){
						for (int z=0; z<cCount; z++){
							BuildChunck(x,y,z);
						}
					}
				}
				playerObj.transform.position = new Vector3((cCount*cSize)/2,(cCount*cSize)+10,(cCount*cSize)/2);
			break;
			case "HM12":
				//generate from heightmap, at half height scale
				totalBlocks = 0;
				for (int i=0; i<voxelArray.Length; i++){
					voxelArray[i] = 0;
				}
				for (int x=0; x<cCount*cSize; x++){
					for (int y=0; y<cCount*cSize; y++){
						for (int z=0; z<cCount*cSize; z++){
							//if (z==0) print(  );
							if (hmTex.GetPixel( (int)(Mathf.Round((((float)x)/(cCount*cSize))*hmTex.width)) , (int)(Mathf.Round((((float)z)/(cCount*cSize))*hmTex.width)) ).r*((cCount*cSize)/2)>y){
								totalBlocks++;
								voxelArray[( ( (z*cSize*cCount) + y )*cSize*cCount ) + x] = 1;
							}
						}
					}
				}
				for (int x=0; x<cCount; x++){
					for (int y=0; y<cCount; y++){
						for (int z=0; z<cCount; z++){
							BuildChunck(x,y,z);
						}
					}
				}
				playerObj.transform.position = new Vector3((cCount*cSize)/2,(cCount*cSize)+10,(cCount*cSize)/2);
			break;
			case "Fill":
				//fill the level with voxels
				totalBlocks = voxelArray.Length;
				for (int i=0; i<voxelArray.Length; i++){
					voxelArray[i] = 1;
				}
				for (int x=0; x<cCount; x++){
					for (int y=0; y<cCount; y++){
						for (int z=0; z<cCount; z++){
							BuildChunck(x,y,z);
						}
					}
				}
				playerObj.transform.position = new Vector3((cCount*cSize)/2,(cCount*cSize)+10,(cCount*cSize)/2);
			break;
		}
		
		
	}
	
	
}
