using UnityEngine;

public class CollisionDamage : MonoBehaviour
{
    public Transform respawnLocation;
    private void OnCollisionEnter(Collision collision)
    {
        PlayerObjectPioComponent playerObjectPioComponent = collision.gameObject.GetComponent<PlayerObjectPioComponent>();
        
        if (playerObjectPioComponent != null)
        {
            playerObjectPioComponent.GetHitForDamage(100);
            collision.gameObject.transform.position = respawnLocation.position;
        }

    }
}
