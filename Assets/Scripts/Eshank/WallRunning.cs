using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMovement1))]
public class WallRunning : MonoBehaviour
{
    [Header("Wall Running Tuning")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallSprintSpeed = 12f;
    [SerializeField] private float wallSinkSpeed = 2f; // NEW: Controls the downward slant
    [SerializeField] private float maxWallRunTime = 1.5f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Wall Jumping")]
    [SerializeField] private float wallJumpUpForce = 7f;
    [SerializeField] private float wallJumpSideForce = 12f;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float minHeightOffGround = 1.5f;

    [Header("Camera Tilt")]
    [SerializeField] private float camTiltAmount = 15f;
    [SerializeField] private float tiltSpeed = 10f;

    [Header("References")]
    [SerializeField] private Transform orientation;

    private Rigidbody rb;
    private PlayerMovement1 pm;
    private float wallRunTimer;

    // Visual State
    private float currentTilt;

    // State Transitions
    private enum MovementState { Grounded, Airborne, WallRunning }
    private MovementState currentState;

    private bool wallLeft;
    private bool wallRight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    private bool exitingWall;
    private float exitWallTimer;
    private readonly float exitWallCooldown = 0.2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement1>();
    }

    private void Update()
    {
        CheckForWall();
        UpdateState();
        HandleProceduralVisuals();
    }

    private void FixedUpdate()
    {
        if (currentState == MovementState.WallRunning)
        {
            ExecuteWallRunMovement();
        }
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, wallLayer);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, wallLayer);
    }

    private bool IsAboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minHeightOffGround);
    }

    private void UpdateState()
    {
        if (exitingWall)
        {
            if (currentState == MovementState.WallRunning) StopWallRun();
            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0) exitingWall = false;
            return;
        }

        bool canWallRun = (wallLeft || wallRight) && IsAboveGround();
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (canWallRun && verticalInput > 0)
        {
            if (currentState != MovementState.WallRunning) StartWallRun();
            if (Input.GetKeyDown(KeyCode.Space)) WallJump();
        }
        else if (currentState == MovementState.WallRunning)
        {
            StopWallRun();
        }
        else
        {
            currentState = IsAboveGround() ? MovementState.Airborne : MovementState.Grounded;
        }
    }

    private void HandleProceduralVisuals()
    {
        float targetTilt = 0f;

        if (currentState == MovementState.WallRunning)
        {
            if (wallLeft)
            {
                targetTilt = -camTiltAmount;
            }
            else if (wallRight)
            {
                targetTilt = camTiltAmount;
            }
        }

        // Smoothly transition camera tilt
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
        pm.wallTilt = currentTilt;
    }

    private void StartWallRun()
    {
        currentState = MovementState.WallRunning;
        wallRunTimer = maxWallRunTime;
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private void ExecuteWallRunMovement()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.ProjectOnPlane(orientation.forward, wallNormal).normalized;

        float currentSpeed = Input.GetKey(sprintKey) ? wallSprintSpeed : wallRunSpeed;

        // NEW: Apply forward speed and force the Y velocity downward using wallSinkSpeed
        rb.linearVelocity = new Vector3(wallForward.x * currentSpeed, -wallSinkSpeed, wallForward.z * currentSpeed);

        // Keep the player stuck to the wall
        rb.AddForce(-wallNormal * 100f, ForceMode.Force);

        wallRunTimer -= Time.fixedDeltaTime;
        if (wallRunTimer <= 0) StopWallRun();
    }

    private void StopWallRun()
    {
        currentState = MovementState.Airborne;
        rb.useGravity = true;
    }

    private void WallJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallCooldown;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}