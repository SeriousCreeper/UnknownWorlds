using UnityEngine;
using System.Collections;

public class StatusUpdater : MonoBehaviour {
	
	public GUIText status;
	public float updateInterval = 0.5f;
	
	private bool running = false;
	private int frames = 0;
	private float lastInterval;

	
	
	void Start()
	{
	}

	// Update is called once per frame
	void Update () 
	{
		++frames;
		
		if (running)
		{
			float now = Time.realtimeSinceStartup;
			
			if (now > lastInterval + updateInterval)
			{
				status.text = (frames / (now - lastInterval)).ToString("f2");
				frames = 0;
				lastInterval = now;
			}
		}
	}
	
	
	void startRunningFPS()
	{
		running = true;
		lastInterval = Time.realtimeSinceStartup;
	}
}
