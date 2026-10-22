using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private string killFloorTag = "KillFloor";

    private Vector3 startPosition;
    private Rigidbody rb;

    private void Start()
    {
        // Remember where the player starts when the game loads
        startPosition = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    // Works if your kill floor is set to "Is Trigger"
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(killFloorTag)) Respawn();
    }

    // Works if your kill floor is a solid collider
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(killFloorTag)) Respawn();
    }

    private void Respawn()
    {
        transform.position = startPosition;

        // You must reset velocity, otherwise the player teleports but keeps falling at 100mph
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}