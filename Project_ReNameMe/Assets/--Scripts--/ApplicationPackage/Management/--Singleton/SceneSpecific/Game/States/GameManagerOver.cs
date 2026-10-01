using UnityEngine;

public class GameManagerOver : GameManager.GameManagerState
{
    public GameManagerOver(
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
        bool shouldShowOverPage = true;
        
        // ensure application time
        if (ApplicationManager.Instance != null &&
            ApplicationManager.Instance.Started)
        {
            if (Context.gameWon)
            {
                // todo: ex: if this game levels are just pass fail,if the player won, set the best score to 1 (pass)
                PlayerPrefsManager.TrySetSceneBestScore(ApplicationManager.Instance.ActiveSceneSettings.ChronologicalId,
                    1);
            }
            
            if (ApplicationManager.Instance.CurrentState.State != ApplicationManager.EApplicationState.Paused)
            {
                ApplicationManager.Instance.RequestChangeState(ApplicationManager.EApplicationState.Paused);
            }
            
            shouldShowOverPage = Context.CheckIfManagerPagesNeeded(ApplicationManager.Instance.ActiveSceneSettings.PlayerManagerSettings);
        }
        else
        {
            if (Context.singlePlayerPioReference != null)
            {
                shouldShowOverPage = Context.CheckIfManagerPagesNeeded(Context.singlePlayerPioReference.CurrentPlayerSettings);
            }
            
            Time.timeScale = 0f;
        }
        
        if (Context.hidePlayWhenOver)
        {
            Context.TogglePlayPage(false);
        }
        
        // show over page?
        Context.ToggleOverPage(Context.gameWon, shouldShowOverPage);
    }

    public override void UpdateState()
    {
        
    }

    public override void ExitState()
    {
        // hide over page
        Context.ToggleOverPage(Context.gameWon, false);
        
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
