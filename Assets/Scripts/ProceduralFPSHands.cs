using UnityEngine;

public class ProceduralFPSHands : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;

    [Header("Left Arm Chain")]
    public Transform leftArm;   // L_arm
    public Transform leftElbow; // L_elbow
    public Transform leftWrist; // L_wrist

    [Header("Right Arm Chain")]
    public Transform rightArm;   // R_arm
    public Transform rightElbow; // R_elbow
    public Transform rightWrist; // R_wrist

    [Header("Walking/Running Sway")]
    public float walkBobSpeed = 10f;
    public float walkBobAmount = 0.04f;
    public float runBobSpeed = 14f;
    public float runBobAmount = 0.08f;

    [Header("Wall Detection Settings")]
    public float wallCheckDistance = 2.0f;
    public LayerMask wallLayer;
    public float lerpSpeed = 14f;

    [Header("Anti-Clipping Safety Stop")]
    [Tooltip("Maximum allowed sideways reach towards the wall")]
    public float maxSidewaysReach = 0.15f;
    [Tooltip("Safety distance buffer from wall surface")]
    public float wallBuffer = 0.35f;

    [Header("Left Arm Wall Rotations")]
    public Vector3 leftArmWallRotation = new Vector3(0f, 15f, -10f);
    public Vector3 leftElbowWallRotation = new Vector3(0f, -35f, 0f);
    public Vector3 leftWristWallRotation = new Vector3(0f, 90f, -90f);

    [Header("Right Arm Wall Rotations")]
    public Vector3 rightArmWallRotation = new Vector3(0f, -15f, 10f);
    public Vector3 rightElbowWallRotation = new Vector3(0f, 35f, 0f);
    public Vector3 rightWristWallRotation = new Vector3(0f, -90f, 90f);

    private float timer = 0f;

    private Vector3 leftArmDefaultPos;
    private Vector3 rightArmDefaultPos;

    private Quaternion leftArmDefaultRot;
    private Quaternion leftElbowDefaultRot;
    private Quaternion leftWristDefaultRot;

    private Quaternion rightArmDefaultRot;
    private Quaternion rightElbowDefaultRot;
    private Quaternion rightWristDefaultRot;

    void Start()
    {
        if (leftArm != null) { leftArmDefaultPos = leftArm.localPosition; leftArmDefaultRot = leftArm.localRotation; }
        if (leftElbow != null) leftElbowDefaultRot = leftElbow.localRotation;
        if (leftWrist != null) leftWristDefaultRot = leftWrist.localRotation;

        if (rightArm != null) { rightArmDefaultPos = rightArm.localPosition; rightArmDefaultRot = rightArm.localRotation; }
        if (rightElbow != null) rightElbowDefaultRot = rightElbow.localRotation;
        if (rightWrist != null) rightWristDefaultRot = rightWrist.localRotation;
    }

    void Update()
    {
        if (controller == null) return;

        bool isGrounded = controller.isGrounded;
        float speed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;

        // Raycast from Camera/Container center outward
        bool wallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, wallCheckDistance, wallLayer);
        bool wallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, wallCheckDistance, wallLayer);

        Vector3 targetLeftArmPos = leftArmDefaultPos;
        Vector3 targetRightArmPos = rightArmDefaultPos;

        Quaternion targetLeftArmRot = leftArmDefaultRot;
        Quaternion targetLeftElbowRot = leftElbowDefaultRot;
        Quaternion targetLeftWristRot = leftWristDefaultRot;

        Quaternion targetRightArmRot = rightArmDefaultRot;
        Quaternion targetRightElbowRot = rightElbowDefaultRot;
        Quaternion targetRightWristRot = rightWristDefaultRot;

        // 1. STRICT CLAMPED WALL REACH
        if (!isGrounded)
        {
            if (wallLeft)
            {
                // Calculate actual distance to wall and clamp maximum extension
                float availableDistance = Mathf.Max(0, leftHit.distance - wallBuffer);
                float allowedReach = Mathf.Min(availableDistance, maxSidewaysReach);

                targetLeftArmPos += new Vector3(-allowedReach, 0.02f, 0.05f);

                targetLeftArmRot = leftArmDefaultRot * Quaternion.Euler(leftArmWallRotation);
                targetLeftElbowRot = leftElbowDefaultRot * Quaternion.Euler(leftElbowWallRotation);
                targetLeftWristRot = leftWristDefaultRot * Quaternion.Euler(leftWristWallRotation);
            }
            if (wallRight)
            {
                float availableDistance = Mathf.Max(0, rightHit.distance - wallBuffer);
                float allowedReach = Mathf.Min(availableDistance, maxSidewaysReach);

                targetRightArmPos += new Vector3(allowedReach, 0.02f, 0.05f);

                targetRightArmRot = rightArmDefaultRot * Quaternion.Euler(rightArmWallRotation);
                targetRightElbowRot = rightElbowDefaultRot * Quaternion.Euler(rightElbowWallRotation);
                targetRightWristRot = rightWristDefaultRot * Quaternion.Euler(rightWristWallRotation);
            }
        }

        // 2. WALKING / RUNNING BOBBING
        if (isGrounded && speed > 0.1f)
        {
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float currentAmount = isRunning ? runBobAmount : walkBobAmount;

            timer += Time.deltaTime * currentSpeed;

            float leftY = Mathf.Sin(timer) * currentAmount;
            float leftZ = Mathf.Cos(timer) * currentAmount;

            float rightY = -Mathf.Sin(timer) * currentAmount;
            float rightZ = -Mathf.Cos(timer) * currentAmount;

            targetLeftArmPos += new Vector3(0, leftY, leftZ);
            targetRightArmPos += new Vector3(0, rightY, rightZ);
        }

        // 3. APPLY SMOOTH INTERPOLATION
        if (leftArm != null)
        {
            leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, targetLeftArmPos, Time.deltaTime * lerpSpeed);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetLeftArmRot, Time.deltaTime * lerpSpeed);
        }
        if (leftElbow != null)
            leftElbow.localRotation = Quaternion.Slerp(leftElbow.localRotation, targetLeftElbowRot, Time.deltaTime * lerpSpeed);
        if (leftWrist != null)
            leftWrist.localRotation = Quaternion.Slerp(leftWrist.localRotation, targetLeftWristRot, Time.deltaTime * lerpSpeed);

        if (rightArm != null)
        {
            rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, targetRightArmPos, Time.deltaTime * lerpSpeed);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRightArmRot, Time.deltaTime * lerpSpeed);
        }
        if (rightElbow != null)
            rightElbow.localRotation = Quaternion.Slerp(rightElbow.localRotation, targetRightElbowRot, Time.deltaTime * lerpSpeed);
        if (rightWrist != null)
            rightWrist.localRotation = Quaternion.Slerp(rightWrist.localRotation, targetRightWristRot, Time.deltaTime * lerpSpeed);
    }
}
