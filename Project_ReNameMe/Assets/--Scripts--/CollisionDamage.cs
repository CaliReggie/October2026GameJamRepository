using UnityEngine;

public class CollisionDamage : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        PlayerObjectPioComponent playerObjectPioComponent = collision.gameObject.GetComponent<PlayerObjectPioComponent>();
        
        if (playerObjectPioComponent != null)
        {
            playerObjectPioComponent.GetHitForDamage(1);
        }
    }
}
