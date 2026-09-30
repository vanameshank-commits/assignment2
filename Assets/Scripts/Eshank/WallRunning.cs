using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WallRunning : MonoBehaviour
{
    [Header("Wall Running Tuning")]
    [SerializeField] private float wallRunSpeed = 8f;
    [SerializeField] private float wallGravityDownforce = 2f;
    [SerializeField] private float maxWallRunTime = 1.5f;

    [Header("Wall Jumping")]
    [SerializeField] private float wallJumpUpForce = 7f;
    [SerializeField] private float wallJumpSideForce = 12f;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float minHeightOffGround = 1.5f;

    [Header("References")]
    [SerializeField] private Transform orientation;

    private Rigidbody rb;
    private float wallRunTimer;

    // State Transitions
    private enum MovementState { Grounded, Airborne, WallRunning }
    private MovementState currentState;

    // Wall Detection & Normals
    private bool wallLeft;
    private bool wallRight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    // Jump Cooldown to prevent instantly re-attaching
    private bool exitingWall;
    private float exitWallTimer;
    private readonly float exitWallCooldown = 0.2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        CheckForWall();
        UpdateState();
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
        // 1. Handle Wall Jump Cooldown Timer
        if (exitingWall)
        {
            if (currentState == MovementState.WallRunning) StopWallRun();

            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0) exitingWall = false;

            return; // Skip normal attachment logic while jumping off
        }

        // 2. Normal Attachment Logic
        bool canWallRun = (wallLeft || wallRight) && IsAboveGround();
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (canWallRun && verticalInput > 0)
        {
            if (currentState != MovementState.WallRunning) StartWallRun();

            // Check for Wall Jump while successfully attached
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

        rb.linearVelocity = new Vector3(wallForward.x * wallRunSpeed, rb.linearVelocity.y, wallForward.z * wallRunSpeed);
        rb.AddForce(Vector3.down * wallGravityDownforce, ForceMode.Force);
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
        // Enter cooldown phase so we don't instantly snap back
        exitingWall = true;
        exitWallTimer = exitWallCooldown;

        // Find the direction pointing exactly away from the wall
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        // Calculate our combined jump force (Up + Away from wall)
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        // Reset the Y velocity and apply the new force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}