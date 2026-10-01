using UnityEngine;


public class PlayerManagerAddingPlayers : PlayerManager.InputManagerState
{
    public PlayerManagerAddingPlayers(
        PlayerManager.PlayerManagerContext context,
        PlayerManager.EPlayerManagementState key,
        PlayerManager.EPlayerManagementState[] invalidTransitions)
        : base(
            context,
            key,
            invalidTransitions)
    {
    }

    private bool hasWaitedForOneUpdateFrame = false; //used to wait for update for certain logic

    private bool hasWaitedAfterPlayerNumValid = false; //used to wait for certain logic in update

    public override void EnterState()
    {
        hasWaitedForOneUpdateFrame = false;
        
        hasWaitedAfterPlayerNumValid = false;

        // setting target state to off while managing player count
        Context.ChangeTargetPlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Off);
        
        // Pause the application time
        if (ApplicationManager.Instance != null )
        {
            if (ApplicationManager.Instance.Started &&
                ApplicationManager.Instance.CurrentState.State == ApplicationManager.EApplicationState.Running)
            {
                ApplicationManager.Instance.RequestChangeState(ApplicationManager.EApplicationState.Paused);
            }
            
        }
        else
        {
            Time.timeScale = 0f;
        }
        
        // cursor unlocked and visible in case players can't add and need to cancel/quit
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // toggling adding players page
        Context.ToggleAddingPlayersPage(true);
    }

    public override void UpdateState()
    {
        if (!hasWaitedForOneUpdateFrame)
        {
            // waiting for update frame to allow initialization before accepting players.
            Context.inputManagerComponent.EnableJoining(); 
            
            hasWaitedForOneUpdateFrame = true;
        }
        
        // If no longer need to add players
        if (!Context.NeedMorePlayers) 
        {
            if (hasWaitedAfterPlayerNumValid)
            {
                // Disable player joining input
                Context.inputManagerComponent.DisableJoining(); 
                
                // back to SufficientPlayers state
                Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.SufficientPlayers); 
                
                return;
            }
            else
            {
                hasWaitedAfterPlayerNumValid = true;
            }
        }
        else
        {
            // calling it repeatedly:/ so it updates based on current player count and settings...
            Context.ToggleAddingPlayersPage(true);
        }
    }

    public override void ExitState()
    {
        // toggling adding players page off
        Context.ToggleAddingPlayersPage(false);
        
        // resume application time
        if (ApplicationManager.Instance != null)
        {
            if (ApplicationManager.Instance.Started &&
                ApplicationManager.Instance.CurrentState.State == ApplicationManager.EApplicationState.Paused)
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
