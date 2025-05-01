using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

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
    public float slideSpeed;
    [Space]

    [Header("Layers")]
    public LayerMask whatIsGround;
    [Space]

    [Header("Drags")]
    public float groundDrag;
    public float airDrag;
    [Space]

    public float crouchYScale;
    public float jumpForce, jumpCooldown;
    public float sensitivity;
    public float grav;
    public float playerHeight = 2f;
    public float maxSlopeAngle;
    public float airMultiplier;
    public float downForce;

    public TextMeshProUGUI text;

    float desiredMoveSpeed;
    float lastDesiredMoveSpeed;
    float verticalInput, horizontalInput;
    float mouseX, mouseY, xRot;
    float startYScale;
    [HideInInspector] public float speed;

    bool readyToJump = true;    
    bool grounded;
    bool crouchForceAdded = false;
    [HideInInspector] public bool wallrunning = false;
    [HideInInspector] public bool sliding = false;

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
        sliding,
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
        if (Input.GetKey(KeyCode.M))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        //Inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        //Mouse 
        xRot -= mouseY * sensitivity;

        Vector3 speedVar = new Vector3 (rb.linearVelocity.x, 0 , rb.linearVelocity.z);  
        text.SetText("speed: " + speedVar.magnitude.ToString("0"));

        //Stop Crouch
        if (state != MovementState.crouching && state != MovementState.sliding)
        {   
            transform.localScale = new Vector3 (transform.localScale.x, startYScale, transform.localScale.z);
            crouchForceAdded = false;
        }

        State();  
    }

    void FixedUpdate()
    {
        if (!sliding)
        {
            //Move Player
            moveVector = transform.TransformDirection(horizontalInput, 0, verticalInput);


            //On Ground
            if (!OnSlope() && grounded)
            {
                rb.AddForce(moveVector.normalized * speed * 10f, ForceMode.Force);
            }

            //On Slope
            else if (OnSlope() && grounded)
            {
                rb.AddForce(SlopeDir(moveVector).normalized * speed * 10f, ForceMode.Force);
            }

            //On Air
            else if (!grounded)
            {
                rb.AddForce(moveVector.normalized * speed * 10f * airMultiplier, ForceMode.Force);
            }
        }

        //Move Camera
        transform.Rotate(0f, mouseX * sensitivity, 0f);
        cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);

        //Extra Gravity
        rb.AddForce(Vector3.down * grav);
        rb.useGravity = true;

        //Ground Check
        grounded = Physics.CheckSphere(transform.position - new Vector3 (0,1,0), 0.4f, whatIsGround);

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

        else rb.linearDamping = airDrag;

        //Speed Control
        Vector3 flatVel = new Vector3 (rb.linearVelocity.x, 0f, rb.linearVelocity.z);        
        if (!sliding)
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
            desiredMoveSpeed = wallrunspeed;

        }

        else  if (sliding)
        {
            state = MovementState.sliding;
            if (OnSlope() && rb.linearVelocity.y < 0.1f)
            {
                desiredMoveSpeed = slideSpeed;
            }
            else
            {
                desiredMoveSpeed = sprintspeed;
            }
        }

        else if (grounded && Input.GetKey(crouchKey))
        {   
            desiredMoveSpeed = crouchspeed;
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
            desiredMoveSpeed = sprintspeed;
            state = MovementState.sprinting;
        }

        else if (grounded)
        {
            desiredMoveSpeed = walkspeed;
            state = MovementState.walking;
        }

        else 
        {
            state = MovementState.air;    
            if (rb.linearVelocity.y < 0f)    
            {
                rb.AddForce(Vector3.down * downForce, ForceMode.Force);
            }                                                                                    
        }      

        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && speed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpSpeed());
        }

        else
        {
            speed = desiredMoveSpeed;
        }


        lastDesiredMoveSpeed = desiredMoveSpeed;  
    }

    private IEnumerator SmoothlyLerpSpeed()
    {
        Debug.Log("coroutine started");
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - speed);
        float startValue = speed;

        while (time < difference)
        {
            speed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            time += Time.deltaTime;
            yield return null;
        }

        speed = desiredMoveSpeed;
    }    

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight + 0.5f, whatIsGround))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }

            else return false;
        }

        return false;
    }

    [HideInInspector] public Vector3 SlopeDir(Vector3 dir)
    {
        return Vector3.ProjectOnPlane(dir, slopeHit.normal).normalized;
    }
}