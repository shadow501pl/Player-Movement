using UnityEngine;

public class ApexMovement : MonoBehaviour
{
    private CharacterController _controller;
    private bool isGrounded;
    public Transform GroundAnchor;
    public LayerMask GroundMask;
    public LayerMask WallMask;
    private bool isClimbing = false;
    private bool hasClimbed;
    private float speed;
    Vector3 MoveVel;
    Vector3 Vel;
    private Vector2 Camrot;
    public float Gravity;
    public float Friction;
    public float PlayerClimbSpeed;
    public float PlayerClimbSpeedY;
    public float ClimbYvelBonus;
    public float RunSpeed;
    public float MoveSpeed;
    private bool isRunning;
    private float ClimbTimer;
    public float JumpHeight;
    public float InAirFriction;
    private GameObject cam;
    public Vector2 mouseSpeed;
    public float AdditionalGravity = 0.5f;
    private float Jumps = 2f;
    public float InAirVelFriction;
    public float WallJump = 15;
    private bool isWallrunning;
    private Vector3 ForwardDirection;
    public float WallRunSpeed;
    public Vector3 wallNormal;
    public float WallJumpOffForce;
    private Vector3 lastWall;
    private float WallRunTimer;
    public float MaxWallRunTime;
    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        cam = Camera.main.gameObject;
    }
    
    private void Update()
    {
        Cursor.lockState = CursorLockMode.Locked;
        MouseRotation();
        CheckGround();
        CheckWallRun();
        if(isWallrunning) WallRunMovement();
        if(!isClimbing && !isWallrunning) Vel.y += Gravity * Time.deltaTime;
        if (!isGrounded && Vel.y > 0 && !isClimbing && !isWallrunning) Vel.y += Gravity * AdditionalGravity * Time.deltaTime;
        if (isGrounded && Vel.y < -2) Vel.y = -2f;
        //Velocity Friction
        {
            if (isGrounded)
            {
                Vel -= Vel * Friction * Time.deltaTime;
                MoveVel -= MoveVel * Friction * Time.deltaTime;
            }
            else
            {
                Vector3 newvel = Vel;
                newvel.y = 0f;
                Vel -= newvel * InAirVelFriction * Time.deltaTime;
                MoveVel -= MoveVel * InAirFriction * Time.deltaTime;
            }
        }
        isRunning = Input.GetKey(KeyCode.LeftShift) && isGrounded;
        speed = isRunning ? RunSpeed : MoveSpeed;
        CheckClimbing();
        if (Input.GetKeyDown(KeyCode.Space) && Jumps > 0)
        {
            if (isClimbing)
            {
                Vel.y = Mathf.Sqrt(JumpHeight * -1f * Gravity);
                Vel += -transform.forward * WallJump;
                isClimbing = false;
                Jumps = 1f;
                ClimbTimer = 1.25f;
            }
            else if (isWallrunning)
            {
                Vel.y = Mathf.Sqrt(JumpHeight * -1f * Gravity);
                ExitWallRun();
                Vel += (transform.forward * 0.2f + wallNormal) * WallJumpOffForce;
                Jumps = 1f;
                
            }
            else
            {
                Vel.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                Jumps--;
                ClimbTimer = 1.25f;
            }

            WallRunTimer = MaxWallRunTime;
        }
        
        
        if(isClimbing) ClimbMovement(); else Movement();
        _controller.Move(Vel * Time.deltaTime);
        _controller.Move(MoveVel * Time.deltaTime);
        if (isGrounded) Jumps = 2f;
        else
        {
            if (Jumps == 2) Jumps = 1f;
        }
    }

    
    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(GroundAnchor.position, 0.1f, GroundMask);
        if (isGrounded)
        {
            isClimbing = false;
            ClimbTimer = 1.25f;
            WallRunTimer = MaxWallRunTime;
        }
    }
    
    void Movement()
    {
        Vector3 pos = transform.forward * Input.GetAxisRaw("Vertical") + transform.right * Input.GetAxisRaw("Horizontal");
        Vector3 calc = pos.normalized * Time.deltaTime * speed;
        if (!isGrounded)
        {
            calc *= 0.6f;
        }
        MoveVel += calc;
    }
    void CheckClimbing()
    {
        RaycastHit wallHit;
        bool canClimb = Physics.Raycast(transform.position, transform.forward, out wallHit, .7f, WallMask);
        float wallAngle = Vector3.Angle(-wallHit.normal, transform.forward);
        if (wallAngle < 15 && canClimb && !hasClimbed && ClimbTimer > 0f)
        {
            isClimbing = true;
            Jumps = 1f;
        }
        else
        {
            if (isClimbing)
            {
                Vel.y = ClimbYvelBonus;
                ClimbTimer = 0f;
            }
            isClimbing = false;
        }
    }
    void CheckWallRun()
    {
        RaycastHit rightWallHit;
        RaycastHit leftWallHit;
        bool onRightWall = Physics.Raycast(transform.position, transform.right, out rightWallHit, 0.7f, WallMask);
        bool onLeftWall = Physics.Raycast(transform.position, -transform.right, out leftWallHit, 0.7f, WallMask);
        
        if ((onRightWall || onLeftWall) && !isWallrunning)
        {
            wallNormal = onRightWall ? rightWallHit.normal : leftWallHit.normal;
            WallRun();
        }
        else if (!onRightWall && !onLeftWall && isWallrunning || (WallRunTimer <= 0f && isWallrunning))
        {
            ExitWallRun();
        }
    }
    void WallRun()
    {
        isWallrunning = true;
        Jumps = 1;
        Vel.y = 0f;
        

        ForwardDirection = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(ForwardDirection, transform.forward) < 0)
        {
            ForwardDirection = -ForwardDirection;
        }
    }
    void WallRunMovement()
    {
        WallRunTimer -= Time.deltaTime;
        Vel += ForwardDirection * WallRunSpeed * Input.GetAxisRaw("Vertical") * Time.deltaTime;
    }
    void ExitWallRun()
    {
        isWallrunning = false;
        lastWall = wallNormal;
        Vel *= 1.1f;
    }
    void ClimbMovement()
    {
        Vector3 pos;
        if (Input.GetAxisRaw("Vertical") > 0)
        {
            Vel.y = PlayerClimbSpeedY;
            pos = transform.right * Input.GetAxisRaw("Horizontal");
        }
        else
        {
            pos = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
        }
        
        MoveVel += pos * Time.deltaTime * PlayerClimbSpeed;
        ClimbTimer -= Time.deltaTime;
    }
    
    private void MouseRotation()
    {
        Camrot = Camrot + new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSpeed;
        Camrot.y = Mathf.Clamp(Camrot.y, -90, 90);
        
        transform.rotation = Quaternion.Euler(0f, Camrot.x, 0f);
        cam.transform.parent.localRotation = Quaternion.Euler(Camrot.y, 0f, 0f);
    }
}
