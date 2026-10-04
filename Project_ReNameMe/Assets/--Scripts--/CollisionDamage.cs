using UnityEngine;

public class CollisionDamage : MonoBehaviour
{
    public Transform[] respawnLocations;
    
    private void OnCollisionEnter(Collision collision)
    {
        PlayerObjectPioComponent playerObjectPioComponent = collision.gameObject.GetComponent<PlayerObjectPioComponent>();
        
        if (playerObjectPioComponent != null)
        {
            playerObjectPioComponent.GetHitForDamage(100);
            
            Vector3 otherPlayerPosition = PlayerManager.Instance.GetPositionOfOtherPlayer(playerObjectPioComponent.GetComponentInParent<PlayerInputObject>().VisualIndex);
            
            //find the closest respawn location to the other player
            Transform closestRespawnLocation = null;
            float closestDistance = float.MaxValue;
            foreach (var respawnLocation in respawnLocations)
            {
                float distance = Vector3.Distance(otherPlayerPosition, respawnLocation.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRespawnLocation = respawnLocation;
                }
            }
            
            if (closestRespawnLocation != null)
            {
                playerObjectPioComponent.TpPlayerObject(closestRespawnLocation.position,closestRespawnLocation.rotation.eulerAngles, false);
            }
        }

    }
}
