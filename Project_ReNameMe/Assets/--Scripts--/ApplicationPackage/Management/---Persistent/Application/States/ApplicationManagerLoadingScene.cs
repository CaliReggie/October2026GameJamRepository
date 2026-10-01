using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationManagerLoadingScene : ApplicationManager.ApplicationManagerState
{
    public ApplicationManagerLoadingScene(
        ApplicationManager.ApplicationManagerContext context,
        ApplicationManager.EApplicationState key,
        ApplicationManager.EApplicationState[] invalidTransitions)
        : base(
            context, 
            key, 
            invalidTransitions)
    {
    }

    private bool _isFirstEverEnter = true; // used to track first enter of state ever
    
    private bool _didFirstValidUpdateOccur; // Used to track if the first valid update has occurred
                                            // (i.e., target scene is active)
    
    public override void EnterState()
    {
        _didFirstValidUpdateOccur = false; // Reset the flag for the first valid update
        
        // Pause the game while loading
        Time.timeScale = 0f; 
        
        if (Context.activeSceneSettings != null) //ensure proper transition settings
        {
            // if location transition
            if (Context.activeSceneSettings.transitionInStyle ==
                ApplicationManager.ApplicationManagerContext.ETransitionStyle.LocationTransition)
            {
                Context.transitionImage.transform.position =
                    Context.coveredLocation.position; // cover screen
                
                Context.transitionImage.color = Context.activeSceneSettings.CoveredColor; // covered color
            }
            // if color transition
            else
            {
                // cover screen
                Context.transitionImage.transform.position =
                    Context.coveredLocation.position; 

                // covered color
                Context.transitionImage.color = Context.activeSceneSettings.CoveredColor;
            }
        }
        
        // if first enter ever, don't load scene if already active
        if (_isFirstEverEnter)
        {
            _isFirstEverEnter = false;
            
            if (SceneSettingsSo.IsActiveScene(Context.targetSceneSettings))
            {
                return;
            }
        }
        
        //load the target scene
        SceneManager.LoadScene(Context.targetSceneSettings.TryGetScenePathAsName());
    }
    
    public override void UpdateState()
    {
        // If target scene is indeed active
        if (!_didFirstValidUpdateOccur && SceneSettingsSo.IsActiveScene(Context.targetSceneSettings)) 
        {
            //Ensure the active scene SO is set correctly
            Context.SetActiveSceneSO(Context.targetSceneSettings);
            
            // Cleaning target settings
            Context.targetSceneSettings = null;
            
            _didFirstValidUpdateOccur = true; // Set the flag to indicate the first valid update has occurred

            Context.StartRunningTransitionInScene();
        }
    }
    
    public override void ExitState()
    {
        Time.timeScale = 1f; // Unpause the game
    }

}
