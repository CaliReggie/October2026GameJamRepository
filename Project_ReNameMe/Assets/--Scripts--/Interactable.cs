using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Interactable : MonoBehaviour
{
    public enum EInteractableType
    {
        PickupDropOnly,
        PickupUseDrop,
        UseOnly
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
            Debug.Log("Cannot use null pickup interactable");
            return false;
        }
        
        if (useableInteractable.InteractableType != EInteractableType.PickupUseDrop)
        {
            Debug.Log("Cannot use pickup interactable that is not PickupUseDrop");
            return false;
        }
        
        switch (useableInteractable.UseInteractionType)
        {
            case EUseInteractionType.Basic:
                Debug.Log("Can use basic pickup interactable");
                return true;
            
            case EUseInteractionType.Cutting:
                
                if (useOnInteractable == null)
                {
                    Debug.Log("Cannot use cutting pickup interactable without a use on interactable");
                    return false;
                }
                
                if (useOnInteractable.InteractableType != EInteractableType.UseOnly)
                {
                    Debug.Log("Cannot use cutting pickup interactable with a non-use on interactable");
                    return false;
                }
                
                if (useOnInteractable.UseInteractionType != EUseInteractionType.Cutting)
                {
                    Debug.Log("Cannot use cutting pickup interactable with a non-use on interactable");
                    return false;
                }
                
                Debug.Log("Can use cutting pickup interactable with a cutting use on interactable");
                return true;
            
            default:
                Debug.Log("Cannot use pickup interactable with unknown use interaction type");
                return false;
        }
    }
    
    public static bool CanUseUseOnInteractable(Interactable useOn, Interactable usableInteractable = null)
    {
        if (useOn == null)
        {
            Debug.Log("Cannot use null use on interactable");
            return false;
        }
        
        if (useOn.InteractableType != EInteractableType.UseOnly)
        {
            Debug.Log("Cannot use use on interactable that is not UseOnly");
            return false;
        }

        switch (useOn.UseInteractionType)
        {
            case EUseInteractionType.Basic:
                Debug.Log("Can use basic use on interactable");
                return true;
            
            case EUseInteractionType.Cutting:
                
                if (usableInteractable == null)
                {
                    Debug.Log("Cannot use cutting use on interactable without a usable interactable");
                    return false;
                }
                
                if (usableInteractable.InteractableType != EInteractableType.PickupUseDrop)
                {
                    Debug.Log("Cannot use cutting use on interactable with a non-pickup usable interactable");
                    return false;
                }
                
                if (usableInteractable.UseInteractionType != EUseInteractionType.Cutting)
                {
                    Debug.Log("Cannot use cutting use on interactable with a non-cutting usable interactable");
                    return false;
                }

                Debug.Log("Can use cutting use on interactable with a cutting usable interactable");
                return true;
            
            default:
                Debug.Log("Cannot use use on interactable with unknown use interaction type");
                return false;
        }
    }
    
    [Header("Inscribed")]
    
    [SerializeField] protected EInteractableType interactableType;
    
    [SerializeField] protected EUseInteractionType useInteractionType;

    [SerializeField] protected Outline outline;
    
    [SerializeField] protected Rigidbody rb;
    
    [SerializeField] protected Collider col;

    [SerializeField] protected bool staticInEnvironment;

    [Header("Dynamic")]
    
    [SerializeField] protected int numHovering;
    
    [field: SerializeField] public bool IsHeld { get; protected set; }
    
    public EInteractableType InteractableType => interactableType;
    
    public EUseInteractionType UseInteractionType => useInteractionType;
    
    protected virtual void Awake()
    {
        EndHover();
        EndInteract();
    }
    
    protected virtual void Start()
    {
        
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
        if (interactableType == EInteractableType.UseOnly) { return; }
        
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
        if (interactableType == EInteractableType.UseOnly) { return; }
        
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
       ToggleDynamic(false);
       
       BeHeld(heldParentLocation);
    }

    public virtual void EndInteract(Transform dropPosition = null, Vector3 throwVelocity = default)
    {
        ToggleDynamic(!staticInEnvironment);
        
        BeDropped(dropPosition, throwVelocity);
        
        if (interactableType == EInteractableType.UseOnly) { return; }
        
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
    }
    
    public virtual void Use()
    {
        if (interactableType == EInteractableType.PickupDropOnly)
        {
            Debug.LogWarning("Attempted to use an interactable that is PickupDropOnly. This is not allowed.");
            return;
        }
        
        
    }
}
