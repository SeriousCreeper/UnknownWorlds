using UnityEngine;
using System.Collections;

public class driller : MonoBehaviour 
{
	void Update()
	{
		transform.Translate(Vector3.up * -2 * Time.deltaTime);
		
		World._main.Detonate(transform.position, 2);
	}
}
