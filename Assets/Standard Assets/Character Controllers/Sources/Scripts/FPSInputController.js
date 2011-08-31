private var motor : CharacterMotor;

    public var extractCube : Transform;
    public var crossHair : Texture2D;

    private var voxScript;

// Use this for initialization
function Awake () 
{
	motor = GetComponent(CharacterMotor);
}

// Update is called once per frame
function Update () {

	// Get the input vector from kayboard or analog stick
	var directionVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
	
	if (directionVector != Vector3.zero) {
		// Get the length of the directon vector and then normalize it
		// Dividing by the length is cheaper than normalizing when we already have the length anyway
		var directionLength = directionVector.magnitude;
		directionVector = directionVector / directionLength;
		
		// Make sure the length is no bigger than 1
		directionLength = Mathf.Min(1, directionLength);
		
		// Make the input vector more sensitive towards the extremes and less sensitive in the middle
		// This makes it easier to control slow speeds when using analog sticks
		directionLength = directionLength * directionLength;
		
		// Multiply the normalized direction vector by the modified length
		directionVector = directionVector * directionLength;
	}
	
	
		theTakenSpace = voxScript.LineCheck(Camera.main.transform.position, Camera.main.transform.position + (Camera.main.transform.forward * -5), true);
		xCubePos = new Vector3(theTakenSpace[0], theTakenSpace[1], theTakenSpace[2]);


        if (theTakenSpace[0] != -1)
        {
            if (Vector3.Distance(xCubePos, Camera.main.transform.position - Vector3.up) < 4.5f)
            {
                // Place Stone
                if (Input.GetMouseButtonDown(0) && Vector3.Distance(xCubePos, Camera.main.transform.position - Vector3.up) > 1)
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
                voxScript.Detonate(xCubePos, -10);
            }
		}
        else
        {
			extractCube.renderer.enabled = false;
		}

	
	// Apply the direction to the CharacterMotor
	motor.inputMoveDirection = transform.rotation * directionVector;
	motor.inputJump = Input.GetButton("Jump");
}

// Require a character controller to be attached to the same game object
@script RequireComponent (CharacterMotor)
@script AddComponentMenu ("Character/FPS Input Controller")
