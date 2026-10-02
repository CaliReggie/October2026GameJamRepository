using UnityEngine;
using UnityEngine.EventSystems;

public abstract class DeviceClickDetector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // Unique ID for the pointer/virtual mouse instance
        int pointerId = eventData.pointerId;
        
        HandleClickFromId(pointerId);
    }
    
    protected abstract void HandleClickFromId(int pointerId);
}