using UnityEngine;
using System.Collections;

public class playerControl : MonoBehaviour
{
    public Transform placeCube;
    public Transform extractCube;
    public Texture2D crossHair;

    private Vector3 voxelPos;
    private int[] voxelCoords;
    private Vector3 moveDir;
    private float gravity;
    private World voxScript;

    bool blockLeft = false;
    bool blockRight = false;
    bool blockForward = false;
    bool blockBackward = false;


    void Start()
    {
        voxScript = World._main;

        transform.position = new Vector3(8 * voxScript.gridSizeX, 8 * voxScript.gridSizeY + 2, 8 * voxScript.gridSizeZ);
    }


    void OnGUI()
    {
        GUI.DrawTexture(new Rect(Screen.width / 2 - 2, Screen.height / 2 - 2, 4, 4), crossHair);
    }


    void Update()
    {
        rigidbody.useGravity = voxScript.isReady;

        if (!voxScript.isReallyBlockHere((int)(transform.position.x + 0.5f), (int)(transform.position.y - 0.6f), (int)(transform.position.z + 0.5f)))
        {
            gravity -= 0.5f;
        }
        else if (Input.GetButtonDown("Jump"))
        {
            rigidbody.AddForce(Vector3.up * 800);
        }
        else
        {
            gravity = 0;
        }


        gravity = Mathf.Clamp(gravity, -5, 20);


		int[] theTakenSpace = voxScript.LineCheck(Camera.main.transform.position, Camera.main.transform.position + (Camera.main.transform.forward * -5), true);
		Vector3 xCubePos = new Vector3(theTakenSpace[0], theTakenSpace[1], theTakenSpace[2]);


        if (theTakenSpace[0] != -1)
        {
            if (Vector3.Distance(xCubePos, Camera.main.transform.position - Vector3.up) < 4.5f)
            {
                // Place Stone
                if (Input.GetMouseButtonDown(0) && Vector3.Distance(xCubePos, Camera.main.transform.position - Vector3.up) > 1.5f)
                {
                    if (true)
                    {
                        voxScript.SetPoint(xCubePos + new Vector3(theTakenSpace[3], theTakenSpace[4], theTakenSpace[5]), 3);
                    }
                }

                // Delete Stone
                if (Input.GetMouseButtonDown(1))
                {
                    if (true)
                    {
                        voxScript.TogglePoint(xCubePos);
                    }
                }

                extractCube.transform.position = xCubePos;
                extractCube.renderer.enabled = true;
            }
            else
            {
                extractCube.renderer.enabled = false;
            }

            // Create Bomb
            if (Input.GetKeyDown(KeyCode.B))
            {
                voxScript.Detonate(xCubePos, 3);
            }

            // Create Sphere
            if (Input.GetKeyDown(KeyCode.F))
            {
                voxScript.Detonate(xCubePos, -2);
            }
		}
        else
        {
			extractCube.renderer.enabled = false;
		}


        voxScript.CreateColliders(transform.position, 2);
     }


    // These variables are for adjusting in the inspector how the object behaves 
    public float maxSpeed  = 7;
    public float force     = 8;
    public float jumpSpeed = 5;
 
    // These variables are there for use by the script and don't need to be edited
    private int state = 0;
    private bool grounded = false;
    private float jumpLimit = 0;
 
    // Don't let the Physics Engine rotate this physics object so it doesn't fall over when running
    void Awake ()
    { 
        rigidbody.freezeRotation = true;
    }
 
    // This part detects whether or not the object is grounded and stores it in a variable
    void OnCollisionEnter ()
    {
        state ++;
        if(state > 0)
        {
            grounded = true;
        }
    }
 
 
    void OnCollisionExit ()
    {
        state --;
        if(state < 1)
        {
            grounded = false;
            state = 0;
        }
    }


    public virtual bool jump
    {
        get 
        {
            return Input.GetButtonDown ("Jump");
        }
    }
 
    // This is called every physics frame
    void FixedUpdate ()
    {
        if (Input.GetKey(KeyCode.LeftShift) && grounded)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.useGravity = false;
        }
        else
        {
            rigidbody.useGravity = true;
        }

        // If the object is grounded and isn't moving at the max speed or higher apply force to move it
        if(rigidbody.velocity.magnitude < maxSpeed && grounded == true)
        {
            moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            transform.Translate(moveDir * 5 * Time.deltaTime);
        }
 
        // This part is for jumping. I only let jump force be applied every 10 physics frames so
        // the player can't somehow get a huge velocity due to multiple jumps in a very short time
        if(jumpLimit < 10) jumpLimit ++;
        Debug.Log(grounded);
        if(jump && grounded  && jumpLimit >= 10)
        {
            rigidbody.velocity = (Vector3.up * jumpSpeed);
            jumpLimit = 0;
        }
     }
}
