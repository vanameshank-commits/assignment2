using UnityEngine;

public class ParkourGameManager : MonoBehaviour
{
    public static ParkourGameManager Instance;

    [Header("Player & Checkpoints")]
    public Transform playerTransform;
    public Vector3 currentCheckpoint;

    [Header("Timer Settings")]
    public float elapsedTime = 0f;
    public bool isTimerRunning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (playerTransform != null)
        {
            currentCheckpoint = playerTransform.position;
        }
        isTimerRunning = true;
    }

    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    public void SetCheckpoint(Vector3 newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    public void RespawnPlayer()
    {
        if (playerTransform == null) return;

        // Clear player momentum
        PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ResetVelocity();
        }

        // Reposition Character Controller
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerTransform.position = currentCheckpoint;

        if (cc != null) cc.enabled = true;
    }

    public void FinishCourse()
    {
        isTimerRunning = false;
        Debug.Log("Course Finished! Time: " + elapsedTime.ToString("F2") + " seconds");
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 28;
        style.normal.textColor = Color.yellow;

        string timeText = "TIME: " + elapsedTime.ToString("F2") + "s";
        GUI.Label(new Rect(30, 30, 200, 50), timeText, style);
    }
}
