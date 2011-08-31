using UnityEngine;
using System.Collections;

public class previewStone : MonoBehaviour 
{
	private byte lastBlockType = 255;
	
	void Update()
	{
        if (lastBlockType != TerrainBrain.Instance().currentBlockType)
		{
			lastBlockType = TerrainBrain.Instance().currentBlockType;
			
			float x = (float)lastBlockType - 1;
			float y = 16f;
			float tileSize = 1/16f;

			while (x >= 16)
			{
				x -= 16;
				y--;
			}

			renderer.material.SetTextureScale("_MainTex", new Vector2(tileSize, tileSize));
			renderer.material.SetTextureOffset("_MainTex", new Vector2(Mathf.Abs(x * tileSize), tileSize * (y - 1f)));
		}
	}
}
