using UnityEngine;

public class WaterFaucetInteractable : Interactable
{
    [Header("Inscribed")]
    
    [SerializeField] private GameObject waterFlowObject;
    
    [Header("Dynamic")]
    
    [SerializeField] private bool isWaterFlowing;
    
    protected override void Start()
    {
        base.Start();
        
        UpdateWaterFlowVisuals();
    }
    
    public override void Use()
    {
        ToggleWaterFlow();
    }
    
    private void ToggleWaterFlow()
    {
        isWaterFlowing = !isWaterFlowing;
        
        UpdateWaterFlowVisuals();
    }
    
    private void UpdateWaterFlowVisuals()
    {
        if (waterFlowObject != null)
        {
            waterFlowObject.SetActive(isWaterFlowing);
        }
    }
}
