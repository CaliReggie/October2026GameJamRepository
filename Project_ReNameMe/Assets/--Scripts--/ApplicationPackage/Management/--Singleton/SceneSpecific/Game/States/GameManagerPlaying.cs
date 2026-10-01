using UnityEngine;

public class GameManagerPlaying : GameManager.GameManagerState
{
    public GameManagerPlaying(
        GameManager.GameManagerContext context,
        GameManager.EGameState key,
        GameManager.EGameState[] invalidTransitions)
        : base(
            context,
            key,
            invalidTransitions)
    {
    }

    public override void EnterState()
    {
        bool shouldShowPlayPage = true;
        
        // ensure application time
        if (ApplicationManager.Instance != null &&
            ApplicationManager.Instance.Started)
        {
            if (ApplicationManager.Instance.CurrentState.State != ApplicationManager.EApplicationState.Running)
            {
                ApplicationManager.Instance.RequestChangeState(ApplicationManager.EApplicationState.Running);
            }
            
            shouldShowPlayPage = Context.CheckIfManagerPagesNeeded(ApplicationManager.Instance.ActiveSceneSettings.PlayerManagerSettings);
        }
        else
        {
            if (Context.singlePlayerPioReference != null)
            {
                shouldShowPlayPage = Context.CheckIfManagerPagesNeeded(Context.singlePlayerPioReference.CurrentPlayerSettings);
            }
            
            Time.timeScale = 1f;
        }
        
        
        // show play page?
        Context.TogglePlayPage(shouldShowPlayPage);
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
    }
}
