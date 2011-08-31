using UnityEngine;
using System.Collections;

public class generateWalls : MonoBehaviour 
{
	public Transform wallGen;
	
	IEnumerator Start()
	{
		int amount = 5;
		while(amount > 0)
		{
			amount--;
			Instantiate(wallGen);
			yield return new WaitForSeconds(0.5f);
		}
	}
}
