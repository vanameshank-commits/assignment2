using UnityEngine;

public enum MovementState
{
    Grounded,
    Airborne,
    WallRunning
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 8f;
    public float wallRunSpeed = 14f;       // Increased for fast, smooth forward momentum
    public float jumpHeight = 2.5f;
    public float gravity = -22f;

    [Header("Wall Running Settings")]
    public float wallCheckDistance = 0.9f;
    public float wallRunGravity = -1f;     // Low gravity for horizontal stickiness
    public float maxWallRunTime = 2.5f;
    public float wallJumpUpForce = 8f;
    public float wallJumpSideForce = 10f;
    public LayerMask wallLayer;

    [Header("References")]
    public PlayerCamera playerCam;

    private CharacterController controller;
    private Vector3 velocity;
    private MovementState state;

    private RaycastHit leftWallHit, rightWallHit;
    private bool hasWallLeft, hasWallRight;
    private bool isWallRunning;
    private float wallRunTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        wallRunTimer = maxWallRunTime;
    }

    void Update()
    {
        CheckForWalls();
        UpdateMovementState();
        ExecuteMovement();

        if (playerCam != null)
        {
            playerCam.SetWallRunEffects(isWallRunning, hasWallLeft);
        }
    }

    void CheckForWalls()
    {
        Vector3 leftDir = (-transform.right + transform.forward * 0.4f).normalized;
        Vector3 rightDir = (transform.right + transform.forward * 0.4f).normalized;

        Vector3 highOrigin = transform.position + Vector3.up * 0.6f;
        Vector3 midOrigin = transform.position;
        Vector3 lowOrigin = transform.position - Vector3.up * 0.6f;

        bool leftHigh = Physics.Raycast(highOrigin, leftDir, out RaycastHit leftHighHit, wallCheckDistance, wallLayer);
        bool leftMid = Physics.Raycast(midOrigin, leftDir, out RaycastHit leftMidHit, wallCheckDistance, wallLayer);
        bool leftLow = Physics.Raycast(lowOrigin, leftDir, out RaycastHit leftLowHit, wallCheckDistance, wallLayer);

        bool rightHigh = Physics.Raycast(highOrigin, rightDir, out RaycastHit rightHighHit, wallCheckDistance, wallLayer);
        bool rightMid = Physics.Raycast(midOrigin, rightDir, out RaycastHit rightMidHit, wallCheckDistance, wallLayer);
        bool rightLow = Physics.Raycast(lowOrigin, rightDir, out RaycastHit rightLowHit, wallCheckDistance, wallLayer);

        hasWallLeft = leftMid && (leftHigh || leftLow);
        hasWallRight = rightMid && (rightHigh || rightLow);

        if (hasWallLeft) leftWallHit = leftMidHit;
        if (hasWallRight) rightWallHit = rightMidHit;
    }

    void UpdateMovementState()
    {
        bool isHoldingForward = Input.GetAxisRaw("Vertical") > 0;

        if (controller.isGrounded)
        {
            state = MovementState.Grounded;
            isWallRunning = false;
            wallRunTimer = maxWallRunTime;

            velocity.x = 0f;
            velocity.z = 0f;

            if (velocity.y < 0) velocity.y = -2f;
        }
        else if ((hasWallLeft || hasWallRight) && isHoldingForward && wallRunTimer > 0)
        {
            // Lock vertical falling momentum instantly upon touching the wall
            if (!isWallRunning)
            {
                velocity.y = 0f;
            }

            state = MovementState.WallRunning;
            isWallRunning = true;
            wallRunTimer -= Time.deltaTime;
        }
        else
        {
            state = MovementState.Airborne;
            isWallRunning = false;

            velocity.x = Mathf.Lerp(velocity.x, 0f, Time.deltaTime * 3f);
            velocity.z = Mathf.Lerp(velocity.z, 0f, Time.deltaTime * 3f);
        }
    }

    void ExecuteMovement()
    {
        Vector3 finalMove = Vector3.zero;

        if (state == MovementState.WallRunning)
        {
            RaycastHit wallHit = hasWallLeft ? leftWallHit : rightWallHit;

            // Project forward movement parallel to wall face
            Vector3 wallDirection = Vector3.ProjectOnPlane(transform.forward, wallHit.normal).normalized;
            finalMove = wallDirection * wallRunSpeed;

            // Apply light downward float (clamped so you don't plunge)
            velocity.y += wallRunGravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -1.5f);
            finalMove.y = velocity.y;

            // Wall Jump
            if (Input.GetButtonDown("Jump"))
            {
                Vector3 wallJumpDirection = (wallHit.normal * wallJumpSideForce) + (Vector3.up * wallJumpUpForce) + (transform.forward * 4f);
                velocity = wallJumpDirection;
                wallRunTimer = 0;
            }
        }
        else // Grounded or Airborne
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 moveDir = transform.right * x + transform.forward * z;
            finalMove = moveDir * walkSpeed;

            if (Input.GetButtonDown("Jump") && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            finalMove.y = velocity.y;
        }

        // Single execution call prevents physics stutter
        controller.Move(finalMove * Time.deltaTime);
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 leftDir = (-transform.right + transform.forward * 0.4f).normalized;
        Vector3 rightDir = (transform.right + transform.forward * 0.4f).normalized;

        Vector3 highOrigin = transform.position + Vector3.up * 0.6f;
        Vector3 midOrigin = transform.position;
        Vector3 lowOrigin = transform.position - Vector3.up * 0.6f;

        Gizmos.DrawRay(highOrigin, leftDir * wallCheckDistance);
        Gizmos.DrawRay(midOrigin, leftDir * wallCheckDistance);
        Gizmos.DrawRay(lowOrigin, leftDir * wallCheckDistance);

        Gizmos.DrawRay(highOrigin, rightDir * wallCheckDistance);
        Gizmos.DrawRay(midOrigin, rightDir * wallCheckDistance);
        Gizmos.DrawRay(lowOrigin, rightDir * wallCheckDistance);
    }
}
