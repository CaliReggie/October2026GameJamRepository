using UnityEngine;

public class PlayerManagerSufficientPlayers : PlayerManager.InputManagerState
{
    public PlayerManagerSufficientPlayers(
        PlayerManager.PlayerManagerContext context,
        PlayerManager.EPlayerManagementState key,
        PlayerManager.EPlayerManagementState[] invalidTransitions)
        : base(
            context,
            key,
            invalidTransitions)
    {
    }

    public override void EnterState()
    {
        if (Context.CanHaveMorePlayers)
        {
            Context.inputManagerComponent.EnableJoining();
        }
        else
        {
            Context.inputManagerComponent.DisableJoining();
        }
        
        // determining target player settings state to set V
        
        // if setup application manager
        if (Context.playerManager.IsApplicationManager && ApplicationManager.Instance.Started)
        {
            // depends on application state
            switch (ApplicationManager.Instance.CurrentState.State)
            {
                case ApplicationManager.EApplicationState.Running:
                    Context.ChangeTargetPlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Default);
                    break;
                case ApplicationManager.EApplicationState.Paused:
                    Context.ChangeTargetPlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Alternate);
                    break;
                default:
                    Context.ChangeTargetPlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Off);
                    break;
            }
        }
        // if no app manager or not yet setup, use context defaults
        else
        {
            Context.ChangeTargetPlayerSettingsConfigurationType(PlayerSettingsSo.EPlayerConfigurationType.Default);
        }
        
        // cursor confined and invisible since managed by player cursor components
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible   = false;
    }

    public override void UpdateState()
    {
        // If target players is greater than current players
        if (Context.NeedMorePlayers) 
        { 
            // Transition to AddingPlayers state
            Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.AddingPlayers);
            
            return;
        }
        // checking in update loop :///, todo: could be better elsewhere if consistent?
        else if (Context.CanHaveMorePlayers)
        {
            Context.inputManagerComponent.EnableJoining();
        }
        else
        {
            Context.inputManagerComponent.DisableJoining();
        }
        
        // If target players is less than current players
        if (Context.NeedLessPlayers) 
        {
            // Transition to RemovingPlayers state
            Context.ContextCallChangeState(PlayerManager.EPlayerManagementState.RemovingPlayers);
            
            return;
        }
    }

    public override void ExitState()
    {

    }
}
