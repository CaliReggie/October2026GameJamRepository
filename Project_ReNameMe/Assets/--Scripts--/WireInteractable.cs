using UnityEngine;

public class WireInteractable : Interactable
{
    [Header("Inscribed")]
    
    [SerializeField] private GameObject normalWireObject;
    
    [SerializeField] private GameObject cutWireObject;
    
    [Header("Dynamic")]
    
    [SerializeField] private bool isWireCut = false;
    
    protected override void Start()
    {
        base.Start();
        
        UpdateWireVisuals();
    }
    
    public override void Use()
    {
        if (!isWireCut)
        {
            CutWire();
        }
    }
    
    private void CutWire()
    {
        isWireCut = true;
        
        UpdateWireVisuals();
        
        Invoke(nameof(GameWon), 2.5f);
    }
    
    private void UpdateWireVisuals()
    {
        if (normalWireObject != null)
        {
            normalWireObject.SetActive(!isWireCut);
        }
        
        if (cutWireObject != null)
        {
            cutWireObject.SetActive(isWireCut);
        }
    }
    
    private void GameWon()
    {
        GameManager.Instance?.GameOver(true);
    }
}
