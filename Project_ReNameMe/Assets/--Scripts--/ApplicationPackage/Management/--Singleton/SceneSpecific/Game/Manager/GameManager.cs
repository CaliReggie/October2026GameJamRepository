using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class GameManager : BaseStateManagerApplicationListener<GameManager, GameManager.EGameState>
{
    /// <summary>
    /// The different states of the Game Manager.
    /// </summary>
    public enum EGameState
    {
        [Tooltip("Game is initializing / resetting.")]
        Initialize,
        [Tooltip("Game is being played")]
        Playing,
        [Tooltip("Game is paused.")]
        Paused,
        [Tooltip("Game is over.")]
        GameOver
    }
    
    #region State Class
    
    public abstract class GameManagerState : BaseState<EGameState>
    {
        protected GameManagerContext Context { get; }
        
        protected GameManagerState(GameManagerContext context,
            EGameState key,
            EGameState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
    }
    
    #endregion

    #region Context Class

    [Serializable]
    public class GameManagerContext : BaseStateMachineContext
    {
        #region Context Declarations

        [Header("Inscribed References")]
        
        [Tooltip("The root play page transform.")]
        public Transform playPage;

        [Tooltip("The root pause page transform.")]
        public Transform pausePage;
        
        [Tooltip("The root settings page transform.")]
        public Transform settingsPage;
        
        [Tooltip("The root game won page transform.")]
        public Transform gameWonPage;
        
        [Tooltip("The root game lost page transform.")]
        public Transform gameLostPage;
        
        [Header("Inscribed Settings")]
        
        [Tooltip("Whether to hide the play page when Paused.")]
        public bool hidePlayWhenPaused;
        
        [Tooltip("Whether to hide the play page when GameOver.")]
        public bool hidePlayWhenOver;
        
        [Header("Dynamic References - Don't Modify In Inspector")]
        
        [Tooltip("The GameManager that this context belongs to.")]
        public GameManager gameManager;
        
        [Tooltip("If in a scene without a PlayerManager, this will be set on Initialize and can be used/referenced" +
                 "by scripts that need to reference a PlayerInputObject in the scene. " +
                 "If a PlayerManager exists, this will be ignored and should not be used/referenced.")]
        public PlayerInputObject singlePlayerPioReference;
        
        [Header("Dynamic Settings - Don't Modify In Inspector")]
        
        [Tooltip("If true, when in GameOver state, the game is considered won, and vice versa for false/lost.")]
        public bool gameWon;
        
        #endregion
        
        #region Base Context Methods

        public override Dictionary<EGameState, BaseState<EGameState>> InitializedContextStates(BaseStateMachine<EGameState> targetStateMachine)
        {
            
            Dictionary<EGameState, EGameState[]> stateTransitions = new()
            {
                { EGameState.Initialize, new [] { EGameState.GameOver } }, //Cannot transition from Initialize to GameOver
                { EGameState.Playing, new EGameState[]{ } }, //No invalid transitions for Playing state
                { EGameState.Paused, new EGameState[]{ } }, //No invalid transitions for Paused state
                { EGameState.GameOver, new [] { EGameState.Playing, EGameState.Paused} } //Cannot transition from GameOver to Playing or Paused
            };

            Dictionary<EGameState, BaseState<EGameState>> stateConstructions = new();

            foreach (var state in stateTransitions)
            {
                switch (state.Key)
                {
                    case EGameState.Initialize:
                        stateConstructions.Add(state.Key, new GameManagerInitialize(this, state.Key, state.Value));
                        break;
                    case EGameState.Playing:
                        stateConstructions.Add(state.Key, new GameManagerPlaying(this, state.Key, state.Value));
                        break;
                    case EGameState.Paused:
                        stateConstructions.Add(state.Key, new GameManagerPaused(this, state.Key, state.Value));
                        break;
                    case EGameState.GameOver:
                        stateConstructions.Add(state.Key, new GameManagerOver(this, state.Key, state.Value));
                        break;
                }
            }
            
            gameManager = (GameManager) targetStateMachine;
            
            if (playPage == null ||
                pausePage == null ||
                pausePage == null ||
                gameWonPage == null ||
                gameLostPage == null)
            {
                Debug.LogError($"{GetType().Name}: Error Checking Inscribed References. Destroying self.");
                
                Destroy(gameManager.gameObject);
                
                return null;
            }

            return stateConstructions;
        }

        public override void ContextCallChangeState(EGameState newState)
        {
            gameManager.ChangeState(newState);
        }
        
        #endregion

        #region UiPages Context Methods

        /// <summary>
        /// True if player(s) are looking through main view and not theirs, false otherwise.
        /// </summary>
        /// <param name="playerManagerSettings"></param>
        /// <returns></returns>
        public bool CheckIfManagerPagesNeeded(PlayerManagerSettingsSo playerManagerSettings)
        {
            if (playerManagerSettings == null)
            {
                Debug.LogError($"{GetType().Name}: PlayerManagerSettings is null. Cannot determine if pages need to be shown.");
                
                return false;
            }
            
            // if a single player needs to see from main camera, disable cover
            for (int i = 0; i < playerManagerSettings.PlayersSettings.Count; i++)
            {
                PlayerSettingsSo playerSettings = playerManagerSettings.PlayersSettings[i];

                if (playerSettings.NeedToSeeFromSceneCamera)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// True if player needs to see through main view and not theirs, false otherwise.
        /// </summary>
        /// <param name="playerSettings"></param>
        /// <returns></returns>
        public bool CheckIfManagerPagesNeeded(PlayerSettingsSo playerSettings)
        {
            if (playerSettings == null)
            {
                return false;
            }
            
            return playerSettings.NeedToSeeFromSceneCamera;
        }
        
        public void TogglePlayPage(bool isActive)
        {
            playPage.gameObject.SetActive(isActive);
        }
        
        public void TogglePausePage(bool isActive)
        {
            pausePage.gameObject.SetActive(isActive);
        }
        
        public void ToggleSettingsPage(bool isActive)
        {
            settingsPage.gameObject.SetActive(isActive);
        }
        
        public void ToggleOverPage(bool won, bool isActive)
        {
            if (won)
            {
                gameWonPage.gameObject.SetActive(isActive);
                gameLostPage.gameObject.SetActive(false);
            }
            else
            {
                gameLostPage.gameObject.SetActive(isActive);
                gameWonPage.gameObject.SetActive(false);
            }
        }

        #endregion
    }

    #endregion
    
    #region Variables, Properties, Fields, Events
    
    public bool GameWon => context.gameWon;
    
    [Header("Editor Testing")]
    
    [SerializeField] private EGameState testTargetState;

    [SerializeField]
    private bool changeToTestTargetState;
    
    [Header("Game Manager Context")]
    
    [SerializeField] private GameManagerContext context;
    
    #endregion
    
    #region Public Methods
    
    public void ResetGame()
    {
        context.ContextCallChangeState(EGameState.Initialize);
    }
    
    public void Play()
    {
        context.ContextCallChangeState(EGameState.Playing);
    }
    
    public void Pause()
    {
        context.ContextCallChangeState(EGameState.Paused);
    }
    
    public void GameOver(bool gameWon)
    {
        context.gameWon = gameWon;
        
        context.ContextCallChangeState(EGameState.GameOver);
    }
    
    #endregion
    
    #region Base Methods
    
    protected override void SetInstanceType()
    {
        InstanceType = EInstanceType.Singleton;
    }
    
    protected override void Initialize()
    {
        States = context.InitializedContextStates(this);
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(AudioManager.EMusicType.InGame);
        }
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnBeforeStateChange += OnBeforePlayerManagerStateChange;
            
            PlayerManager.Instance.OnAfterStateChange += OnAfterPlayerManagerStateChange;
            
            if (DebugMode)
            {
                Debug.Log($"{GetType().Name}: Subscribed to PlayerManager events.");
            }
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: No PlayerManager instance found in scene. " +
                             $"Functionality will be limited without.");
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnBeforeStateChange -= OnBeforePlayerManagerStateChange;
            
            PlayerManager.Instance.OnAfterStateChange -= OnAfterPlayerManagerStateChange;
            
            if (DebugMode)
            {
                Debug.Log($"{GetType().Name}: Unsubscribed from PlayerManager events.");
            }
        }
    }

    protected override void OnActiveSceneSettingsChanged(SceneSettingsSo newActiveSceneSettings)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnActiveSceneSettingsChanged: {newActiveSceneSettings}");
        }
    }

    protected override void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnBeforeApplicationStateChange: {toState}");
        }
    }
    
    protected override void OnAfterApplicationStateChange(ApplicationManager.EApplicationState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnAfterApplicationStateChange: {toState}");
        }

        switch (toState)
        {
            case ApplicationManager.EApplicationState.Running:
                
                // if going to running and PlayerManager with sufficient players exists, go to Playing state
                if (PlayerManager.Instance != null &&
                    PlayerManager.Instance.CurrentState.State == PlayerManager.EPlayerManagementState.SufficientPlayers)
                {
                    context.ContextCallChangeState(EGameState.Playing);
                }
                
                break;
                
            case ApplicationManager.EApplicationState.Paused:
                
                // if going to paused, check if any player needs to see the scene pause UI and show it if so
                if (PlayerManager.Instance != null &&
                    PlayerManager.Instance.CurrentState.State == PlayerManager.EPlayerManagementState.SufficientPlayers)
                {
                    if (CurrentState.State == EGameState.Playing)
                    {
                        context.ContextCallChangeState(EGameState.Paused);
                    }
                }
                
                break;
        }
    }
    
    protected override void ChangeState(EGameState newState)
    {
        base.ChangeState(newState);
        
        testTargetState = CurrentState.State;
    }

    #endregion
    
    #region Private Methods
    
    private void OnValidate()
    {
        if (changeToTestTargetState)
        {
            changeToTestTargetState = false;
            
            ChangeState(testTargetState);
        }
    }
    
    private void OnBeforePlayerManagerStateChange(PlayerManager.EPlayerManagementState fromState)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnBeforePlayerManagerStateChange: {fromState}");
        }
    }
    
    private void OnAfterPlayerManagerStateChange(PlayerManager.EPlayerManagementState toState)
    {
        if (DebugMode)
        {
            Debug.Log($"[{GetType().Name}] OnAfterPlayerManagerStateChange: {toState}");
        }
        
        switch (toState)
        {
            case PlayerManager.EPlayerManagementState.SufficientPlayers:
                
                // if just got sufficient players and application manager exists
                if (ApplicationManager.Instance != null &&
                    ApplicationManager.Instance.Started)
                {
                    // conditionally match running - playing
                    if (ApplicationManager.Instance.CurrentState.State ==
                        ApplicationManager.EApplicationState.Running)
                    {
                        context.ContextCallChangeState(EGameState.Playing);
                    }
                    // conditionally math paused - paused
                    else if (ApplicationManager.Instance.CurrentState.State ==
                             ApplicationManager.EApplicationState.Paused)
                    {
                        context.ContextCallChangeState(EGameState.Paused);
                    }
                }
                
                break;
            case PlayerManager.EPlayerManagementState.AddingPlayers:
            case PlayerManager.EPlayerManagementState.RemovingPlayers:
                
                // if not at sufficient players anymore, reset with initialize and wait for sufficient players again
                context.ContextCallChangeState(EGameState.Initialize);
                
                break;
        }
    }
    
    #endregion
}
