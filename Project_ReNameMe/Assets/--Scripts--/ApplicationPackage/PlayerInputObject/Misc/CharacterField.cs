using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CharacterField : MonoBehaviour
{
    public event Action OnCharacterChanged;
    
    [Header("Inscribed References")]
    
    [SerializeField] private TextMeshProUGUI characterText;
    
    [SerializeField] private Button increaseCharacterButton;
    
    [SerializeField] private Button decreaseCharacterButton;
    
    [Header("Inscribed Settings")]
    
    [Tooltip("A string of the available characters to choose from, seperated by commas." +
             " The first element will be the default character.")]
    [SerializeField] private string charactersCsv;

    [Tooltip("If trie, will override charactersCsv to be the player numbers in game. Ex if 3 players: '1,2,3'")]
    [SerializeField] private bool overrideCharactersCsvToNumPlayers;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [SerializeField] private List<string> characters;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [SerializeField] private int currentCharacterIndex = 0;
    
    // Properties
    
    public string CurrentCharacter => characters != null && characters.Count > 0 ? characters[currentCharacterIndex] : null;

    //Methods
    
    public void SetCharacterText(string character)
    {
        if (!AreCharactersValid())
        {
            return;
        }
        
        if (string.IsNullOrEmpty(character))
        {
            Debug.LogError("Passed character text is null or empty..");
            
            return;
        }
        
        if (characters.Contains(character))
        {
            currentCharacterIndex = characters.IndexOf(character);
        }
        
        characterText.text = character;
        
        OnCharacterChanged?.Invoke();
    }
    
    private void Awake()
    {
        //checking inscribed references
        if (characterText == null ||
            increaseCharacterButton == null ||
            decreaseCharacterButton == null)
        {
            Debug.LogError("One or more inscribed references are not set in the inspector.");
            
            return;
        }
        
        //setting up button listeners
        increaseCharacterButton.onClick.AddListener(IncreaseCharacter);
        
        decreaseCharacterButton.onClick.AddListener(DecreaseCharacter);
        
        //initializing characters from csv
        SetCharacterIndex(0);
    }

    private void OnDestroy()
    {
        //cleaning up button listeners
        if (increaseCharacterButton != null)
        {
            increaseCharacterButton.onClick.RemoveListener(IncreaseCharacter);
        }
        
        if (decreaseCharacterButton != null)
        {
            decreaseCharacterButton.onClick.RemoveListener(DecreaseCharacter);
        }
    }

    private void ParseCharacters()
    {
        // currently not extensive error checking or special handling, assuming setup correctly
        if (overrideCharactersCsvToNumPlayers)
        {
            int numPlayers = Mathf.Min(
                PlayerManager.Instance != null && PlayerManager.Instance.CurrentPlayerManagerSettings != null
                    ? PlayerManager.Instance.CurrentPlayerManagerSettings.TargetPlayers
                    : 1,
                PlayerManager.Instance != null
                    ? PlayerManager.Instance.NumPlayers
                    : 1);

            characters = new();
            
            for (int i = 1; i <= numPlayers; i++)
            {
                characters.Add(i.ToString());
            }
        }
        else
        {
            characters = new List<string>(charactersCsv.Split(','));
        }
    }
    
    private bool AreCharactersValid()
    {
        ParseCharacters();
        
        if (characters == null || characters.Count == 0)
        {
            return false;
        }

        return true;
    }
    
    private void SetCharacterIndex(int index)
    {
        if (!AreCharactersValid() || index < 0 || index >= characters.Count)
        {
            Debug.LogError($"Invalid character index: {index}." +
                           $" Please ensure the characters list is set up correctly and the index is within bounds.");
            
            return;
        }
        
        currentCharacterIndex = index;
        SetCharacterText(characters[currentCharacterIndex]);
        
        OnCharacterChanged?.Invoke();
    }
    
    private void IncreaseCharacter()
    {
        if (!AreCharactersValid())
        {
            return;
        }
        
        int newIndex = (currentCharacterIndex + 1) % characters.Count;
        SetCharacterIndex(newIndex);
    }
    
    private void DecreaseCharacter()
    {
        if (!AreCharactersValid())
        {
            return;
        }
        
        int newIndex = (currentCharacterIndex - 1 + characters.Count) % characters.Count;
        SetCharacterIndex(newIndex);
    }
}
