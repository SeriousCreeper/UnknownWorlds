var prefab : GameObject;

function Start()
{
	for (var y = -10; y < 10; y++)
	{
		for (var x = -10; x < 10; x++)
		{
			for (var z = -10; z <= 0; z++)
				Instantiate(prefab, Vector3(x, z, y), Quaternion.identity);
		}
	}
}
