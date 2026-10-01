using UnityEngine;

public class ApplicationManagerExitingScene : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerExitingScene(
        ApplicationManager.ApplicationManagerContext context,
        ApplicationManager.EApplicationState key,
        ApplicationManager.EApplicationState[] invalidTransitions)
        : base(
            context,
            key,
            invalidTransitions)
    {
    }
    
    public override void EnterState()
    {
        // Pause the game while exiting
        Time.timeScale = 0f; 
        
        
        if (Context.activeSceneSettings != null) //ensure proper transition settings
        {
            // if location transition
            if (Context.activeSceneSettings.transitionOutStyle ==
                ApplicationManager.ApplicationManagerContext.ETransitionStyle.LocationTransition)
            {
                Context.transitionImage.transform.position =
                    Context.uncoveredLocation.position; // uncover screen
                
                Context.transitionImage.color = Context.activeSceneSettings.CoveredColor; // covered color
            }
            // if color transition
            else
            {
                // cover screen
                Context.transitionImage.transform.position =
                    Context.coveredLocation.position; 

                // uncovered color
                Context.transitionImage.color = Context.activeSceneSettings.UncoveredColor;
            }
        }

        Context.StartExitTransitionOutScene();
    }
    
    public override void UpdateState()
    {
        
    }
    public override void ExitState()
    {
        Time.timeScale = 1f; // Unpause the game
    }
}
