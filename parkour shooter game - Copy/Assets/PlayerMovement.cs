using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.R;
    [Space]

    [Header("References")]
    public Rigidbody rb;
    public Camera cam;
    [Space]

    [Header("Speeds")]
    public float walkspeed;
    public float sprintspeed;
    public float crouchspeed;
    public float wallrunspeed;
    [Space]

    [Header("Layers")]
    public LayerMask whatIsGround;
    [Space]

    public float crouchYScale;
    public float jumpForce, jumpCooldown;
    public float sensitivity;
    public float grav;
    public float playerHeight = 2f;
    public float maxSlopeAngle;
    public float airMultiplier;
    public float groundDrag;

    float verticalInput, horizontalInput;
    float mouseX, mouseY, xRot;
    float startYScale;
    float speed;

    private bool readyToJump = true;    
    private  bool grounded;
    private bool crouchForceAdded = false;
    public bool wallrunning = false;

    RaycastHit slopeHit;
    Vector3 moveVector;

    public Vector3 force;

    public MovementState state;
    public enum MovementState
    {
        crouching,
        walking,
        sprinting,
        wallrunning,
        air
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rb = GetComponent<Rigidbody>();
        startYScale = transform.localScale.y;
    }


    void Update()
    {
        //Inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X") ;
        mouseY = Input.GetAxis("Mouse Y") ;

        //Mouse 
        xRot -= mouseY * sensitivity;

        float speedVar = rb.linearVelocity.magnitude;  
        Debug.Log(OnSlope());
        //Stop Crouch
        if (state != MovementState.crouching)
        {   
            transform.localScale = new Vector3 (transform.localScale.x, startYScale, transform.localScale.z);
            crouchForceAdded = false;
        }

        State();  
    }

    void FixedUpdate()
    {
        //Move Player
        moveVector = transform.TransformDirection(horizontalInput, 0, verticalInput);

        //On Ground
        if (grounded)
        {
            rb.AddForce(moveVector.normalized * speed * 10f, ForceMode.Force);
        }

        //On Slope
        else if (OnSlope())
        {
            rb.linearVelocity = new Vector3 (SlopeDir().x, rb.linearVelocity.y, SlopeDir().z) + force;
        }

        //On Air
        if (grounded)
        {
            rb.AddForce(moveVector.normalized * speed * 10f * airMultiplier, ForceMode.Force);
        }

        if (force != new Vector3 (0f,0f,0f))
        {
            force -= force * 2f * Time.deltaTime;
        }
        if (force.magnitude > -3 && force.magnitude < 3)
        {
            force = new Vector3 (0f,0f,0f);
        }

        //Move Camera
        transform.Rotate(0f, mouseX * sensitivity, 0f);
        cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);

        //Extra Gravity
        rb.AddForce(Vector3.down * grav);
        rb.useGravity = true;

        //Ground Check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.1f, whatIsGround);

        //Jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            Jump();
        }

        //Drag
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }

        else rb.linearDamping = 0;

        //Speed Control
        Vector3 flatVel = new Vector3 (rb.linearVelocity.x, 0f, rb.linearVelocity.z);        
        if (flatVel.magnitude > speed)
        {
            Vector3 limitedVel = flatVel.normalized * speed;
            rb.linearVelocity = new Vector3 (limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }

        State();  
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3 (rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        readyToJump = false;
        Invoke(nameof(JumpReset), jumpCooldown);
    }

    void JumpReset()
    {
        readyToJump = true;
    }

    void State()
    {
        //State
        if (wallrunning)
        {
            state = MovementState.wallrunning;
            speed = wallrunspeed;

        }

        else if (grounded && Input.GetKey(crouchKey))
        {   
            speed = crouchspeed;
            state = MovementState.crouching;
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            if (!crouchForceAdded)
            {
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                crouchForceAdded = true;
            }
        }

        else if (grounded && Input.GetKey(sprintKey))
        {
            speed = sprintspeed;
            state = MovementState.sprinting;
        }

        else if (grounded)
        {
            speed = walkspeed;
            state = MovementState.walking;
        }

        else 
        {
            state = MovementState.air;                                                                                            
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight + 0.4f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 SlopeDir()
    {
        return Vector3.ProjectOnPlane(moveVector, slopeHit.normal).normalized;
    }
}