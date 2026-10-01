using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

public class CharacterFieldContainer : MonoBehaviour
{
    private enum ECharactersFieldType
    {
        Unassigned,
        PlayerNumber,
        Account,
        CameraSensitivity,
        CursorSensitivity,
        MasterVolume
    }
    
    [Header("Inscribed Settings")]
    
    [SerializeField] private ECharactersFieldType charactersFieldType = ECharactersFieldType.Unassigned;
    
    [Tooltip("The number of decimal places to consider when interpreting the characters as a numeric value. " +
             "For example: With a value of 2 decimal places and character value of 12345, you get 123.45.")]
    [SerializeField] private int decimalPlaces;
    
    [Header("Inscribed References")]
    
    [SerializeField] private List<CharacterField> characterSelectors;
    
    [SerializeField] private Button applyButton;
    
    [Tooltip("The CharacterFieldContainer components to sync with on modifications to an account or its settings.")]
    [SerializeField] private List<CharacterFieldContainer> associatedSettingsFields;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [Tooltip("The PlayerInputObject to use (variably) when saving/loading values related to an account." +
             " Required to function, set dynamically.")]
    [SerializeField] private PlayerInputObject associatedPlayerInputObject;
    
    [FormerlySerializedAs("applyButtonText")]
    [SerializeField] private TextMeshProUGUI applyButtonTMP;
    
    // Properties
    
    /// <summary>
    /// The string value of all current characters in the field, concatenated together.
    /// Updated when ReadCurrentCharacters or WriteCurrentCharacters is called.
    /// </summary>
    public string AllCurrentCharacters => AreCharacterSelectorsValid
        ? string.Join(null, characterSelectors.ConvertAll(selector => selector.CurrentCharacter))
        : string.Empty;
    
    private bool AreCharacterSelectorsValid => characterSelectors != null && characterSelectors.Count > 0 && !characterSelectors.Contains(null);
    
    private float GetFloatFromString(string characters)
    {
        // (if we have 5 character selectors and 2 decimal places)
        // 12345 => 123.45
        // 00100 => 1.00
        // 01001 => 10.01
        // etc

        int value = 0;
        
        if (string.IsNullOrEmpty(characters))
        {
            return 0f;
        }

        foreach (char c in characters)
        {
            if (!char.IsDigit(c)) {continue;} 
            
            int digit = c - '0'; 
            
            value = value * 10 + digit;
        }
        
        return value / Mathf.Pow(10, decimalPlaces);
    }
    
    private string GetStringFromFloat(float value)
    {
        //(if we have 5 character selectors and 2 decimal places)
        // .97 => 000.97 
        // 1  => 001.00
        // 10.01 => 010.01
        // 937.46 => 937.46
        
        int scaled = Mathf.RoundToInt(value * Mathf.Pow(10, decimalPlaces));
        
        return scaled.ToString().PadLeft(AllCurrentCharacters.Length, '0');
        
    }
    
    private void Awake()
    {
        //checking for null references
        if (!AreCharacterSelectorsValid ||
            applyButton == null)
        {
            Debug.LogError("CharacterFieldContainer: One or more references are not set in the inspector.");
        }
        
        // Set up button listener
        applyButton.onClick.AddListener(WriteCurrentCharacters);
        
        // Set up character change listeners
        foreach (var selector in characterSelectors)
        {
            selector.OnCharacterChanged += OnCharactersChanged;
        }
        
        if (associatedSettingsFields != null &&
            !associatedSettingsFields.Contains(this))
        {
            associatedSettingsFields.Add(this);
        }
        else if (associatedSettingsFields == null)
        {
            associatedSettingsFields = new List<CharacterFieldContainer> { this };
        }
        
        if (applyButtonTMP == null)
        {
            applyButtonTMP = applyButton.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
    
    private void OnEnable()
    {
        ReadCurrentCharacters();
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(WriteCurrentCharacters);
        }
        
        // Clean up character change listeners
        if (characterSelectors != null)
        {
            foreach (var selector in characterSelectors)
            {
                if (selector != null)
                {
                    selector.OnCharacterChanged -= OnCharactersChanged;
                }
            }
        }
    }

    private void Start()
    {
        // if does get set, acts as a pair to the parent player,
        // if not, still functions: is paired the current managed player number in the settings interface
        if (associatedPlayerInputObject == null)
        {
            associatedPlayerInputObject = GetComponentInParent<PlayerInputObject>();    
        }
        
        ReadCurrentCharacters();
    }
    
     private void OnCharactersChanged()
    {
        if (applyButton == null)
        {
            return;
        }
        
        int associatedPlayerNumber = TryGetAssociatedPlayerNumber();
        
        if (associatedPlayerNumber < 1 &&
            charactersFieldType != ECharactersFieldType.PlayerNumber &&
            charactersFieldType != ECharactersFieldType.Account)
        {
            applyButton.interactable = false;
        }
        else
        {
            switch (charactersFieldType)
            {
                default:
                case ECharactersFieldType.Unassigned:
                    
                    applyButton.interactable = false;
                    
                    break;
                case ECharactersFieldType.PlayerNumber:
                    
                    try
                    {
                        // if the player number is already associated with a player in settings,
                        // only allow applying if it's associated with this field's current player
                        // (to prevent accidentally changing other player's associations)
                        applyButton.interactable = PlayerManager.Instance != null &&
                                                   PlayerManager.Instance.GetPlayer((int) GetFloatFromString(AllCurrentCharacters)).VisualIndex
                                                   != associatedPlayerInputObject.VisualIndex;
                    }
                    catch (NullReferenceException) // possible player doesn't exist for the given player number
                    {
                        // if the player number isn't currently associated with a player in settings,
                        // allow applying to set the association (unless the player number is invalid, handled above)
                        applyButton.interactable = TryGetAssociatedPlayerNumber() < 1;
                    }
                    
                    break;
                
                case ECharactersFieldType.Account:
                    
                    string associatedAccount = PlayerPrefsManager.TryGetAccountFromPlayerNumber(associatedPlayerNumber);
                    
                    try
                    {
                        Image applyButtonImage = applyButton.GetComponent<Image>();
                        
                        if (!string.IsNullOrEmpty(associatedAccount)) // associated acct with this player num alr exists
                        {
                            applyButton.interactable = PlayerPrefsManager.TryGetPlayerNumberFromAccount(AllCurrentCharacters) < 1;

                            applyButtonImage.color =
                                PlayerPrefsManager.TryGetPlayerNumberFromAccount(AllCurrentCharacters) ==
                                associatedPlayerNumber ? Color.white :
                                    applyButton.interactable ? Color.white : Color.red;
                        }
                        else // no associated acct with this player num exists currently
                        {
                            applyButton.interactable = !PlayerPrefsManager.IsPlayerAccountInUse(AllCurrentCharacters);
                            applyButtonImage.color = applyButton.interactable ? Color.white : Color.red;
                        }
                    }
                    catch (NullReferenceException) // possible something doesn't exist
                    {
                        
                    }
                    
                    break;
                case ECharactersFieldType.CameraSensitivity:
                    
                    string associatedAccountForCameraSensitivity = PlayerPrefsManager.TryGetAccountFromPlayerNumber(associatedPlayerNumber);
                    
                    float savedCameraSensitivity = PlayerPrefsManager.TryGetAccountCameraSensitivity(associatedAccountForCameraSensitivity);
                    
                    float currentCameraSensitivity = GetFloatFromString(AllCurrentCharacters);
                    
                    applyButton.interactable = !string.IsNullOrEmpty(associatedAccountForCameraSensitivity) && !Mathf.Approximately(savedCameraSensitivity, currentCameraSensitivity);
                    
                    break;
                
                case ECharactersFieldType.CursorSensitivity:
                    
                    string associatedAccountForCursorSensitivity = PlayerPrefsManager.TryGetAccountFromPlayerNumber(associatedPlayerNumber);
                    
                    float savedCursorSensitivity = PlayerPrefsManager.TryGetAccountCursorSensitivity(associatedAccountForCursorSensitivity);
                    
                    float currentCursorSensitivity = GetFloatFromString(AllCurrentCharacters);
                    
                    applyButton.interactable = !string.IsNullOrEmpty(associatedAccountForCursorSensitivity) && !Mathf.Approximately(savedCursorSensitivity, currentCursorSensitivity);
                    
                    break; 
                
                case ECharactersFieldType.MasterVolume:
                    
                    string associatedAccountForMasterVolume = PlayerPrefsManager.TryGetAccountFromPlayerNumber(associatedPlayerNumber);
                    
                    float savedMasterVolume = PlayerPrefsManager.TryGetAccountMasterVolume(associatedAccountForMasterVolume);
                    
                    float currentMasterVolume = GetFloatFromString(AllCurrentCharacters);
                    
                    applyButton.interactable = !string.IsNullOrEmpty(associatedAccountForMasterVolume) && !Mathf.Approximately(savedMasterVolume, currentMasterVolume);
                    
                    break;
            } 
        }
        
        if (applyButtonTMP != null)
        {
            applyButtonTMP.text = applyButton.interactable ? "Apply" : "Applied";
        }
    }
    
    private void ReadCurrentCharacters()
    {
        switch (charactersFieldType)
        {
            default:
            case ECharactersFieldType.Unassigned:
                // nada
                break;
            case ECharactersFieldType.PlayerNumber:
                try
                {
                    PlayerInputObject targetInputObject = PlayerManager.Instance.GetPlayer(TryGetAssociatedPlayerNumber());
                    
                    associatedPlayerInputObject = targetInputObject;
                }
                catch (NullReferenceException) // possible player manager or player doesn't exist for the given player number
                {
                    try
                    {
                        associatedPlayerInputObject = FindAnyObjectByType<PlayerInputObject>();
                    }
                    catch (NullReferenceException) // possible no PlayerInputObject exists in the scene
                    {
                        Debug.LogError("No PlayerInputObject exists in the scene. Cannot set player reference.");
                    }
                }
                
                string associatedPlayerNumberString = GetStringFromFloat(Mathf.Abs(TryGetAssociatedPlayerNumber())).PadLeft(characterSelectors.Count, '0');
                
                for ( int i = 0; i < associatedPlayerNumberString.Length; i++)
                {
                    if (i >= characterSelectors.Count)
                    {
                        Debug.LogWarning("Associated player number has more digits than available character selectors." +
                                         " Extra digits will be ignored.");
                        break;
                    }
                    
                    characterSelectors[i].SetCharacterText(associatedPlayerNumberString[i].ToString());
                }

                break;
            case ECharactersFieldType.Account:
                
                string savedAccount = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                if (!string.IsNullOrEmpty(savedAccount))
                {
                    for ( int i = 0; i < savedAccount.Length; i++)
                    {
                        if (i >= characterSelectors.Count)
                        {
                            Debug.LogWarning("Saved account name has more characters than available character selectors." +
                                             " Extra characters will be ignored.");
                            break;
                        }
                        
                        characterSelectors[i].SetCharacterText(savedAccount[i].ToString());
                    }
                }
                break;
            case ECharactersFieldType.CameraSensitivity:
                
                string associatedAccountForCameraSensitivity = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                float savedCameraSensitivity = PlayerPrefsManager.TryGetAccountCameraSensitivity(associatedAccountForCameraSensitivity);
                
                string cameraSensitivityCharacters = GetStringFromFloat(savedCameraSensitivity);
                
                for ( int i = 0; i < cameraSensitivityCharacters.Length; i++)
                {
                    if (i >= characterSelectors.Count)
                    {
                        Debug.LogWarning("Camera sensitivity value has more characters than available character selectors." +
                                         " Extra characters will be ignored.");
                        break;
                    }
                    
                    characterSelectors[i].SetCharacterText(cameraSensitivityCharacters[i].ToString());
                }
                
                break;
            
            case ECharactersFieldType.CursorSensitivity:
                
                string associatedAccountForCursorSensitivity = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                float savedCursorSensitivity = PlayerPrefsManager.TryGetAccountCursorSensitivity(associatedAccountForCursorSensitivity);
                
                string cursorSensitivityCharacters = GetStringFromFloat(savedCursorSensitivity);
                
                for ( int i = 0; i < cursorSensitivityCharacters.Length; i++)
                {
                    if (i >= characterSelectors.Count)
                    {
                        Debug.LogWarning("Cursor sensitivity value has more characters than available character selectors." +
                                         " Extra characters will be ignored.");
                        break;
                    }
                    
                    characterSelectors[i].SetCharacterText(cursorSensitivityCharacters[i].ToString());
                }
                
                break;
                
            case ECharactersFieldType.MasterVolume:
                
                string associatedAccountForMasterVolume = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                float savedMasterVolume = PlayerPrefsManager.TryGetAccountMasterVolume(associatedAccountForMasterVolume);
                
                string masterVolumeCharacters = GetStringFromFloat(savedMasterVolume);
                
                for ( int i = 0; i < masterVolumeCharacters.Length; i++)
                {
                    if (i >= characterSelectors.Count)
                    {
                        Debug.LogWarning("Master volume value has more characters than available character selectors." +
                                         " Extra characters will be ignored.");
                        break;
                    }
                    
                    characterSelectors[i].SetCharacterText(masterVolumeCharacters[i].ToString());
                }
                
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.UpdateSoundSettings(associatedAccountForMasterVolume);
                }
                
                break;
        }
        
        if (associatedPlayerInputObject != null)
        {
            associatedPlayerInputObject.UpdateCurrentPlayerSettings();
        }

        OnCharactersChanged();
    }
    
    private void WriteCurrentCharacters()
    {
        switch (charactersFieldType)
        {
            default:
            case ECharactersFieldType.Unassigned:
                // nada
                break;
            case ECharactersFieldType.PlayerNumber:
                try
                {
                    int initialI = (int) GetFloatFromString(AllCurrentCharacters);
                    
                    // if player prefs are null for any lower players, set to that
                    // todo: can remove this if don't want, or want to change behaviour
                    for (int i = initialI; i > 0; i--)
                    {
                        if (PlayerPrefsManager.TryGetAccountFromPlayerNumber(i) == null)
                        {
                            string numToSet = i.ToString().PadLeft(characterSelectors.Count, '0');
                            
                            if (i < initialI)
                            {
                                Debug.LogWarning($"Player {i} still is not associated with an account." +
                                      $" Setting player {i} account to the current account associated with this field.");
                                
                                for ( int j = 0; j < numToSet.Length; j++)
                                {
                                    if (j >= characterSelectors.Count)
                                    {
                                        Debug.LogWarning("Associated player number has more digits than available character selectors." +
                                                         " Extra digits will be ignored.");
                                        break;
                                    }
                                
                                    characterSelectors[j].SetCharacterText(numToSet[j].ToString());
                                }
                            }
                        }
                    }
                    
                    associatedPlayerInputObject = PlayerManager.Instance.GetPlayer((int) GetFloatFromString(AllCurrentCharacters));
                }
                catch (NullReferenceException) // possible player manager or player doesn't exist for the given player number
                {
                    try 
                    {
                        associatedPlayerInputObject = FindAnyObjectByType<PlayerInputObject>();
                        
                        Debug.Log($"{associatedPlayerInputObject.name} was found in the scene.");
                    }
                    catch (NullReferenceException) // possible no PlayerInputObject exists in the scene
                    {
                        Debug.LogError("No PlayerInputObject exists in the scene. Cannot set player reference.");
                        
                        return;
                    }
                }
          
                PlayerPrefsManager.SetPlayerAccount((int) GetFloatFromString(AllCurrentCharacters), PlayerPrefsManager.TryGetAccountFromPlayerNumber((int) GetFloatFromString(AllCurrentCharacters)));
                
                break;
            
            case ECharactersFieldType.Account:
                
                PlayerPrefsManager.SetPlayerAccount(TryGetAssociatedPlayerNumber(), AllCurrentCharacters);
                
                break;
            case ECharactersFieldType.CameraSensitivity:
                
                string associatedAccountForCameraSensitivity = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                if (!string.IsNullOrEmpty(associatedAccountForCameraSensitivity))
                {
                    float cameraSensitivityToSave = GetFloatFromString(AllCurrentCharacters);
                
                    PlayerPrefsManager.SetAccountCameraSensitivity(associatedAccountForCameraSensitivity, cameraSensitivityToSave);
                }
                
                break;
            
            case ECharactersFieldType.CursorSensitivity:
                
                string associatedAccountForCursorSensitivity = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                if (!string.IsNullOrEmpty(associatedAccountForCursorSensitivity))
                {
                    float cursorSensitivityToSave = GetFloatFromString(AllCurrentCharacters);
                
                    PlayerPrefsManager.SetAccountCursorSensitivity(associatedAccountForCursorSensitivity, cursorSensitivityToSave);
                }
                
                break;
            
            case ECharactersFieldType.MasterVolume:
                
                string associatedAccountForMasterVolume = PlayerPrefsManager.TryGetAccountFromPlayerNumber(TryGetAssociatedPlayerNumber());
                
                if (!string.IsNullOrEmpty(associatedAccountForMasterVolume))
                {
                    float masterVolumeToSave = GetFloatFromString(AllCurrentCharacters);
                
                    PlayerPrefsManager.SetAccountMasterVolume(associatedAccountForMasterVolume, masterVolumeToSave);
                    
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.UpdateSoundSettings(associatedAccountForMasterVolume);
                    }
                }
                
                break;
        }

        ReadCurrentCharacters();
        
        // Notify associated settings fields to update their values based on the new account or settings
        foreach (var field in associatedSettingsFields)
        {
            if (field != null && field != this)
            {
                field.ReadCurrentCharacters();
            }
        }
        
        if (associatedPlayerInputObject != null)
        {
            associatedPlayerInputObject.UpdateCurrentPlayerSettings();
        }
    }
    
    private int TryGetAssociatedPlayerNumber()
    {
        if (associatedPlayerInputObject != null)
        {
            return associatedPlayerInputObject.VisualIndex;
        }
        else
        {
            switch (charactersFieldType)
            {
                default:
                case ECharactersFieldType.Unassigned:
                    return -1;
                
                case ECharactersFieldType.PlayerNumber:
                    int allCurrentCharactersAsInt = (int) GetFloatFromString(AllCurrentCharacters);
                    return !string.IsNullOrEmpty(PlayerPrefsManager.TryGetAccountFromPlayerNumber(allCurrentCharactersAsInt))
                        ? allCurrentCharactersAsInt 
                        : 1;
                
                case ECharactersFieldType.Account:
                    return associatedSettingsFields.Find(field => field.charactersFieldType == ECharactersFieldType.PlayerNumber)?.TryGetAssociatedPlayerNumber()
                                  ?? PlayerPrefsManager.TryGetPlayerNumberFromAccount(AllCurrentCharacters);
                
                case ECharactersFieldType.CameraSensitivity:
                case ECharactersFieldType.CursorSensitivity:
                case ECharactersFieldType.MasterVolume:
                    return associatedSettingsFields.Find(field => field.charactersFieldType == ECharactersFieldType.PlayerNumber)?.TryGetAssociatedPlayerNumber() 
                                  ?? associatedSettingsFields.Find(field => field.charactersFieldType == ECharactersFieldType.Account)?.TryGetAssociatedPlayerNumber() 
                                  ?? 1;
            }
        }
    }
}
