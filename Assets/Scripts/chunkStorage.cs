using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class chunkStorage : MonoBehaviour
{
    public byte[] myVoxels = new byte[4096];
    public bool isReady = false;


    void Awake()
    {
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                for (int z = 0; z < 16; z++)
                    myVoxels[(((z * 16) + y) * 16) + x] = 3;

        isReady = true;
    }
}