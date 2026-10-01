using System;
using UnityEngine;

/// <summary>
/// The PlayerUiPioComponent class extends the PioComponent base class to manage the user interface (UI)
/// of the Pio's player input object. It handles the activation and deactivation of various UI canvases
/// based on the Pio's state and settings.
/// </summary>
public class PlayerUiPioComponent : PioComponent
{
    /// <summary>
    /// The different styles of Ui groups to target when toggling on and off based on Pio state and settings.
    /// </summary>
    private enum EToggleTarget
    {
        PlayerPlaying,
        PlayerPaused,
        PlayerWon,
        PlayerLost,
        SceneUICanvas,
        All
    }
    
    [Header("Inscribed References")]
    
    [Tooltip("The canvas used for player object world space UI.")]
    [SerializeField] private Canvas playerObjectUiCanvas;
    
    [Tooltip("The canvas used for scene screen space UI.")]
    [SerializeField] private Canvas sceneUiCanvas;
    
    [Tooltip("The transform parent for playing page for player")]
    [SerializeField] private Transform playerObjUiPlayPage;

    [Tooltip("The transform parent for pause page for player")]
    [SerializeField] private Transform playerObjUiPausePage;
    
    [Tooltip("The transform parent for settings page for player")]
    [SerializeField] private Transform playerObjSettingsPage;
    
    [Tooltip("The transform parent for won page for player")]
    [SerializeField] private Transform playerObjUiWonPage;
    
    [Tooltip("The transform parent for lost page for player")]
    [SerializeField] private Transform playerObjUiLostPage;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("If true, the player object canvas will persist below the player object ui canvas when in PlayerUi state," +
             " otherwise it will only be active in PlayerUi state.")]
    [SerializeField] private bool persistentPlayerCanvas = true;
    
    /// <summary>
    /// Returns the Pio player object world space canvas.
    /// </summary>
    public Canvas PlayerObjectUiCanvas => playerObjectUiCanvas;
    
    /// <summary>
    /// Returns the Pio scene screen space canvas.
    /// </summary>
    public Canvas SceneUiCanvas => sceneUiCanvas;
    
    protected override void Initialize()
    {
        if (!CheckInscribedReferences())
        {
            return;
        }
        
        // ensure UI starts off
        ToggleUI(EToggleTarget.All, false);
        
        Initialized = true;
        
        return;
        
        // check inscribed references
        bool CheckInscribedReferences()
        {
            if (playerObjectUiCanvas == null ||
                sceneUiCanvas == null ||
                playerObjUiPlayPage == null ||
                playerObjUiPausePage == null ||
                playerObjSettingsPage == null ||
                playerObjUiLostPage == null ||
                playerObjUiWonPage == null)
            {
                Debug.LogError($"{GetType().Name}: Error checking inscribed references.");
                
                return false;
            }
            
            return true;
        }
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.Off:
                
                // if on, deactivate
                if (enabled)
                {
                    // toggle all off
                    ToggleUI(EToggleTarget.All, false);
                                    
                    enabled = false;
                }
                
                break;
        }
    }
    
    protected override void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.Player:
                
                ToggleUI(EToggleTarget.All, false);
                
                // for player, just toggle player object canvas
                ToggleUI(EToggleTarget.PlayerPlaying, true);
                
                break;
            
            case PlayerInputObject.EPlayerInputObjectState.PlayerUi:
                
                ToggleUI(EToggleTarget.All, false);
                
                // for player ui, logic varies based on camera type
                bool isUsingMainCamera = Pio.CurrentPlayerSettings.CameraType == PlayerCameraPioComponent.EPlayerCameraType.SceneCamera;
                
                // if using main camera, cursor will use scene ui, enable scene ui
                if (isUsingMainCamera)
                {
                    ToggleUI(EToggleTarget.SceneUICanvas, true);
                }
                else
                {
                    // showing player canvas persistently?
                    if (persistentPlayerCanvas)
                    {
                        ToggleUI(EToggleTarget.PlayerPlaying, true);
                    }
                    
                    // with game manager in over state, not paused but different ui
                    if (GameManager.Instance != null &&
                        GameManager.Instance.Started &&
                        GameManager.Instance.CurrentState.State == GameManager.EGameState.GameOver)
                    {
                        if (GameManager.Instance.GameWon)
                        {
                            ToggleUI(EToggleTarget.PlayerWon, true);
                        }
                        else
                        {
                            ToggleUI(EToggleTarget.PlayerLost, true);
                        }
                    }
                    // without game manager or being in over, is basic paused
                    else
                    {
                        ToggleUI(EToggleTarget.PlayerPaused, true);
                    }
                }
                
                break;
            
            case PlayerInputObject.EPlayerInputObjectState.PlayerSceneUi:
                
                ToggleUI(EToggleTarget.All, false);
                
                ToggleUI(EToggleTarget.PlayerPlaying, true);
                
                ToggleUI(EToggleTarget.SceneUICanvas, true);
                
                break;
                
            case PlayerInputObject.EPlayerInputObjectState.SceneUi:
                
                ToggleUI(EToggleTarget.All, false);
                
                // for scene ui, just enable scene ui
                ToggleUI(EToggleTarget.SceneUICanvas, true);
                
                break;
                
        }
        
        if (toState != PlayerInputObject.EPlayerInputObjectState.Off)
        {
            enabled = true;
        }
    }
    
    protected override void OnAfterPioSettingsChange(PlayerSettingsSo playerSettings)
    {
        try
        {
            // update persistent player canvas setting
            persistentPlayerCanvas = playerSettings.PersistentPlayerCanvas;
            
            // updating canvas / ui to catch pause > game over (both player ui states but different pages). Messy ? ://
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState.State == GameManager.EGameState.GameOver)
            {
                OnAfterPioStateChange(playerSettings.CurrentConfiguration.State);
            }
            
        }
        catch (Exception e)
        {
            Debug.LogError($"{GetType().Name}:{name}: Error applying PlayerSettingsSo changes: {e.Message}");
        }
        
    }
    
    /// <summary>
    /// True if player is looking through own view, false otherwise. 
    /// </summary>
    /// <returns></returns>
    private bool ShowPlayerPagesNeeded()
    {
        if (!Initialized) 
        {
            return false;
        }
        
        if (Pio.CurrentPlayerSettings == null)
        {
            if (debugMode)
            {
                Debug.LogError($"{GetType().Name}: PlayerSettings is null. Cannot determine if pages need to be shown.");
            }
            
            return false;
        }

        return !Pio.CurrentPlayerSettings.NeedToSeeFromSceneCamera;
    }
    
    /// <summary>
    /// Toggles a specific group of UI elements on or off.
    /// </summary>
    private void ToggleUI(EToggleTarget target, bool active)
    {
        switch (target)
        {
            case EToggleTarget.PlayerPlaying:
                
                if (!ShowPlayerPagesNeeded()) active = false;
                
                TogglePlayerUICanvas(active);
                
                TogglePlayerUICanvasPlayPage(active);
                
                if (!active) // reverse the case for the scene canvas when dealing with player canvas.
                             // Player doesn't need own canvas so will need to use scene canvas
                {
                    TogglePlayerSceneUICanvas(true);
                }
                
                break;
            
            case EToggleTarget.PlayerPaused:
                
                if (!ShowPlayerPagesNeeded()) active = false;
                
                TogglePlayerUICanvas(active);
                
                TogglePlayerUICanvasPausePage(active);
                
                if (!active) // same as above
                {
                    TogglePlayerSceneUICanvas(true);
                }
                
                break;
            
            case EToggleTarget.PlayerWon:
                
                if (!ShowPlayerPagesNeeded()) active = false;
                
                TogglePlayerUICanvas(active);
                    
                TogglePlayerUICanvasWonPage(active);
                
                if (!active) // same as above
                {
                    TogglePlayerSceneUICanvas(true);
                }
                
                break;
            
            case EToggleTarget.PlayerLost:
                
                if (!ShowPlayerPagesNeeded()) active = false;
                
                TogglePlayerUICanvas(active);
                
                TogglePlayerUICanvasLostPage(active);
                
                if (!active) // same as above
                {
                    TogglePlayerSceneUICanvas(true);
                }
                
                break;
            
            case EToggleTarget.SceneUICanvas:
                
                TogglePlayerSceneUICanvas(active);
                
                break;
            
            case EToggleTarget.All:
                
                TogglePlayerSceneUICanvas(active);
                
                if (!ShowPlayerPagesNeeded()) active = false;
                
                TogglePlayerUICanvas(active);
                
                TogglePlayerUICanvasPlayPage(active);
                
                TogglePlayerUICanvasPausePage(active);
                
                TogglePlayerUICanvasSettingsPage(active);
                
                TogglePlayerUICanvasWonPage(active);
                
                TogglePlayerUICanvasLostPage(active);
                
                break;
                
        }
    }
    
    private void TogglePlayerUICanvas(bool active)
    {
        playerObjectUiCanvas.gameObject.SetActive(active);
    }
    
    private void TogglePlayerUICanvasPlayPage(bool active)
    {
        playerObjUiPlayPage.gameObject.SetActive(active);
    }
    
    private void TogglePlayerUICanvasPausePage(bool active)
    {
        playerObjUiPausePage.gameObject.SetActive(active);
    }
    
    private void TogglePlayerUICanvasSettingsPage(bool active)
    {
        playerObjSettingsPage.gameObject.SetActive(active);
    }
    
    private void TogglePlayerUICanvasWonPage(bool active)
    {
        playerObjUiWonPage.gameObject.SetActive(active);
    }
    
    private void TogglePlayerUICanvasLostPage(bool active)
    {
        playerObjUiLostPage.gameObject.SetActive(active);
    }
    
    private void TogglePlayerSceneUICanvas(bool active)
    {
        sceneUiCanvas.gameObject.SetActive(active);
    }
}
