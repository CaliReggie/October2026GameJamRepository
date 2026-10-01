using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class MainMenuManager : BaseStateManagerApplicationListener<MainMenuManager, MainMenuManager.EMainMenuUIState>
{
    public enum EMainMenuUIState
    {
        Default
    }
    
    #region Variables, Properties, Fields, Events
    
    [Header("Main Menu Manager Context")]
    
    [SerializeField] private MainMenuUIManagerContext context;
    
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
    }
    
    #endregion

    #region Context Class

    [Serializable]
    public class MainMenuUIManagerContext : BaseStateMachineContext
    {
        #region ContextDeclarations
        
        [Header("Inscribed References")]
        
        [Tooltip("Objects to remain disabled until a player is recognized as active / joined.")]
        public List<GameObject> delayedObjects;
        
        [Header("Dynamic References - Don't Modify In Inspector")]
        
        public MainMenuManager mainMenuManager;

        #endregion
        
        #region Base Context Methods
        
        public override Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>> InitializedContextStates(BaseStateMachine<EMainMenuUIState> targetStateMachine)
        {
            
            Dictionary<EMainMenuUIState, EMainMenuUIState[]> stateTransitions = new()
            {
                { EMainMenuUIState.Default, new EMainMenuUIState[]{ } } //No invalid transitions for Default state
            };
            
            Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>> stateConstructions = new();
            
            foreach (var state in stateTransitions)
            {
                switch (state.Key)
                {
                    case EMainMenuUIState.Default:
                        stateConstructions.Add(state.Key, new MainMenuUIManagerDefault(this, state.Key, state.Value));
                        break;
                }
            }
            
            mainMenuManager = (MainMenuManager)targetStateMachine;
            
            // if delayed ui elements is null or contains null error and destroy self
            if (delayedObjects == null || delayedObjects.Contains(null))
            {
                Debug.LogError($"[{GetType().Name}] Delayed UI Elements list is null or contains null references." +
                               $" Destroying MainMenuManager.");
                Destroy(mainMenuManager.gameObject);
                return new Dictionary<EMainMenuUIState, BaseState<EMainMenuUIState>>();
            }

            return stateConstructions;
        }
        
        public override void ContextCallChangeState(EMainMenuUIState newState)
        {
            mainMenuManager.ChangeState(newState);
        }
        
        #endregion

        #region Delayed Objects Context Methods

        public void ToggleDelayedObjects(bool isActive)
        {
            foreach (var uiElement in delayedObjects)
            {
                if (uiElement != null)
                {
                    uiElement.SetActive(isActive);
                }
            }
        }

        #endregion
    }

    #endregion

    #region State Class

    public abstract class MainMenuUIManagerState : BaseState<EMainMenuUIState>
    {
        protected MainMenuUIManagerContext Context { get; }
        
        protected MainMenuUIManagerState(MainMenuUIManagerContext context,
            EMainMenuUIState key,
            EMainMenuUIState[] invalidTransitions) 
            : base(key, invalidTransitions)
        {
            Context = context;
        }
    }

    #endregion
}
