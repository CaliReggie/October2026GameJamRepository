using UnityEngine;

public class CatAnimationEvents : MonoBehaviour
{
    [SerializeField] private BoxCollider collisionBox;

    public void ToggleHitbox()
    {
        collisionBox.enabled = (!collisionBox.enabled);
    }
}
