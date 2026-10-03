using UnityEngine;

public class Interactable : MonoBehaviour
{
    public enum EInteractableType
    {
        PickupDropOnly,
        PickupUseDrop,
        UseOnly
    }
    
    [Header("Inscribed")]
    
    [SerializeField] private EInteractableType interactableType;

    [SerializeField] private Outline outline;
    
    [SerializeField] private Rigidbody rb;
    
    [SerializeField] private Collider col;

    [SerializeField] private bool isDynamicLoose = true;
    
    [SerializeField] private bool isDynamicHeld;

    [Header("Dynamic")]
    
    [SerializeField] private int numHovering;
    
    [field: SerializeField] public bool IsHeld { get; private set; }
    
    public EInteractableType InteractableType => interactableType;
    
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
        IsHeld = true;
        
        ToggleOutline(true);
        
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
        IsHeld = false;
        
        ToggleOutline(false);
        
        if (col != null)
        {
            col.enabled = true;
        }

        transform.SetParent(null);
        
        if (dropPosition != null)
        {
            // moving to drop position before applying throw velocity to avoid unwanted physics behavior
            transform.position = dropPosition.position;
            
            transform.rotation = dropPosition.rotation;
            
            if (throwVelocity != Vector3.zero)
            {
                rb.AddForce(throwVelocity, ForceMode.VelocityChange);
            }
        }
    }
    
    public virtual void StartHover()
    {
        numHovering++;
        
        ToggleOutline(true);
    }
    
    public virtual void EndHover()
    {
        
        numHovering--;
        
        if (numHovering < 0)
        {
            numHovering = 0;
        }
        else if (numHovering > 0 || IsHeld)
        {
            return;
        }
        
        ToggleOutline(false);
    }
    
    public virtual void StartInteract(Transform heldParentLocation)
    { 
       ToggleDynamic(isDynamicHeld);
       
       BeHeld(heldParentLocation);
    }

    public virtual void EndInteract(Transform dropPosition = null, Vector3 throwVelocity = default)
    {
        ToggleDynamic(isDynamicLoose);
        
        BeDropped(dropPosition, throwVelocity);
    }
    
    public virtual void Use()
    {
        Debug.Log("Used");
    }
}
