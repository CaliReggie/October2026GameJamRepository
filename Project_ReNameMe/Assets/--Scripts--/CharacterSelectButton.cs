using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class CharacterSelectButton : DeviceClickDetector
{
    private enum ECharacterSelectOptionType
    {
        Unassigned,
        SmallSquirrel,
        BigSquirrel
    }
    
    [Header("Inscribed")]
    
    [SerializeField] private ECharacterSelectOptionType selectOptionType;

    [SerializeField] private List<CharacterSelectButton> buttonGroup;
    
    [SerializeField] private TextMeshProUGUI pairedPlayerText;
        
    
    [Header("Dynamic")]
    
    [SerializeField] private int assignedVisualPlayerIndex = -1;
    
    [SerializeField] private Button button;
    

    private void Start()
    {
        button = GetComponent<Button>();
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged += UpdatePairedPlayer;
        }
    }

    private void OnEnable() 
    {
        if (PlayerManager.Instance != null)
        {
            switch (selectOptionType)
            {
                case ECharacterSelectOptionType.SmallSquirrel:
                    PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex = -1;
                    break;
                case ECharacterSelectOptionType.BigSquirrel:
                    PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex = -1;
                    break;
                default:
                    break;
            }
        }
    }
    
    private void OnDestroy() 
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged -= UpdatePairedPlayer;
        }
    }
    
    protected override void HandleClickFromId(int pointerId)
    {
        try
        {
            for (int i = 1; i < PlayerManager.Instance.NumPlayers + 1 ; i++)
            {
                if (PlayerManager.Instance.GetPlayer(i).MouseId == pointerId)
                {
                    int clickingPlayerVisualIndex = i;
                    
                    Debug.Log($"Player {clickingPlayerVisualIndex} id {pointerId}.");
                    
                    if (CheckIfSelectOptionAvailableForVisualIndex(clickingPlayerVisualIndex))
                    {
                        switch (selectOptionType)
                        {
                            case ECharacterSelectOptionType.SmallSquirrel:
                                PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex = clickingPlayerVisualIndex;
                                break;
                            case ECharacterSelectOptionType.BigSquirrel:
                                PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex = clickingPlayerVisualIndex;
                                break;
                            default:
                                if (clickingPlayerVisualIndex == PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex)
                                {
                                    PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex = -1;
                                }
                                if (clickingPlayerVisualIndex == PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex)
                                {
                                    PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex = -1;
                                }
                                break;
                        }
                    }
                    
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling click for pointer ID: {pointerId}. Exception: {e}");
        }
    }
    
    private bool CheckIfSelectOptionAvailableForVisualIndex(int visualIndex)
    {
        // checking self availability / pairing
        if (assignedVisualPlayerIndex != -1)
        {
            return false;
        }
        
        // checking others for already paired to visualIndex
        foreach (CharacterSelectButton otherButton in buttonGroup)
        {
            if (otherButton != null && otherButton != this && otherButton.assignedVisualPlayerIndex == visualIndex)
            {
                switch (selectOptionType)
                {
                    case ECharacterSelectOptionType.SmallSquirrel:
                    case ECharacterSelectOptionType.BigSquirrel:
                        if (otherButton.selectOptionType == 
                            ECharacterSelectOptionType.SmallSquirrel 
                            || otherButton.selectOptionType == ECharacterSelectOptionType.BigSquirrel)
                        {
                            return false;
                        }
                        break;
                    
                    default:
                        break;
                }
            }
        }
        

        // checking general availability / pairing in management
        switch (selectOptionType)
        {
            case ECharacterSelectOptionType.SmallSquirrel:
                if (PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex == -1)
                {
                    return true;
                }
                return false;
            
            case ECharacterSelectOptionType.BigSquirrel:
                if (PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex == -1)
                {
                    return true;
                }
                return false;
                
            default:
                return true;
        }
    }
    
    private void UpdatePairedPlayer()
    {
        switch (selectOptionType)
        {
            case ECharacterSelectOptionType.SmallSquirrel:
                assignedVisualPlayerIndex = PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex;
                break;
            case ECharacterSelectOptionType.BigSquirrel:
                assignedVisualPlayerIndex = PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex;
                break;
            default:
                assignedVisualPlayerIndex = -1;
                break;
        }
        
        if (assignedVisualPlayerIndex != -1)
        {
            if (pairedPlayerText != null)
            {
                pairedPlayerText.text = $"Player {assignedVisualPlayerIndex}";
            }
            
            if (button != null)
            {
                button.interactable = false;
            }
        }
        else
        {
            if (pairedPlayerText != null)
            {
                pairedPlayerText.text = "Unassigned";
            }
            
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
}
