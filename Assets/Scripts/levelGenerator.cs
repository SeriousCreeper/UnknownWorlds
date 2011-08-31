using UnityEngine;
using System.Collections;
using System.IO;


public class levelGenerator : MonoBehaviour 
{
    public int levelSizeX = 4096;
    public int levelSizeY = 4096;
    public int levelSizeZ = 8;


    void Start()
    {
        FileStream nrOut = new FileStream("level", FileMode.Create, FileAccess.Write) ;
        BinaryWriter outW = new BinaryWriter(nrOut);

        for (int x = 0; x < levelSizeX; x++)
        {
            for (int y = 0; y < levelSizeY; y++)
            {
                for (int z = 0; z < levelSizeZ; z++)
                {
                    outW.Write(3);
                }
            }

            outW.Write("\n");
        }

        nrOut.Close();
        outW.Close();
    }
}
