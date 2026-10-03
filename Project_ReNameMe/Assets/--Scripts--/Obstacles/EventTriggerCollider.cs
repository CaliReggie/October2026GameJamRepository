using UnityEngine;

public abstract class EventTriggerCollider : MonoBehaviour, IEventTrigger
{
    public abstract void Event();

    public virtual void OnTriggerEnter(Collider other)
    {
        Event();
    }
}
