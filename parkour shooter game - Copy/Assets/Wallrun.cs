using UnityEngine;
public class Wallrun : MonoBehaviour
{
    public float maxWallcheckDistance;
    public float wallrunTimer;
    public float minJumpHeight;
    public float wallrunForce;
    public float walljumpUpForce, walljumpSideForce;
    private float wallrunExitTimer;
    public float wallrunExitTime;
    public bool exitingWallrun = false;

    private float horizontalInput;
    private float verticalInput;

    public LayerMask whatIsGround;
    public LayerMask whatIsWall;

    public KeyCode jumpKey = KeyCode.Space;

    private bool wallLeft;
    private bool wallRight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    public Transform player;
    public PlayerMovement pm;
    public Rigidbody rb;


    void Update()
    {
        WallCheck();      
    }

    void FixedUpdate()
    {   
        StateMachine(); 
        
        if (pm.wallrunning)
        {
            WallrunMovement();
            
        }
    }

    void WallCheck()
    {
        wallLeft = Physics.Raycast(player.position, -player.right, out leftWallHit, maxWallcheckDistance, whatIsWall);
        wallRight = Physics.Raycast(player.position, player.right, out rightWallHit, maxWallcheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    void StateMachine()
    {
        //Input
        horizontalInput = Input.GetAxis("Horizontal");    
        verticalInput = Input.GetAxis("Vertical");

        //State 1 - Wallrunning
        if (AboveGround() && verticalInput == 1 && (wallRight || wallLeft) && !exitingWallrun)  
        {
            if (!pm.wallrunning)
                StartWallrun();

            if (Input.GetKey(jumpKey))
            {
                Walljump();
            }
        }

        //State 2 - Exiting
        else if (exitingWallrun)
        {
            if (pm.wallrunning)
            {
                StopWallrun();
            }

            if (wallrunExitTimer > 0)
            {
                wallrunExitTimer -= Time.deltaTime;
            }

            if (wallrunExitTimer <= 0)
            {
                exitingWallrun = false;
            }
        }

        else 
            if (pm.wallrunning)
                StopWallrun();
    }

    void StartWallrun()
    {
        pm.wallrunning = true;
    }

    void WallrunMovement()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3 (rb.linearVelocity.x, 0, rb.linearVelocity.z);

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(Vector3.up, wallNormal);

        if((player.forward - wallForward).magnitude > (player.forward + wallForward).magnitude) {
            wallForward = -wallForward;
        }

        rb.AddForce (wallForward * wallrunForce, ForceMode.Force);
    }

    void StopWallrun()
    {
        pm.wallrunning = false;
    }

    void Walljump()
    {
        exitingWallrun = true;
        wallrunExitTimer = wallrunExitTime;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = transform.up * walljumpUpForce;
        pm.force = wallNormal * walljumpSideForce;
        Debug.DrawRay(transform.position, forceToApply, Color.red);

        rb.linearVelocity = new Vector3 (rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce (forceToApply, ForceMode.Impulse);
    }
}
