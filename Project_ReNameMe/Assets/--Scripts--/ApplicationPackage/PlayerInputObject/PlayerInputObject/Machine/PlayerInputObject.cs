using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The PlayerInputObject (Pio) class acts as a GameObject container for a combination and mix-and-match style
/// of MonoBehaviour PioComponents that handle different aspects of player functionality such as object control,
/// camera control, ui interaction, and cursor control. Functionality is tied heavily into Unity's InputSystem package
/// and related packages like CineMachine for camera control. The Pio derives from the BaseStateMachine class,
/// using an abstract framework for defining states, transitions, and settings in those states.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputObject : BaseStateMachine<PlayerInputObject.EPlayerInputObjectState>
{
    public enum EPlayerInputObjectState
    {
        [InspectorName(null)] // Don't want this to show as setting in inspector
        [Tooltip("Initializing the Player Input Object and its components, settings refs, etc.")]
        ObjectInitialize,
        [InspectorName(null)]
        [Tooltip("No components or input maps active for the player, inclusion reliant on SceneCamera view.")]
        Off,
        [Tooltip("PlayerObject and PlayerCamera PioComponents active with Player input map.")]
        Player,
        [Tooltip("PlayerObject, PlayerCamera, Ui(Player Object's), and PlayerCursor PioComponents active with UI input map.")]
        PlayerUi,
        [Tooltip("PlayerObject, PlayerCamera, Ui(Scene's), and PlayerCursor PioComponents active with UI input map.")]
        PlayerSceneUi,
        [Tooltip("Ui(Scene's) and PlayerCursor PioComponents active with UI input map.")]
        SceneUi
    }
    
    #region State Class
        
        /// <summary>
        /// Base Pio state class that all Pio states derive from.
        /// </summary>
        public abstract class NewPlayerInputObjectState : BaseState<EPlayerInputObjectState>
        {
            protected NewPlayerInputObjectState(PlayerInputObjectContext context, 
                EPlayerInputObjectState key, 
                EPlayerInputObjectState[] invalidTransitions) 
                : base(key, invalidTransitions)
            {
                Context = context;
            }
            
            protected PlayerInputObjectContext Context { get; }
        }
        
        #endregion
        
    #region Context Class
    
    /// <summary>
    /// Pio context class that contains all relevant references and settings for the PlayerInputObject.
    /// </summary>
    [Serializable]
    public class PlayerInputObjectContext : BaseStateMachineContext 
    {
        #region Context Declarations
        
        /// <summary>
        /// The different input action maps available for the PlayerInputObject as defined in the InputActions asset.
        /// </summary>
        public enum EInputActionMap
        {  
            None,
            Player,
            UI 
        }
    
        /// <summary>
        /// Dictionary that maps EInputActionMap enum values to their corresponding string names as defined
        /// in the InputActions asset. Null results in no action map being set.
        /// </summary>
        private static readonly Dictionary<EInputActionMap, string> ActionMapTypeNames = new ()
        {
            { EInputActionMap.None, null }, 
            { EInputActionMap.Player, "Player" },
            { EInputActionMap.UI, "UI" }
        };
        
        [Header("Inscribed References")]
        
        [Tooltip("The default PlayerSettingsSo to use if no PlayerManager is present.")]
        public PlayerSettingsSo defaultPlayerSettings;

        [Tooltip("The PioComponents attached / childed to the Pio GameObject.")]
        public List<PioComponent> playerInputObjectComponents;

        [Header("Dynamic References - Don't Modify In Inspector")]
        
        [Tooltip("The Pio script/GameObject this context belongs to.")]
        public PlayerInputObject playerInputObject;
        
        [Tooltip("The PlayerInput component on the Pio GameObject.")]
        public PlayerInput playerInput;
        
        [Tooltip("The current PlayerSettingsSo assigned to this Pio.")]
        public PlayerSettingsSo currentPlayerSettings;

        [Header("Dynamic Settings - Don't Modify In Inspector")]

        [Tooltip("Flag to know if all PioComponents have been initialized.")]
        public bool componentsInitialized;
        
        [Tooltip("Tracker used for individual configuration setting when requesting a change from " +
                 "Player Manager that doesn't force configuration type to match.")]
        public PlayerSettingsSo.EPlayerConfigurationType requestedManagedConfigurationType =
            PlayerSettingsSo.EPlayerConfigurationType.Off;

        #endregion
        
        #region Base Context Methods

        public override Dictionary<EPlayerInputObjectState, BaseState<EPlayerInputObjectState>> 
            InitializedContextStates(BaseStateMachine<EPlayerInputObjectState> targetStateMachine)
        {
            
            Dictionary<EPlayerInputObjectState, EPlayerInputObjectState[]> stateTransitions = new()
            {
                { EPlayerInputObjectState.ObjectInitialize, Array.Empty<EPlayerInputObjectState>() }, // Go anywhere
                { EPlayerInputObjectState.Off, new [] { EPlayerInputObjectState.ObjectInitialize }}, // No re init
                { EPlayerInputObjectState.Player, new [] { EPlayerInputObjectState.ObjectInitialize } }, // No re init
                { EPlayerInputObjectState.PlayerUi, new [] { EPlayerInputObjectState.ObjectInitialize } }, // No re init
                { EPlayerInputObjectState.PlayerSceneUi, new [] { EPlayerInputObjectState.ObjectInitialize } }, // No re init
                { EPlayerInputObjectState.SceneUi, new [] { EPlayerInputObjectState.ObjectInitialize } } // No re init
            };
            
            Dictionary<EPlayerInputObjectState, BaseState<EPlayerInputObjectState>> stateConstructions = new();

            foreach (var state in stateTransitions)
            {
                switch (state.Key)
                {
                    case EPlayerInputObjectState.ObjectInitialize:
                        stateConstructions.Add(state.Key, new PioInitialize(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.Off:
                        stateConstructions.Add(state.Key, new PioOff(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.Player:
                        stateConstructions.Add(state.Key, new PioPlayerObject(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.PlayerUi:
                        stateConstructions.Add(state.Key, new PioPlayerObjectUi(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.PlayerSceneUi:
                        stateConstructions.Add(state.Key, new PioPlayerObjectSceneUi(this, state.Key, state.Value));
                        break;
                    case EPlayerInputObjectState.SceneUi:
                        stateConstructions.Add(state.Key, new PioSceneUi(this, state.Key, state.Value));
                        break;
                }
            }
            
            // assigning target state machine as Pio
            playerInputObject = (PlayerInputObject) targetStateMachine;
            
            // checking inscribed references
            if (defaultPlayerSettings == null ||
                playerInputObjectComponents == null ||
                playerInputObjectComponents.Contains(null)
                )
            {
                // if errors, log and destroy go
                Debug.LogError($"{GetType().Name}: No defaultPlayerSettings assigned in Pio Context. " +
                               $"Destroying PlayerInputObject.");
                
                Destroy(playerInputObject.gameObject);
                
                return null;
            }
            
            // ensure all components start enabled and objects on (they turn selves off once initialized)
            foreach (PioComponent component in playerInputObjectComponents)
            {
                component.enabled = true;
                component.gameObject.SetActive(true);
            }
            
            // assigning PlayerInput component
            playerInput = playerInputObject.GetComponent<PlayerInput>();
            
            // renaming GO
            playerInputObject.name = ($"Player{playerInputObject.VisualIndex}InputObject");
            
            // if there is a player manager
            if (playerInputObject.IsPlayerManager)
            { 
                try
                {
                    // getting the settings of corresponding player number
                    PlayerSettingsSo targetPlayerSettings = 
                        PlayerManager.Instance.CurrentPlayerManagerSettings.GetPlayerSettingsFromVisualIndex(playerInputObject.VisualIndex);

                    // getting target configuration type from player manager settings
                    PlayerSettingsSo.EPlayerConfigurationType targetConfigurationType =
                        PlayerManager.Instance.CurrentPlayerManagerSettings.TargetPlayerConfigurationType;
                
                    // plugging the target configuration settings into the matched player settings
                    targetPlayerSettings.CurrentConfigurationType = targetConfigurationType;
                    
                    // setting current player settings to matched
                    SetPlayerSettings(targetPlayerSettings);
                }
                catch (Exception e)
                {
                    // use default if error and log it
                    SetPlayerSettings(defaultPlayerSettings);
                    
                    Debug.LogError($"{playerInputObject.name}:" +
                                   $" Error in matching PlayerSettingsSo on PlayerManager init:\n{e}");
                }
            }
            // if not VV
            // starting without player manager handled in exit of PioInitialize state, 
            // have to wait for components to initialize.
            
            // Pio needs states initialized for its functionality
            return stateConstructions;
        }

        /// <summary>
        /// BaseStateMachineContext method necessary to override, BUT not used due to target state management system for
        /// the Pio. Logs an error if called to indicate no effect. Use ChangePlayerSettingsConfigurationType
        /// with a configuration that contains a target state instead.
        /// </summary>
        public override void ContextCallChangeState(EPlayerInputObjectState newState)
        {
            // log error this was called and will have no effect
            Debug.LogError($"{playerInputObject.name}: ContextCallChangeState was called with {newState}, " +
                           $"but PlayerInputObject uses target state management. No effect.");
        }

        #endregion
        
        #region State And State Settings Context Methods
        
        /// <summary>
        /// Sets the current PlayerSettingsSo for this Pio and invokes the OnAfterPlayerSettingsChanged event.
        /// </summary>
        public void SetPlayerSettings(PlayerSettingsSo newSettings)
        {
            // cannot set if null
            if (newSettings == null)
            {
                Debug.LogError($"{playerInputObject.name}: Cannot set PlayerSettingsSo, newSettings is null.");
                return;
            }
            
            // assigning new settings
            currentPlayerSettings = newSettings;

            currentPlayerSettings.SetHostPlayerInputObject(playerInputObject);
            
            // invoking event for change
            playerInputObject.OnAfterPlayerSettingsChanged?.Invoke(newSettings);
            
            requestedManagedConfigurationType = newSettings.CurrentConfigurationType;
            
            if (playerInputObject.DebugMode)
            {
                Debug.Log($"{playerInputObject.name}: Set PlayerSettingsSo to {newSettings.name}");
            }
        }
        
        public void ClearPlayerSettings()
        {
            if (currentPlayerSettings == null)
            {
                Debug.LogWarning($"{playerInputObject.name}: PlayerSettingsSo is already null.");
                return;
            }
            
            currentPlayerSettings.ClearHostPlayerInputObject();
            
            // unsetting current settings
            currentPlayerSettings = null;
            
            // todo: would have to support clearing for components to receive a null value and act according
            // playerInputObject.OnAfterPlayerSettingsChanged?.Invoke(null);
            
            if (playerInputObject.DebugMode)
            {
                Debug.Log($"{playerInputObject.name}: Unset PlayerSettingsSo, set to null.");
            }
        }
        
        /// <summary>
        /// Causes the Pio to change the current PlayerSettingsSo's configuration type to the target type given.
        /// The logic for the behaviour of that configuration type being defined in the PlayerSettingsSo and
        /// then respected by the Pio and its PioComponents.
        /// </summary>
        public void ChangePlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType configurationType)
        {
            // cannot change if null
            if (currentPlayerSettings == null)
            {
                if (playerInputObject.DebugMode)
                {
                    Debug.LogWarning($"{playerInputObject.name}: currentPlayerSettings is null during settings change.");
                }
                
                return;
            }
            
            // assigning new configuration type
            currentPlayerSettings.CurrentConfigurationType = configurationType;
            
            // if not managed by PlayerManager, handle certain aspects related to TargetStateSettings (like timeScale).
            if (!playerInputObject.IsPlayerManager)
            {
                Time.timeScale = currentPlayerSettings.CurrentConfiguration.PauseTime ? 0f : 1f;
            }
            
            // invoking event for change
            playerInputObject.OnAfterPlayerSettingsChanged?.Invoke(currentPlayerSettings);
            
            requestedManagedConfigurationType = configurationType;
            
            if (playerInputObject.DebugMode)
            {
                    Debug.Log($"{playerInputObject.name}: Toggled PlayerStateSettings to " +
                          $"{currentPlayerSettings.CurrentConfiguration.State}");
            }
        }
        
        /// <summary>
        /// Gets subscribed to PlayerManager's OnAfterPlayerManagerSettingsChange event to handle
        /// managing changes in the PlayerManagerSettingsSo that may affect this Pios settings.
        /// </summary>
        public void OnAfterPlayerManagerSettingsChange(PlayerManagerSettingsSo currentPlayerManagerSettings)
        {
            // on change, always first ensure that we have the correct player numbers settings
            try
            {
                // getting target configuration type from player manager settings
                PlayerSettingsSo.EPlayerConfigurationType targetConfigurationType =
                    currentPlayerManagerSettings.TargetPlayerConfigurationType;
                
                // getting the settings of corresponding player number
                PlayerSettingsSo targetPlayerSettings =
                    currentPlayerManagerSettings.GetPlayerSettingsFromVisualIndex(playerInputObject.VisualIndex);
                
                // possible to be null during player count / settings changes,
                // or be a higher player than target. Intended, not an error just return
                if (targetPlayerSettings == null ||
                    playerInputObject.VisualIndex > currentPlayerManagerSettings.TargetPlayers)
                {
                    ChangePlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Off);
                    return;
                }
                
                // if different from current, set
                if (currentPlayerSettings != targetPlayerSettings)
                {
                    SetPlayerSettings(targetPlayerSettings);
                }
                // else, still need to set target state settings
                else
                {
                    // if forced to match PlayerManager simply change to given target
                    if (currentPlayerManagerSettings.ForceConfigurationTypeMatch)
                    {
                        ChangePlayerSettingsConfigurationType(targetConfigurationType);
                    }
                    else // if not forced, few other cases to consider
                    {
                        // first of importance. While not forced, currently no good way (should work on)
                        // to track if it is an individual request or not. The solution to this is a 
                        // tracker for the last requested configuration type. While this allows for freedom,
                        // it ignores important cases like turning on from off, or off from on.
                        // For now, first will check the off to on, or on to off cases and respond accordingly
                        // If neither of those, could be a solo request
                        bool offToOn = currentPlayerSettings.CurrentConfigurationType == PlayerSettingsSo.EPlayerConfigurationType.Off &&
                                      targetConfigurationType != PlayerSettingsSo.EPlayerConfigurationType.Off;
                        
                        bool onToOff = currentPlayerSettings.CurrentConfigurationType != PlayerSettingsSo.EPlayerConfigurationType.Off && 
                                      targetConfigurationType == PlayerSettingsSo.EPlayerConfigurationType.Off;
                        
                        if (offToOn || onToOff)
                        {
                            ChangePlayerSettingsConfigurationType(targetConfigurationType);
                        }
                        else if (currentPlayerSettings.CurrentConfigurationType != requestedManagedConfigurationType)
                        {
                            ChangePlayerSettingsConfigurationType(requestedManagedConfigurationType);
                        }
                    }
                }
            }
            // logging error if something went wrong
            catch (Exception e)
            {
                Debug.LogError($"{playerInputObject.name}:" +
                               $" Error in matching PlayerSettingsSo on PlayerManager settings change:\n{e}");
            }
        }
        
        #endregion

        #region Input Map Context Methods

        /// <summary>
        /// Function to set the current input action map. Disables all other action maps first.
        /// If target map type corresponds to null/None, all maps are disabled.
        /// Should be used when transitioning to/from states based
        /// on the desired map for relevant listener components to be able to function by receiving messages from
        /// the correct map.
        /// </summary>
        public void SetCurrentInputActionMap(EInputActionMap targetActionMapType)
        {
            try
            {
                playerInput.actions.Disable();
                
                if (targetActionMapType == EInputActionMap.None) { return; }
                
                playerInput.SwitchCurrentActionMap(ActionMapTypeNames[targetActionMapType]);
            }
            // logging error if something went wrong (likely corresponding map not found)
            catch (Exception e)
            {
                Debug.LogError($"{playerInputObject.name}: Error in setting current action map: \n{e}");
            }
            
            if (playerInputObject.DebugMode)
            {
                Debug.Log($"{playerInputObject.name}: Set current action map to {targetActionMapType}");
            }
        }

        #endregion
    }
    
    #endregion
    
    #region Variables, Properties, Fields, Events
    
    //ADDED FOR SQUIRREL GAME
    public bool IsSmallSquirrel => PlayerManager.Instance != null &&
                                   PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex == VisualIndex;
    
    public bool IsBigSquirrel => PlayerManager.Instance != null &&
                                   PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex == VisualIndex;
    
    public int MouseId
    {
        get
        {
            foreach (PioComponent component in context.playerInputObjectComponents)
            {
                if (component is PlayerCursorPioComponent cursor)
                {
                    return cursor.MouseId;
                }
            }
            
            return -1;
        }
    }
    
    /// <summary>
    /// Flag to know if this Pio and PioComponents are initialized and ready to use.
    /// </summary>
    private bool Initialized => context.componentsInitialized;
    
    /// <summary>
    /// Flag to know if this Pio is managed by a PlayerManager in the scene.
    /// </summary>
    public bool IsPlayerManager => PlayerManager.Instance != null;
    
    /// <summary>
    /// Flag to know if this Pio is managed by a GameManager in the scene.
    /// </summary>
    public bool IsGameManager => GameManager.Instance != null;
    
    /// <summary>
    /// Int representing the index of the player thinking visually in split screen from top left to bottom right (1-4)
    /// in left-to-right, top-to-bottom reading style.
    /// </summary>
    public int VisualIndex
    {
        get
        {
            if (context.playerInput == null) return -1;
            
            if (context.playerInput.splitScreenIndex == -1) return -1;
            
            return context.playerInput.splitScreenIndex + 1;
        }
    }
    
    /// <summary>
    /// The current PlayerSettingsSo assigned to this Pio.
    /// </summary>
    public PlayerSettingsSo CurrentPlayerSettings => context.currentPlayerSettings;
    
    /// <summary>
    /// The event for PioComponents and anything else to subscribe to for when the PlayerSettingsSo values change.
    /// Should be called accordingly on changes.
    /// </summary>
    public event Action<PlayerSettingsSo> OnAfterPlayerSettingsChanged;
    
    [Header("Pio Context")]
    
    [SerializeField] private PlayerInputObjectContext context;
    
    #endregion
    
    #region Public Methods
        
    /// <summary>
    /// Method to be called to act as a manual toggle request to switch between ConfigurationTypes from
    /// the player's will. (Think the player pressing a pause/menu button to get in or out of a menu, or a UI
    /// button to do the same).
    /// </summary>
    public void TogglePlayerSettingsConfigurationType()
    {
        // Cannot toggle if BaseStateMachine not started
        if (!Started) { return; }
        
        // If PlayerSettingsSo does not allow manual switching, cannot toggle
        if (!CurrentPlayerSettings.AllowManualSwitching) { return; }
        
        // cannot toggle during certain app or game states
        bool isValidAppState = ApplicationManager.Instance == null || 
                               ApplicationManager.Instance.CurrentState.State == ApplicationManager.EApplicationState.Running ||
                               ApplicationManager.Instance.CurrentState.State == ApplicationManager.EApplicationState.Paused;
        
        bool isValidGameState = GameManager.Instance == null ||
                            GameManager.Instance.CurrentState.State == GameManager.EGameState.Playing ||
                            GameManager.Instance.CurrentState.State == GameManager.EGameState.Paused;
        
        
        if (!isValidAppState || !isValidGameState)
        {
            Debug.LogWarning($"{name}: Blocked player state alternation.");
            return;
        }
        
        // determine target configuration type, assigned as current first
        PlayerSettingsSo.EPlayerConfigurationType targetConfigurationType = 
            CurrentPlayerSettings.CurrentConfigurationType;
        
        // determining where to go based on where we are
        switch (CurrentPlayerSettings.CurrentConfigurationType)
        {
            // default => alternate
            case PlayerSettingsSo.EPlayerConfigurationType.Default:
                targetConfigurationType = 
                    PlayerSettingsSo.EPlayerConfigurationType.Alternate;
                break;
            // alternate => default
            case PlayerSettingsSo.EPlayerConfigurationType.Alternate:
                targetConfigurationType = 
                    PlayerSettingsSo.EPlayerConfigurationType.Default;
                break;
            // anything else, cannot toggle
            default:
                
                Debug.LogWarning($"{name}: Cannot alternate player state from " +
                                 $"{CurrentPlayerSettings.CurrentConfigurationType}");
                return;
            
        }
        
        // if managed by PlayerManager, notify of desired change so it can manage app/game/player accordingly
        // Pio responds accordingly
        if (IsPlayerManager && PlayerManager.Instance.Started)
        {
            context.requestedManagedConfigurationType = targetConfigurationType;
            
            PlayerManager.Instance.OnPlayerChangePlayerSettingsConfigurationType(this, targetConfigurationType);
        }
        // swapping play and pause states in game manager based on target configuration type,
        // since no player manager to manage it for us
        // Pio responds accordingly
        else if (IsGameManager && GameManager.Instance.Started)
        {
            switch(targetConfigurationType)
            {
                case PlayerSettingsSo.EPlayerConfigurationType.Default:
                    GameManager.Instance.Play();
                    break;
                case PlayerSettingsSo.EPlayerConfigurationType.Alternate:
                    GameManager.Instance.Pause();
                    break;  
            }
        }
        else
        {
            // manually calling state change in context
            context.ChangePlayerSettingsConfigurationType(targetConfigurationType);
        }
    }
    

    /// <summary>
    /// Public message to be received by the player's clone of InputActions in the PlayerInput component.
    /// Should be named whatever the action is called in the InputActions asset.
    /// This action is intended to toggle between ConfigurationTypes on the current PlayerSettingsSo.
    /// (Think like a pause/menu button)
    /// </summary>
    public void OnBack(InputValue value)
    {
        if (value.isPressed)
        {
            TogglePlayerSettingsConfigurationType();
        }
    }
    
    /// <summary>
    /// Call whenever to attempt and refresh the current PlayerSettingsSo values on the Pio and its components from
    /// PlayerPrefs settings.
    /// </summary>
    public void UpdateCurrentPlayerSettings()
    {
        if (CurrentPlayerSettings != null)
        {
            CurrentPlayerSettings.UpdatePlayerSettings();
            
            OnAfterPlayerSettingsChanged?.Invoke(CurrentPlayerSettings);
        }
    }
    
    #endregion

    #region Base Methods

    /// <summary>
    /// Defined initialization for the Pio.
    /// </summary>
    protected override void Initialize()
    {
        States = context.InitializedContextStates(this);
        
        //subscribe to application manager event if exists for cleanup on scene exit, etc.
        if (ApplicationManager.Instance != null)
        {
            ApplicationManager.Instance.OnBeforeStateChange += OnBeforeApplicationStateChange;
        }
        
        // subscribe to player manager event if exists to manage settings / state changes from there.
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAfterPlayerManagerSettingsChange += context.OnAfterPlayerManagerSettingsChange;
        }
    }

    /// <summary>
    /// Extended Update logic for the Pio. While the BaseStateMachine uses primarily internal calls to change state,
    /// the Pio uses an added target state management system. This means that in Update, the Pio checks if it is indeed
    /// in the correct target state based on the current PlayerSettingsSo assigned to it, and if not,
    /// changes to the correct target state. Allows for things like player choice and game architecture ability in
    /// externally changing Pio states/functionality.
    /// </summary>
    protected override void Update()
    {
        // still uses base Update logic
        base.Update();
        
        // extended logic cannot happen if not initialized
        if (!Initialized) { return; }

        EPlayerInputObjectState targetState = CurrentState.State;
        
        // could be null settings while they change, or have none
        // (ex: previously connected players 2,3, or 4 in a menu with only player 1 active).
        // Don't like the Update check tho:/
        if (context.currentPlayerSettings == null)
        {
            if (CurrentState.State != EPlayerInputObjectState.Off)
            {
                targetState = EPlayerInputObjectState.Off;
            }
        }
        // if not null then target is defined by the current configuration of the settings
        else
        {
            targetState = context.currentPlayerSettings.CurrentConfiguration.State;
        }
        
        if (CurrentState.State != targetState)
        {
            SelfBeforeChangeState(targetState);
            
            ChangeState(targetState);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles logic related to cleaning up on destruction of the Pio.
    /// </summary>
    private void OnDestroy()
    {
        //unsubscribe from application manager event if exists
        if (ApplicationManager.Instance != null)
        {
            ApplicationManager.Instance.OnBeforeStateChange -= OnBeforeApplicationStateChange;
        }

        if (IsPlayerManager)
        {
            PlayerManager.Instance.OnAfterPlayerManagerSettingsChange -= context.OnAfterPlayerManagerSettingsChange;
        }
        
        if (IsGameManager)
        {
            GameManager.Instance.OnAfterStateChange -= OnAfterGameStateChange;
        }
        
        // reset settings state when quitting
        if (CurrentPlayerSettings != null)
        {
            context.ClearPlayerSettings();
        }
        
        // removing account association after sessions for all players greater than player 1
        try
        {
            // todo: can increase this number if want to remember 2+ players instead
            if (VisualIndex > 1 
                && !string.IsNullOrEmpty(PlayerPrefsManager.TryGetAccountFromPlayerNumber(VisualIndex)))
            {
                PlayerPrefsManager.ClearPlayerAccountFromNumber(VisualIndex);
            }
        }
        catch (NullReferenceException) // possible something doesn't exist, which is alright
        {
        }
    }
    
    private void SelfBeforeChangeState(EPlayerInputObjectState toState)
    {
        // updating possible GameManager event subscription since not a persistent singleton
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAfterStateChange -= OnAfterGameStateChange;
            GameManager.Instance.OnAfterStateChange += OnAfterGameStateChange;
        }
    }
    
    private void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        // if going to loading scene with PlayerManager, reset settings to avoid carry over,
        // if there is no PlayerManager, no worry because Pio will be destroyed and reference lost
        if (PlayerManager.Instance != null &&
            toState == ApplicationManager.EApplicationState.LoadingScene)
        {
            // if going to loading scene, reset settings state
            if (CurrentPlayerSettings != null)
            {
                context.ClearPlayerSettings();
            }
        }
    }
    
    private void OnAfterGameStateChange(GameManager.EGameState toState)
    {
        if (!Initialized || CurrentPlayerSettings == null)
        {
            return;
        }
        
        bool isValidPlayerManager = IsPlayerManager && PlayerManager.Instance.CurrentPlayerManagerSettings != null;
        
        switch (toState)
        {
            default:
            case GameManager.EGameState.Initialize:
                
                // regardless of PlayerManager or not, shouldn't have control in this game state
                context.ChangePlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Off);
                
                break;
                
            case GameManager.EGameState.Playing:
                
                // if not managed by PlayerManager but GameManager present, match to it
                if (!isValidPlayerManager)
                {
                    context.ChangePlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Default);
                }
                
                break;
            case GameManager.EGameState.Paused:
                
                // same as playing
                if (!isValidPlayerManager)
                {
                    context.ChangePlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Alternate);
                }
                
                break;
            
            case GameManager.EGameState.GameOver:
                
                // regardless of PlayerManager or not, should not have control over anything but menu in this game state
                // (Alternate should be set up for a menu in PlayerSettingsSo)
                context.ChangePlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Alternate);
                
                break;
            
        }
    }

    #endregion
}
