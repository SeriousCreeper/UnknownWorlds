using UnityEngine;
using System.Collections;

public class generateWorld : MonoBehaviour 
{
    public Vector3 worldSize;
    public int[,,] worldIDs;
    public Transform[,,] worldCubes;
    public Transform cubePrefab;
    public int scanDist = 100;

    public static generateWorld _main;

    private bool doneCreating = false;


    IEnumerator Start()
    {
        _main = this;

        worldIDs = new int[(int)worldSize.x, (int)worldSize.y, (int)worldSize.z];
        worldCubes = new Transform[(int)worldSize.x, (int)worldSize.y, (int)worldSize.z];

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    worldIDs[x, y, z] = 1;
                    Transform clone = Instantiate(cubePrefab, new Vector3(x, z, y), Quaternion.identity) as Transform;
					
                    worldCubes[x, y, z] = clone;
                }         
            }

            Debug.Log(x*(worldSize.y*worldSize.z) + "/" + worldSize.x*worldSize.y*worldSize.z);
			
            yield return null;
        }
		
        Debug.Log(worldIDs.Length);
		
        doneCreating = true;
    }


    public void RePaint(Vector3 pos)
    {
        if (!doneCreating)
            return;

        for (int x = 0; x < worldSize.x; x++)
        {
            for (int y = 0; y < worldSize.y; y++)
            {
                for (int z = 0; z < Random.Range(1, worldSize.z); z++)
                {
                    if (worldIDs[x, y, z] != 0 && worldCubes[x, y, z] != null)
                    {
                        if(Vector3.Distance(worldCubes[x, y, z].position, pos) < scanDist)
                            worldCubes[x, y, z].renderer.enabled = true;
                        else
                            worldCubes[x, y, z].renderer.enabled = false;
                    }
                }
            }
        }
    }
}
