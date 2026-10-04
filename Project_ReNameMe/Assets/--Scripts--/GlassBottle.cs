using UnityEngine;

public class GlassBottle : MonoBehaviour
{
    [SerializeField] private float breakForceThreshold = 7.5f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                rb = collision.gameObject.GetComponentInParent<Rigidbody>();
            }
            
            if (rb != null && collision.relativeVelocity.magnitude > breakForceThreshold)
            {
                Destroy(gameObject);
            }
        }
    }
}
