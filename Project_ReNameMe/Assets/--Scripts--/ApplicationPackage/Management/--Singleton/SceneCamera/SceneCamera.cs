using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CinemachineBrain))]
public class SceneCamera : Singleton<SceneCamera>
{
    [Header("Inscribed References")]
    
    [Tooltip("The Camera component attached to this GameObject.")]
    [field: SerializeField] public Camera Camera { get; private set; }
    
    [Tooltip("The CineMachine Brain component attached to this GameObject.")]
    [SerializeField] private CinemachineBrain cinBrain;
    
    [Tooltip("The Transform that the SceneCamera looks at, if set in inspector.")]
    [SerializeField] private Transform cameraLookAtTransform;
    
    [Tooltip("The GameObject that covers the camera view when no player needs to see from it.")]
    [SerializeField] private GameObject cameraCover;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [Tooltip("The initial position that the camera will follow when no positions are being tracked." +
             " Set to the camera look at transform's position on Start.")]
    [SerializeField] private Vector3 initialLookFollow;
    
    [Tooltip("The position that the camera will follow. Updated by follow position tracking requesters (ex: players) through UpdateTrackedPosition and RemoveTrackedPosition." +
             " If no positions are being tracked, it will be set to the initial target follow position.")]
    [SerializeField] private Vector3 targetLookFollow;

    [Tooltip("If in a scene without player management," +
             " will attempt to find an existing Pio in scene to check for state related behaviour." +
             " This will / should be null if in an Player Managed scene.")]
    [SerializeField]
    private PlayerInputObject singlePlayerPioReference;

    /// <summary>
    /// Set this to a reference, and it will be checked in the next update. For state related catch up / lag. 
    /// </summary>
    private PlayerManagerSettingsSo playerManagerSettingsNextFrame;
    
    /// <summary>
    /// Dictionary mapping position tracking requesters (int, ex:PlayerIDs) to the positions they want the camera to follow.
    /// The camera will follow the average of all tracked positions.
    /// If no positions are being tracked, it will follow the initial target follow position.
    /// </summary>
    private readonly Dictionary<int, Vector3> trackedPositions = new ();
    
    public void UpdateTrackedPosition(int playerID, Vector3 followTargetPosition)
    {
        trackedPositions[playerID] = followTargetPosition;
        
        Vector3 averagePosition = Vector3.zero;
        
        foreach (Vector3 position in trackedPositions.Values)
        {
            averagePosition += position;
        }
        
        averagePosition /= trackedPositions.Count;
        
        targetLookFollow = averagePosition;
    }
    
    public void RemoveTrackedPosition(int playerID)
    {
        trackedPositions.Remove(playerID);
        
        if (trackedPositions.Count == 0)
        {
            targetLookFollow = initialLookFollow;
        }
        else
        {
            Vector3 averagePosition = Vector3.zero;
        
            foreach (Vector3 position in trackedPositions.Values)
            {
                averagePosition += position;
            }
        
            averagePosition /= trackedPositions.Count;
        
            targetLookFollow = averagePosition;
        }
    }
    
    public void ResetAllFollowPositions()
    {
        trackedPositions.Clear();
        
        targetLookFollow = initialLookFollow;
    }
    
    protected override void Awake()
    {
        if (!CheckInscribedReferences())
        {
            ToggleCamera(false);
            
            return;
        }

        ToggleCover(false);
        
        base.Awake();
        
        return;
        
        bool CheckInscribedReferences()
        {
            if (cinBrain == null ||
                Camera == null  ||
                cameraCover == null) 
            {
                Debug.LogError($"{GetType().Name}: Error checking inscribed references.");
                
                return false;
            }
            
            return true;
        }
    }
    
    private void Start()
    {
        if (cameraLookAtTransform != null)
        {
            targetLookFollow = cameraLookAtTransform.position;
            
            initialLookFollow = cameraLookAtTransform.position;
        }
        
        // if PlayerManager exists subscribe to PlayerManager settings change event
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAfterPlayerManagerSettingsChange += OnAfterPlayerManagerSettingsChange;
            
            PlayerManager.Instance.OnAfterStateChange += OnAfterPlayerManagerStateChange;
            
            if (PlayerManager.Instance.Started)
            {
                // trigger the event manually for instant correct state
                OnAfterPlayerManagerSettingsChange(PlayerManager.Instance.CurrentPlayerManagerSettings);
            }
        }
        else
        {
            // if no PlayerManager, attempt to find a single player Pio in scene to check for state related behaviour
            if (singlePlayerPioReference == null)
            {
                singlePlayerPioReference = FindAnyObjectByType<PlayerInputObject>();
                
                if (singlePlayerPioReference != null)
                {
                    // trigger the event manually for instant correct state
                    OnAfterSinglePlayerSettingsChanged(singlePlayerPioReference.CurrentPlayerSettings);
                    
                    singlePlayerPioReference.OnAfterPlayerSettingsChanged += OnAfterSinglePlayerSettingsChanged;
                }
            }
        }
    }

    protected override void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAfterPlayerManagerSettingsChange -= OnAfterPlayerManagerSettingsChange;
            
            PlayerManager.Instance.OnAfterStateChange -= OnAfterPlayerManagerStateChange;
        }
        else
        {
            if (singlePlayerPioReference != null)
            {
                singlePlayerPioReference.OnAfterPlayerSettingsChanged -= OnAfterSinglePlayerSettingsChanged;
            }
        }
        
        base.OnDestroy();
    }
    
    private void LateUpdate()
    {
        if (cameraLookAtTransform != null)
        {
            cameraLookAtTransform.position = targetLookFollow;
        }
        //
        // if (playerManagerSettingsNextFrame != null)
        // {
        //     UpdateCoverFromSettings(playerManagerSettingsNextFrame);
        //     
        //     playerManagerSettingsNextFrame = null;
        // }
    }

    private void ToggleCamera(bool active)
    {
        if (Camera != null)
        {
            Camera.enabled = active;
        }
        
        if (cinBrain != null)
        {
            cinBrain.enabled = active;
        }
        
        gameObject.SetActive(active);
    }
    
    private void ToggleCover(bool active)
    {
        if (cameraCover != null)
        {
            cameraCover.SetActive(active);
        }
    }
    
    private void OnAfterPlayerManagerStateChange(PlayerManager.EPlayerManagementState toState)
    {
        switch (toState)
        {
            case PlayerManager.EPlayerManagementState.SufficientPlayers:
                UpdateCoverFromSettings(PlayerManager.Instance.CurrentPlayerManagerSettings);
                // added for instance in 2DMainCam1P scene on start being black screen
                playerManagerSettingsNextFrame = PlayerManager.Instance.CurrentPlayerManagerSettings;
                break;
        }
    }
    
    private void OnAfterPlayerManagerSettingsChange(PlayerManagerSettingsSo currentPlayerManagerSettings)
    {
        UpdateCoverFromSettings(currentPlayerManagerSettings);
    }
    
    private void OnAfterSinglePlayerSettingsChanged(PlayerSettingsSo currentSinglePlayerSettings)
    {
        UpdateCoverFromSettings(currentSinglePlayerSettings);
    }
    
    /// <summary>
    /// Depending on circumstance, for efficiency, cover the camera.
    /// </summary>
    private void UpdateCoverFromSettings(PlayerManagerSettingsSo currentPlayerManagerSettings)
    {
        // if a single player needs to see from main camera, disable cover
        for (int i = 0; i < currentPlayerManagerSettings.TargetPlayers; i++)
        {
            PlayerSettingsSo playerSettings = currentPlayerManagerSettings.PlayersSettings[i];

            if (playerSettings.NeedToSeeFromSceneCamera)
            {
                ToggleCover(false);
                
                // return if found one
                return;
            }
        }
        
        // otherwise, enable cover
        ToggleCover(true);
    }
    
    /// <summary>
    /// Depending on circumstance, for efficiency, cover the camera.
    /// </summary>
    public void UpdateCoverFromSettings(PlayerSettingsSo playerSettings)
    {
        if (playerSettings == null)
        {
            return;
        }
        
        ToggleCover(!playerSettings.NeedToSeeFromSceneCamera);
    }
}
