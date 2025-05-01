using System.Drawing;
using UnityEditor.Experimental.GraphView;
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
    [HideInInspector] public bool exitingWallrun = false;

    public float slideDrag;
    public float slideForce;
    public float slopeSlideForce;
    bool slideForceAdded = false;

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

    Vector3 slideDir;


    void Update()
    {
        WallCheck();      
    }

    void FixedUpdate()
    {   
        StateMachineSlide(); 
        StateMachineWallrun();

        if (pm.sliding)
        {
            SlidingMovement();        
        }
        
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

    void StateMachineWallrun()
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
        Vector3 forceToApply = transform.up * walljumpUpForce + wallNormal * walljumpSideForce;

        rb.linearVelocity = new Vector3 (rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce (forceToApply, ForceMode.Impulse);
    }

    void StateMachineSlide()
    {
        //State 1 - Sliding
        if (!AboveGround() && Input.GetKey(KeyCode.R))  
        {
            if (!pm.sliding)
                //Start Slide
                slideDir = player.forward * verticalInput + player.right * horizontalInput;
                transform.localScale = new Vector3(transform.localScale.x, pm.crouchYScale, transform.localScale.z);
                pm.sliding = true;                
        }

        else 
        {
            if (pm.sliding)
                //Stop Slide
                pm.sliding = false;
                slideForceAdded = false;
        }
    }

    void SlidingMovement()
    {
        
        if (!pm.OnSlope() || rb.linearVelocity.y > 0)
        {
            rb.linearDamping = slideDrag;
            if (!slideForceAdded)
            {
                rb.AddForce(slideDir.normalized * slideForce, ForceMode.Impulse);   
                slideForceAdded = true;            
            }
        }

        else {
            Vector3 inputDir = player.forward * verticalInput + player.right * horizontalInput;
            rb.AddForce(pm.SlopeDir(inputDir).normalized * slopeSlideForce, ForceMode.Force);
        }
    }
}


