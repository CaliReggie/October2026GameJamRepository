using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPlayButtonToggle : MonoBehaviour
{
    [Header("Dynamic")]
    
    [SerializeField] private Button button;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged += HandleButtonToggleOnCharacterSelectionChanged;
            
            HandleButtonToggleOnCharacterSelectionChanged();
        }
    }
    
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged -= HandleButtonToggleOnCharacterSelectionChanged;
        }
    }
    
    private void HandleButtonToggleOnCharacterSelectionChanged()
    {
        button.interactable = PlayerManager.Instance.CharactersAssigned;
    }
    
}
