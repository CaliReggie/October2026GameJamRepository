using UnityEngine;

public class GameManagerPaused : GameManager.GameManagerState
{
    public GameManagerPaused(
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
        bool shouldShowPausePage = true;
        
        // ensure application time
        if (ApplicationManager.Instance != null &&
            ApplicationManager.Instance.Started)
        {
            if (ApplicationManager.Instance.CurrentState.State != ApplicationManager.EApplicationState.Paused)
            {
                ApplicationManager.Instance.RequestChangeState(ApplicationManager.EApplicationState.Paused);
            }
            
            shouldShowPausePage = Context.CheckIfManagerPagesNeeded(ApplicationManager.Instance.ActiveSceneSettings.PlayerManagerSettings);
        }
        else
        {
            if (Context.singlePlayerPioReference != null)
            {
                shouldShowPausePage = Context.CheckIfManagerPagesNeeded(Context.singlePlayerPioReference.CurrentPlayerSettings);
            }
            
            Time.timeScale = 0f;
        }
        
        if (Context.hidePlayWhenPaused)
        {
            Context.TogglePlayPage(false);
        }
        
        // show pause page?
        Context.TogglePausePage(shouldShowPausePage);
        
        // turn off settings page
        Context.ToggleSettingsPage(false);
    }

    public override void UpdateState()
    {
        
    }

    public override void ExitState()
    {
        // hide pause and settings page
        Context.TogglePausePage(false);
        
        Context.ToggleSettingsPage(false);
        
        // resume application time
        if (ApplicationManager.Instance != null)
        {
            if (ApplicationManager.Instance.CurrentState.State != ApplicationManager.EApplicationState.Running)
            {
                ApplicationManager.Instance.RequestChangeState(ApplicationManager.EApplicationState.Running);
            }
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
