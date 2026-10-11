using UnityEngine;

public enum MovementState
{
    Grounded,
    Airborne,
    HorizontalWallRun,
    VerticalWallRun
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Base Movement")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float gravity = -22f;

    [Header("Horizontal Wall Run (Titanfall)")]
    [SerializeField] private float horizontalWallSpeed = 14f;
    [SerializeField] private float horizontalWallGravity = -1f;
    [SerializeField] private float wallJumpSideForce = 10f;
    [SerializeField] private float wallJumpUpForce = 8f;

    [Header("Vertical Wall Run (Mirror's Edge)")]
    [SerializeField] private float verticalClimbSpeed = 10f;    // Speed going straight UP
    [SerializeField] private float maxVerticalClimbTime = 1.5f; // Climb duration limit
    [SerializeField] private float wallKickBackForce = 8f;      // Push backward from wall
    [SerializeField] private float wallKickUpForce = 10f;       // Vertical launch height

    [Header("Detection Settings")]
    [SerializeField] private float wallCheckDistance = 0.9f;
    [SerializeField] private LayerMask wallLayer;

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform orientation;

    // Camera Look & Tilt
    private float xRotation;
    private float yRotation;
    private float currentWallTilt;

    // Physics Engine & State
    private CharacterController controller;
    private Vector3 velocity;
    private MovementState state;

    private float horizontalInput;
    private float verticalInput;

    // Raycast Hits
    private RaycastHit leftWallHit, rightWallHit, frontWallHit;
    private bool hasWallLeft, hasWallRight, hasWallFront;
    private bool isWallRunning;
    private float climbTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        climbTimer = maxVerticalClimbTime;
    }

    private void Update()
    {
        MyInput();
        HandleMouseLook();
        CheckForWalls();
        UpdateMovementState();
        ExecuteMovement();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * 200f;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * 200f;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        // Tilt camera during horizontal wall run only
        float targetTilt = 0f;
        if (state == MovementState.HorizontalWallRun)
        {
            targetTilt = hasWallLeft ? -12f : 12f;
        }
        currentWallTilt = Mathf.Lerp(currentWallTilt, targetTilt, Time.deltaTime * 8f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentWallTilt);
        }

        if (orientation != null)
        {
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void CheckForWalls()
    {
        Vector3 leftDir = (-orientation.right + orientation.forward * 0.4f).normalized;
        Vector3 rightDir = (orientation.right + orientation.forward * 0.4f).normalized;
        Vector3 frontDir = orientation.forward;

        Vector3 highOrigin = transform.position + Vector3.up * 0.6f;
        Vector3 midOrigin = transform.position;
        Vector3 lowOrigin = transform.position - Vector3.up * 0.6f;

        // Front Raycasts (Vertical Wall Run Check)
        hasWallFront = Physics.Raycast(midOrigin, frontDir, out frontWallHit, wallCheckDistance, wallLayer);

        // Side Raycasts (Horizontal Wall Run Check)
        bool leftMid = Physics.Raycast(midOrigin, leftDir, out leftWallHit, wallCheckDistance, wallLayer);
        bool rightMid = Physics.Raycast(midOrigin, rightDir, out rightWallHit, wallCheckDistance, wallLayer);

        hasWallLeft = leftMid;
        hasWallRight = rightMid;
    }

    private void UpdateMovementState()
    {
        bool isHoldingForward = verticalInput > 0;

        if (controller.isGrounded)
        {
            state = MovementState.Grounded;
            isWallRunning = false;
            climbTimer = maxVerticalClimbTime;

            velocity.x = 0f;
            velocity.z = 0f;

            if (velocity.y < 0) velocity.y = -2f;
        }
        // 1. VERTICAL WALL RUN: Facing wall directly + holding W + in air
        else if (hasWallFront && isHoldingForward && climbTimer > 0)
        {
            state = MovementState.VerticalWallRun;
            isWallRunning = true;
            climbTimer -= Time.deltaTime;
        }
        // 2. HORIZONTAL WALL RUN: Side wall + holding W + in air
        else if ((hasWallLeft || hasWallRight) && isHoldingForward && climbTimer > 0)
        {
            if (!isWallRunning) velocity.y = 0f;

            state = MovementState.HorizontalWallRun;
            isWallRunning = true;
            climbTimer -= Time.deltaTime;
        }
        // 3. AIRBORNE
        else
        {
            state = MovementState.Airborne;
            isWallRunning = false;

            velocity.x = Mathf.Lerp(velocity.x, 0f, Time.deltaTime * 3f);
            velocity.z = Mathf.Lerp(velocity.z, 0f, Time.deltaTime * 3f);
        }
    }

    private void ExecuteMovement()
    {
        Vector3 finalMove = Vector3.zero;

        // --- STATE 1: VERTICAL CLIMB (MIRROR'S EDGE) ---
        if (state == MovementState.VerticalWallRun)
        {
            // Move vertically UP along the wall face
            finalMove = Vector3.up * verticalClimbSpeed;
            velocity.y = verticalClimbSpeed;

            // Backwards Kick-Off Jump
            if (Input.GetButtonDown("Jump"))
            {
                Vector3 wallKickDirection = (-orientation.forward * wallKickBackForce) + (Vector3.up * wallKickUpForce);
                velocity = wallKickDirection;
                climbTimer = 0; // End climb
            }
        }
        // --- STATE 2: HORIZONTAL RUN (TITANFALL) ---
        else if (state == MovementState.HorizontalWallRun)
        {
            RaycastHit wallHit = hasWallLeft ? leftWallHit : rightWallHit;

            Vector3 wallDirection = Vector3.ProjectOnPlane(orientation.forward, wallHit.normal).normalized;
            finalMove = wallDirection * horizontalWallSpeed;

            velocity.y += horizontalWallGravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -1.5f);
            finalMove.y = velocity.y;

            // Outward Directional Wall Jump
            if (Input.GetButtonDown("Jump"))
            {
                Vector3 wallJumpDirection = (wallHit.normal * wallJumpSideForce) + (Vector3.up * wallJumpUpForce) + (orientation.forward * 4f);
                velocity = wallJumpDirection;
                climbTimer = 0;
            }
        }
        // --- STATE 3: GROUNDED OR AIRBORNE ---
        else
        {
            Vector3 moveDir = orientation.forward * verticalInput + orientation.right * horizontalInput;
            finalMove = moveDir.normalized * walkSpeed;

            if (Input.GetButtonDown("Jump") && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            finalMove.y = velocity.y;
        }

        controller.Move(finalMove * Time.deltaTime);
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (orientation == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, orientation.forward * wallCheckDistance);
        Gizmos.DrawRay(transform.position, (-orientation.right + orientation.forward * 0.4f).normalized * wallCheckDistance);
        Gizmos.DrawRay(transform.position, (orientation.right + orientation.forward * 0.4f).normalized * wallCheckDistance);
    }
}
