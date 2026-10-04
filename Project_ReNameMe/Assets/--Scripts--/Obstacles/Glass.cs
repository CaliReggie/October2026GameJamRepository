using UnityEngine;

public class Glass : MonoBehaviour
{
    [SerializeField] private float breakForceThreshold = 7.5f;
    
    [SerializeField] GameObject brokenMesh;

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
                brokenMesh.SetActive(true);
                brokenMesh.transform.SetParent(null);
                gameObject.SetActive(false);
            }
        }
    }
}
