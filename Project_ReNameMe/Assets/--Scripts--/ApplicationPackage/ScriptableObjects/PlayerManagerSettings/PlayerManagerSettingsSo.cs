using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerManagerSettingsSO", menuName = "ScriptableObjects/PlayerManagerSettingsSo")]
public class PlayerManagerSettingsSo : ScriptableObject
{
    #region Declarations

    [Header("Inscribed Settings")]
    
    // commented out, only used for Sos that set a setting via editor extension
    // [Space]
    //
    // [Tooltip("Press when done making changes to the SO")]
    // [SerializeField] private bool writeChanges;
    
    [Space]
        
    [Tooltip("Number of players for this configuration")]
    [SerializeField] [Range(1,4)]  private int targetPlayers = 1;
    
    [Tooltip("If true, must hit target players to start, and will remove excess players on changes to management.\n" +
             "If false, will allow just one player to start, add players until target is hit," +
             " and won't fully remove excess players on changes to management.")]
    [SerializeField] private bool strictTargetPlayers = true;
    
    [Space]

    [Tooltip("Settings for each player to use in this configuration, should be unique per player")]
    [SerializeField] private List<PlayerSettingsSo> playersSettings = new ();

    [Space]
    
    [Tooltip("If true, all players need to be requesting time pause for time to be paused")]
    [SerializeField] private bool allNeededToPauseTime = true;
    
    [Tooltip("If true, players' configuration types must match all the time.")]
    [SerializeField] private bool forceConfigurationTypeMatch = true;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("The current target player configuration type for this PlayerManager configuration")]
    [SerializeField] private PlayerSettingsSo.EPlayerConfigurationType targetPlayerConfigurationType =
        PlayerSettingsSo.EPlayerConfigurationType.Off;
    

    #endregion

    #region Properties

    /// <summary>
    /// Target number of players for this configuration
    /// </summary>
    public int TargetPlayers => targetPlayers;
    
    /// <summary>
    /// If true, must hit target players to start, and will remove excess players on changes to management.
    /// If false, will allow just one player to start, add players until target is hit,
    /// and won't fully remove excess players on changes to management.
    /// </summary>
    public bool StrictTargetPlayers => strictTargetPlayers;
    
    /// <summary>
    /// The settings for each player in this configuration
    /// </summary>
    public List<PlayerSettingsSo> PlayersSettings => playersSettings;
    
    /// <summary>
    /// If true, all players need to be requesting time pause for time to be paused
    /// </summary>
    public bool AllNeededToPauseTime => allNeededToPauseTime;
    
    /// <summary>
    /// If true, players' configuration types must match the target configuration type of PlayerManager
    /// </summary>
    public bool ForceConfigurationTypeMatch => forceConfigurationTypeMatch;

    /// <summary>
    /// The current target player configuration type for this PlayerManager configuration
    /// </summary>
    public PlayerSettingsSo.EPlayerConfigurationType TargetPlayerConfigurationType
    {
        get => targetPlayerConfigurationType;
        set => targetPlayerConfigurationType = value;
    }

    #endregion

    #region Public Methods
    
    public PlayerSettingsSo GetPlayerSettingsFromVisualIndex(int visualIndex)
    {
        if (visualIndex < 1 || visualIndex > playersSettings.Count)
        {
            Debug.LogError($"{GetType().Name}: {name}: VisualIndex {visualIndex} is out of range.");
            return null;
        }
        
        int targetIndex = visualIndex - 1;
        
        if (playersSettings[targetIndex] == null)
        {
            // if this comes up in future single player case with empty settings 2-4, not a big issue, can remove log
            Debug.LogWarning($"{GetType().Name}: {name}: PlayerSettings at visualIndex {visualIndex} is null.");
            return null;
        }
        
        return playersSettings[targetIndex];
    }
    
    public PlayerSettingsSo GetPlayerSettingsSoFromPioReference(PlayerInputObject targetPio)
    {
        if (targetPio == null)
        {
            Debug.LogError($"{GetType().Name}: {name}: Target PlayerInputObject is null.");
            return null;
        }
        
        for (int i = 0; i < playersSettings.Count; i++)
        {
            if (playersSettings[i] != null && playersSettings[i].GetHostPlayerInputObject() == targetPio)
            {
                return playersSettings[i];
            }
        }
        
        Debug.LogError($"{GetType().Name}: {name}: No PlayerSettings found for target PlayerInputObject: {targetPio.name}.");
        
        return null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// For some reason, changing values elsewhere is required to save values set by Editor extensions.
    /// Hence, the "writeChanges" boolean is used to trigger this method.
    /// </summary>
    private void OnValidate()
    {
        // commented out, see paired commented out block in inspector settings
        // if (writeChanges)
        // {
        //     writeChanges = false;
        //     
        //     Debug.Log($"Wrote changes to {GetType().Name}: {name}");
        // }
        
        // if player settings list contains null, reminding to set
        for (int i = 0; i < playersSettings.Count; i++)
        {
            if ( i < targetPlayers &&
                playersSettings[i] == null)
            {
                Debug.LogWarning($"{GetType().Name}: {name}: PlayerSettings at index {i + 1} is null" +
                                 $" and will need to be used. Please assign it.");
            }
        }
        
        // if playersSettings list size needs to be changed to match target players, change it
        if (playersSettings.Count < targetPlayers)
        {
            // add more elements
            int elementsToAdd = targetPlayers - playersSettings.Count;
            for (int i = 0; i < elementsToAdd; i++)
            {
                playersSettings.Add(null);
            }
        }
        else if (playersSettings.Count > PlayerManager.MaxPlayers)
        {
            // remove excess elements
            playersSettings.RemoveRange(PlayerManager.MaxPlayers, playersSettings.Count - PlayerManager.MaxPlayers);
        }
    }

    #endregion
}
