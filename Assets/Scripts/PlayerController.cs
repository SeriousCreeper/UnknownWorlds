using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
	// Controls how the player moves, originally based on FPSWalker
	
	public float speed = 6.0f;
	public float jumpSpeed = 8.0f;
	public float gravity = 20.0f;
	
	private Vector3 moveDir = Vector3.zero;
	private bool grounded = false;
	
	public Transform selectCube;

    private bool useGravity = false;

	
	void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            transform.position = new Vector3(transform.position.x, 100, transform.position.z);
		
		Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2)); 
        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(ray, out hit, 10.0f))
        {
            Vector3 hp = hit.point + 0.0001f * ray.direction;

            int xHit = Mathf.CeilToInt(hp.x) - 1;
            int yHit = Mathf.CeilToInt(hp.y) - 1;
            int zHit = Mathf.CeilToInt(hp.z) - 1;
			
			selectCube.position = new Vector3(xHit + 0.5f, yHit + 0.5f, zHit + 0.5f);
			
			if(hit.distance > 4)
				selectCube.renderer.enabled = false;
			else
			{
				selectCube.renderer.enabled = true;
				
				if(Input.GetMouseButtonDown(1))
				{
					TerrainBrain.m_instance.PlaceBlock(xHit, yHit, zHit);
				}
				
				if(Input.GetMouseButtonDown(0))
				{
					TerrainBrain.m_instance.RemoveBlock(xHit, yHit, zHit);
				}
			}
		}
		else
		{
			selectCube.renderer.enabled = false;
		}
	}


    void StartGravity()
    {
		Debug.Log("Use Gravity!");
        useGravity = true;
    }

	
	void FixedUpdate()
	{
		if (grounded)
		{
			moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			moveDir = Quaternion.AngleAxis(transform.localEulerAngles.y, Vector3.up) * moveDir;
			//moveDir = transform.TransformDirection(moveDir);
			moveDir *= speed;
			
			if (Input.GetButton("Jump"))
			{
				moveDir.y = jumpSpeed;
			}
		}
		
        if(useGravity)
		    moveDir.y -= gravity*Time.deltaTime;
		
		CharacterController controller = GetComponent<CharacterController>();
		CollisionFlags flags = controller.Move(moveDir*Time.deltaTime);
		grounded = (flags & CollisionFlags.CollidedBelow) != 0;
	}
}
