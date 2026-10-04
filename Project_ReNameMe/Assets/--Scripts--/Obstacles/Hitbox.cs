using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Rigidbody>(out var rb))
        {
            Debug.Log(other.gameObject.name);
            rb.AddForce((transform.position + Random.insideUnitSphere - transform.position) * 5, ForceMode.Impulse);
        }
    }
}
