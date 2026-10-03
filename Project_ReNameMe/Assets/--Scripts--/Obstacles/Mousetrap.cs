using UnityEngine;

public class Mousetrap : EventTriggerCollider
{
    public override void Event()
    {
        Debug.Log("Mousetrap Triggered");
    }
}
