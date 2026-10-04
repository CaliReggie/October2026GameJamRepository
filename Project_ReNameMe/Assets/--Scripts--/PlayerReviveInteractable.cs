using UnityEngine;

public class PlayerReviveInteractable : Interactable
{
    [Header("Dynamic")]
    
    [SerializeField] private PlayerObjectPioComponent playerObjectPioComponent;
    
    protected override void Awake()
    {
        base.Awake();
        
        playerObjectPioComponent = GetComponentInParent<PlayerInputObject>().GetComponentInChildren<PlayerObjectPioComponent>();
    }
    
    public override void Use()
    {
        if (playerObjectPioComponent != null)
        {
            playerObjectPioComponent.HealForHealth(1);
        }
    }
}
