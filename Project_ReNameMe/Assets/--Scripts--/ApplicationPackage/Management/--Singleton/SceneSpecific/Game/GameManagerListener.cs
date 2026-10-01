using UnityEngine;

public abstract class GameManagerListener : MonoBehaviour
{
    #region Variables, Properties, Events, Fields

    /// <summary>
    /// True if this listener has successfully (and still is) initialized.
    /// Derived classes should set this to true in their Initialize method if initialization is successful,
    /// and should set it to false if they are destroyed or otherwise become unusable.
    /// </summary>
     public bool Initialized { get; protected set; }
     
     /// <summary>
     /// Gets the current GameManager state. If the GameManager instance is null or has not started, it returns Initialize as the default state.
     /// </summary>
     protected GameManager.EGameState CurrentGameManagerState => GameManager.Instance != null ? GameManager.Instance.Started ?
             GameManager.Instance.CurrentState.State :
             GameManager.EGameState.Initialize : GameManager.EGameState.Initialize;
     
     /// <summary>
     /// Gets the previous GameManager state. If the GameManager instance is null or has not started, it returns Initialize as the default state.
     /// </summary>
     protected GameManager.EGameState PreviousGameManagerState => 
         GameManager.Instance != null ? GameManager.Instance.Started ?
             GameManager.Instance.PreviousState.State :
             GameManager.EGameState.Initialize : GameManager.EGameState.Initialize;

    #endregion

    #region Base Methods

    /// <summary>
    /// Base Awake calls Initialize, where derived classes do init logic and MUST set initialized to true if successful.
    /// </summary>
    protected virtual void Awake()
    {
    if (!Initialized)
    {
        Initialize();
    }
    }

    /// <summary>
    /// Base Start handles GameManager state event subscription and initial action based on GameManager state.
    /// </summary>
    protected virtual void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAfterStateChange += OnAfterGameStateChanged;
            
            if (GameManager.Instance.Started)
            {
                OnBeforeGameStateChanged(GameManager.Instance.CurrentState.State);
                OnAfterGameStateChanged(GameManager.Instance.CurrentState.State);
            }
            else
            {
                OnBeforeGameStateChanged(GameManager.EGameState.Initialize);
                OnAfterGameStateChanged(GameManager.EGameState.Initialize);
            }
        }
    }
     
    /// <summary>
    /// Base OnDestroy handles GameManager state event unsubscription.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAfterStateChange -= OnAfterGameStateChanged;
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// To be implemented by derived classes for initialization logic. Must set initialized to true if successful.
    /// </summary>
    protected abstract void Initialize();

    /// <summary>
    /// To be implemented by derived classes for logic to execute before the GameManager state has officially changed.
    /// The toState parameter indicates the new state that the GameManager is transitioning to.
    /// </summary>
    protected abstract void OnBeforeGameStateChanged(GameManager.EGameState toState);

    /// <summary>
    /// To be implemented by derived classes for logic to execute after the GameManager state has officially changed.
    /// The toState parameter indicates the new state that the GameManager has transitioned to.
    /// </summary>
    protected abstract void OnAfterGameStateChanged(GameManager.EGameState toState);

    #endregion
}
