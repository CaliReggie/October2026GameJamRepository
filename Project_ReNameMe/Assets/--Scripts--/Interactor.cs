using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Interactor : MonoBehaviour
{
    [Header("Inscribed")]
    
    [SerializeField] private GameObject cameraObject;
    
    [SerializeField] private GameObject playerObjectPio;
    
    [SerializeField] private LayerMask interactableLayerMask;
    
    [SerializeField] private float interactRaycastDistance = 1f;
    
    [SerializeField] private float interactRaycastRadius = 0.1f;

    [SerializeField] private float maxDistanceFromObjectToInteract = 0.5f;
    
    [Header("Dynamic")]
    
    [SerializeField] private Interactable hoveringInteractable;
    
    [SerializeField] private Interactable lastHoveringInteractable;

    [SerializeField] private Interactable heldInteractable;
    
    [SerializeField] private Transform heldParentLocation;

    [SerializeField] private Transform dropLocation;
    
    

    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged += UpdateFromSquirrelData;
        }
    }
    
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged -= UpdateFromSquirrelData;
        }
    }
    
    private void UpdateFromSquirrelData()
    {
        try
        {
            PlayerObjectSquirrelTypeHandler squirrelTypeHandler = playerObjectPio.GetComponent<PlayerObjectSquirrelTypeHandler>();

            heldParentLocation = squirrelTypeHandler.CurrentPlayerObjectData.heldObjectLocation;
            
            dropLocation = squirrelTypeHandler.CurrentPlayerObjectData.dropObjectLocation;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating from squirrel data: {e.Message}");
        }
    }
        
    public void OnInteract()
    {
        if (hoveringInteractable != null) // will need to add differentation between holding or using here, and alt location
        {
            // we can only be holding a pickup type interactable
            switch (hoveringInteractable.InteractableType)
            {
                case Interactable.EInteractableType.PickupDropOnly:
                case Interactable.EInteractableType.PickupUseDrop:    
                    //pickup the hovering interactable
                    SetHeldInteractable(hoveringInteractable, heldParentLocation);
                    break;
                case Interactable.EInteractableType.UseOn:
                    // first see if holding a useable interactable, (if not then nothing)
                    // next see if the hovering interactable can be used on, (if not then nothing)
                    // next verity match between use can of holding and use on hovering, (if not then nothing)
                    // lastly, use the hovering interactable with the holding interactable,
                    if (heldInteractable != null && heldInteractable.InteractableType == Interactable.EInteractableType.PickupUseDrop)
                    {
                        // // Assuming the held interactable has a method to check if it can be used on the hovering interactable
                        // if (heldInteractable.CanUseOn(hoveringInteractable))
                        // {
                        //     heldInteractable.UseOn(hoveringInteractable);
                        // }
                    }
                    break;
            }
            
            
        }
        // might be holding a useable pickup interactable, if so then use it
        else if (heldInteractable != null && Interactable.CanUsePickupInteractable(heldInteractable))
        {
            heldInteractable.Use();;
        }
    }
    
    public void OnDrop()
    {
        RemoveHeldInteractable(dropLocation);
    }
    
    private void Update()
    {
        ManageHoveringInteractable();
    }
    
    private void ManageHoveringInteractable()
    {
        RaycastHit hit;
        bool hitSomething = Physics.SphereCast(cameraObject.transform.position, interactRaycastRadius, cameraObject.transform.forward, out hit, interactRaycastDistance, interactableLayerMask);
        
        if (hitSomething)
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            
            if (interactable != null && interactable != heldInteractable)
            {
                float distanceToInteractable = Vector3.Distance(playerObjectPio.transform.position, hit.point);
                
                if (distanceToInteractable <= maxDistanceFromObjectToInteract)
                {
                    hoveringInteractable = interactable;
                }
                else
                {
                    hoveringInteractable = null;
                }
            }
            else
            {
                hoveringInteractable = null;
            }
        }
        else
        {
            hoveringInteractable = null;
        }

        if (hoveringInteractable != lastHoveringInteractable)
        {
            if (lastHoveringInteractable != null)
            {
                lastHoveringInteractable.EndHover();
            }

            if (hoveringInteractable != null)
            {
                hoveringInteractable.StartHover();
            }

            lastHoveringInteractable = hoveringInteractable;
        }
    }
    
    private void RemoveHeldInteractable(Transform dropPosition = null, Vector3 throwVelocity = default)
    {
        if (heldInteractable != null)
        {
            heldInteractable.EndInteract(dropPosition, throwVelocity);
            heldInteractable = null;
        }
    }
    
    private void SetHeldInteractable(Interactable interactable, Transform parentLocation)
    {
        if (heldInteractable != null)
        {
            RemoveHeldInteractable(dropLocation);
        }

        heldInteractable = interactable;

        if (heldInteractable != null)
        {
            heldInteractable.StartInteract(parentLocation);
        }
    }
}
