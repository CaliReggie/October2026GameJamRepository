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
    
    [SerializeField] private int pairedVisualPlayerIndex = -1;
    
    [SerializeField] private Button button;

    private void Start()
    {
        PlayerManager.Instance.OnSquirrelCorrespondingPlayerVisualIndexesChanged += UpdatePairedPlayer;
        
        button = GetComponent<Button>();
    }

    private void OnEnable() 
    {
        PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex = -1;
        PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex = -1; }
    
    private void OnDestroy() 
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnSquirrelCorrespondingPlayerVisualIndexesChanged -= UpdatePairedPlayer;
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
                    
                    if (CheckIfSelectOptionAvailableForId(clickingPlayerVisualIndex))
                    {
                        switch (selectOptionType)
                        {
                            case ECharacterSelectOptionType.SmallSquirrel:
                                PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex = clickingPlayerVisualIndex;
                                break;
                            case ECharacterSelectOptionType.BigSquirrel:
                                PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex = clickingPlayerVisualIndex;
                                break;
                            default:
                                if (clickingPlayerVisualIndex == PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex)
                                {
                                    PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex = -1;
                                }
                                if (clickingPlayerVisualIndex == PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex)
                                {
                                    PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex = -1;
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
    
    private bool CheckIfSelectOptionAvailableForId(int pointerId)
    {
        // checking others for already paired to pointerid
        foreach (CharacterSelectButton otherButton in buttonGroup)
        {
            if (otherButton != null && otherButton != this && otherButton.pairedVisualPlayerIndex == pointerId)
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
        
        // checking self for already paired to any id
        if (pairedVisualPlayerIndex != -1)
        {
            return false;
        }

        // checking general availability / pairing in management
        switch (selectOptionType)
        {
            case ECharacterSelectOptionType.SmallSquirrel:
                if (PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex == -1) return true;
                return false;
            
            case ECharacterSelectOptionType.BigSquirrel:
                if (PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex == -1) return true;
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
                pairedVisualPlayerIndex = PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex;
                break;
            case ECharacterSelectOptionType.BigSquirrel:
                pairedVisualPlayerIndex = PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex;
                break;
            default:
                pairedVisualPlayerIndex = -1;
                break;
        }
        
        if (pairedPlayerText != null)
        {
            if (pairedVisualPlayerIndex != -1)
            {
                pairedPlayerText.text = $"Player {pairedVisualPlayerIndex}";
                
                if (button != null)
                {
                    button.interactable = false;
                }
            }
            else
            {
                pairedPlayerText.text = "Unassigned";
                
                if (button != null)
                {
                    button.interactable = true;
                }
            }
        }
    }
}
