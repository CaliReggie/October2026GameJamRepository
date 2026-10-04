using UnityEngine;

public class Interactable : MonoBehaviour
{
    public enum EInteractableType
    {
        PickupDropOnly,
        PickupUseDrop,
        UseOn
    }
    
    public enum EUseInteractionType
    {
        Basic, // just need use on interactable, no pickup needed, turn something on/off, open/close, etc.
        Cutting // need cutting use pickup interactable and cutting use on interactable
    }
    
    public static bool CanUsePickupInteractable(Interactable useableInteractable, Interactable useOnInteractable = null)
    {
        if (useableInteractable == null)
        {
            return false;
        }
        
        if (useableInteractable.InteractableType != EInteractableType.PickupUseDrop)
        {
            return false;
        }
        
        switch (useableInteractable.UseInteractionType)
        {
            case EUseInteractionType.Basic:
                return true;
            
            case EUseInteractionType.Cutting:
                
                if (useOnInteractable == null)
                {
                    return false;
                }
                
                if (useOnInteractable.InteractableType != EInteractableType.UseOn)
                {
                    return false;
                }
                
                if (useOnInteractable.UseInteractionType != EUseInteractionType.Cutting)
                {
                    return false;
                }

                return true;
            
            default:
                return false;
        }
    }
    
    public static bool CanUseUseOnInteractable(Interactable useOn, Interactable usableInteractable = null)
    {
        if (useOn == null)
        {
            return false;
        }
        
        if (useOn.InteractableType != EInteractableType.UseOn)
        {
            return false;
        }

        switch (useOn.UseInteractionType)
        {
            case EUseInteractionType.Basic:
                return true;
            
            case EUseInteractionType.Cutting:
                
                if (usableInteractable == null)
                {
                    return false;
                }
                
                if (usableInteractable.InteractableType != EInteractableType.PickupUseDrop)
                {
                    return false;
                }
                
                if (usableInteractable.UseInteractionType != EUseInteractionType.Cutting)
                {
                    return false;
                }

                return true;
            
            default:
                return false;
        }
    }
    
    [Header("Inscribed")]
    
    [SerializeField] private EInteractableType interactableType;
    
    [SerializeField] private EUseInteractionType useInteractionType;

    [SerializeField] private Outline outline;
    
    [SerializeField] private Rigidbody rb;
    
    [SerializeField] private Collider col;

    [SerializeField] private bool isDynamicLoose = true;
    
    [SerializeField] private bool isDynamicHeld;

    [Header("Dynamic")]
    
    [SerializeField] private int numHovering;
    
    [field: SerializeField] public bool IsHeld { get; private set; }
    
    public EInteractableType InteractableType => interactableType;
    
    public EUseInteractionType UseInteractionType => useInteractionType;
    
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
        if (interactableType == EInteractableType.UseOn)
        {
            Debug.LogWarning("Attempted to hold an interactable that is UseOn. This is not allowed.");
            return;
        }
        
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
        if (interactableType == EInteractableType.UseOn)
        {
            Debug.LogWarning("Attempted to drop an interactable that is UseOn. This is not allowed.");
            return;
        }
        
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
        if (interactableType == EInteractableType.PickupDropOnly)
        {
            Debug.LogWarning("Attempted to use an interactable that is PickupDropOnly. This is not allowed.");
            return;
        }
        
        Debug.Log("Used");
    }
}
