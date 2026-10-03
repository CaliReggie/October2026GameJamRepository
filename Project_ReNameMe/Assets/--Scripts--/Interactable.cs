using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Inscribed")]

    [SerializeField] private Outline outline;
    
    [SerializeField] private Rigidbody rb;
    
    [SerializeField] private Collider col;

    [SerializeField] private bool isDynamicLoose = true;
    
    [SerializeField] private bool isDynamicHeld;
    
    protected virtual void Start()
    {
        ToggleOutline(false);

        EndInteract();
    }
    
    protected virtual void ToggleOutline(bool enable)
    {
        if (outline != null)
        {
            outline.enabled = enable;
        }
    }
    
    protected virtual void ToggleDynamic(bool enable)
    {
        if (rb != null)
        {
            if (!rb.isKinematic) // resetting velocity when making changes for unwanted physics behavior
            {
                rb.linearVelocity = Vector3.zero;
                
                rb.angularVelocity = Vector3.zero;
            }
            
            rb.isKinematic = !enable;
        }
    }
    
    protected virtual void BeHeld(Transform heldParentLocation)
    {
        if (col != null)
        {
            col.enabled = false;
        }
        
        transform.SetParent(heldParentLocation);
        
        transform.position = heldParentLocation.position;
        
        transform.rotation = heldParentLocation.rotation;
    }
    
    protected virtual void BeDropped(Transform dropPosition = null, Vector3 throwVelocity = default)
    {
        if (col != null)
        {
            col.enabled = true;
        }

        transform.SetParent(null);
        
        if (dropPosition != null)
        {
            if (rb != null)
            { 
                //using rb.move position and rotation
                rb.MovePosition(dropPosition.position);
                
                rb.MoveRotation(dropPosition.rotation);
                
                if (throwVelocity != Vector3.zero)
                {
                    rb.AddForce(throwVelocity, ForceMode.VelocityChange);
                }
            }
            else
            {
                transform.position = dropPosition.position;
            
                transform.rotation = dropPosition.rotation;
            }
        }
    }
    
    public virtual void StartHover()
    {
        ToggleOutline(true);
    }
    
    public virtual void EndHover()
    {
        ToggleOutline(false);
    }
    
    public virtual void StartInteract(Transform heldParentLocation)
    { 
        ToggleOutline(true);
        
       ToggleDynamic(isDynamicHeld);
       
       BeHeld(heldParentLocation);
    }

    public virtual void EndInteract(Transform dropPosition = null, Vector3 throwVelocity = default)
    {
        ToggleOutline(false);

        ToggleDynamic(isDynamicLoose);
        
        BeDropped(dropPosition, throwVelocity);
    }
    
    public virtual void Use()
    {
        Debug.Log("Used");
    }
}
