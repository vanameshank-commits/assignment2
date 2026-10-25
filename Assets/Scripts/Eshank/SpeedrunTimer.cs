using UnityEngine;
using TMPro;

public class SpeedrunTimer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI liveTimerText;
    [SerializeField] private TextMeshProUGUI lastRunTimeText;

    [Header("Settings")]
    [SerializeField] private string endPlatformTag = "EndPlatform";

    private float currentTime;
    private bool isRunning = true;
    private Vector3 startPosition;
    private Rigidbody rb;

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI(liveTimerText, currentTime);
        }
    }

    private void UpdateTimerUI(TextMeshProUGUI textElement, float time)
    {
        if (textElement == null) return;

        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        float milliseconds = (time % 1) * 1000;

        textElement.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(endPlatformTag))
        {
            // Save final time to the secondary text box before resetting
            if (lastRunTimeText != null)
            {
                lastRunTimeText.text = "Last Run: " + liveTimerText.text;
            }

            // Reset player position and momentum
            transform.position = startPosition;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Reset timer to 0 for the next attempt
            currentTime = 0f;
        }
    }
}