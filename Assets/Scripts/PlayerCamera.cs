using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float mouseSensitivity = 200f;
    public Transform playerBody;

    [Header("Visual Juice & Camera Tilt")]
    public float tiltAmount = 12f;
    public float tiltSpeed = 8f;
    public float normalFOV = 60f;
    public float wallRunFOV = 75f;
    public float fovSpeed = 8f;

    private float xRotation = 0f;
    private float currentTilt = 0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cam != null) cam.fieldOfView = normalFOV;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        playerBody.Rotate(Vector3.up * mouseX);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
    }

    public void SetWallRunEffects(bool isWallRunning, bool isLeftWall)
    {
        float targetTilt = 0f;
        if (isWallRunning)
        {
            targetTilt = isLeftWall ? -tiltAmount : tiltAmount;
        }
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        float targetFOV = isWallRunning ? wallRunFOV : normalFOV;
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
        }
    }
}
