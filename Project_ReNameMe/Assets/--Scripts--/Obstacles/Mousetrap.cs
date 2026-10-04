using UnityEngine;

public class Mousetrap : EventTriggerCollider
{
    public override void Event()
    {
        Debug.Log("Mousetrap Triggered");
    }

    public override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.gameObject.TryGetComponent<PlayerObjectPioComponent>(out var player))
        {
            player.GetHitForDamage(1);
        }
    }
}
