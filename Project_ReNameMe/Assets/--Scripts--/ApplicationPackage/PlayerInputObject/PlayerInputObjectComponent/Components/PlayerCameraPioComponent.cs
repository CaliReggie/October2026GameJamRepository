using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// The PlayerCameraPioComponent class is responsible for managing the Pios camera system,
/// using various view types, controls and settings to do so. Leverages CineMachine for camera control.
/// </summary>
public class PlayerCameraPioComponent : PioComponent
{
    /// <summary>
    /// The different types of player cameras available to the player to use as their main view.
    /// </summary>
    public enum EPlayerCameraType
    {
        [InspectorName(null)] // Don't want to show as setting in inspector
        PlayerFixed, // playerObjectCamera + firstPersonVirtual, static position and rotation set by target position
        [Tooltip("Player Object First Person Camera View")]
        PlayerFirstPerson, // playerObjectCamera + firstPersonVirtual, dynamic position and rotation
        [Tooltip("Player Object Third Person Orbit Camera View. Player can rotate/move independent of look direction.")]
        PlayerThirdOrbit, // playerObjectCamera + thirdPersonOrbitVirtual, dynamic position and rotation
        [Tooltip("Player Object Third Person Fixed Camera View. Player faces/moves corresponding to look direction.")]
        PlayerThirdFixed, // playerObjectCamera + thirdPersonFixedVirtual, dynamic position and rotation
        [Tooltip("Player Object Scene Camera View. Uses the Scene Camera in the scene. " +
                 "Player can rotate/move independent of look direction.")]
        SceneCamera // SceneCamera in scene, static position and rotation set SceneCamera location
    }
    
    [Header("Inscribed References")]
    
    [Tooltip("The PlayerObjectPioComponent attached/childed to Pio.")]
    [SerializeField] private PlayerObjectPioComponent playerObjectComponent;
    
    [Tooltip("The default position target for camera type. Null will lead to no follow.")]
    [SerializeField] private Transform playerObjectCameraPosition;
    
    [Tooltip("The default look orientation for the camera in types directly associated with the player object.")]
    [SerializeField] private Transform playerObjectLookOrientation;
    
    [Tooltip("The real camera component to be used in camera types directly associated with the player object.")]
    [SerializeField] private Camera playerObjectCamera;
    
    [Tooltip("CineMachine Virtual Camera to use for PlayerFirstPerson type")]
    [SerializeField] private CinemachineCamera firstPersonVirtual;
    
    [Tooltip("CineMachine Virtual Camera to use for PlayerThirdOrbit type")]
    [SerializeField] private CinemachineCamera thirdPersonOrbitVirtual;
    
    [Tooltip("CineMachine Virtual Camera to use for PlayerThirdFixed type")]
    [SerializeField] private CinemachineCamera thirdPersonFixedVirtual;
    
    [Tooltip("The objects to dynamically set to not be seen by, or interfere with this player's camera in relevant camera types.\n" +
             "Sets the layer of said objects recursively to do so by using the masks and layers in below settings.")]
    [SerializeField] private List<Transform> playerSpecificLayerObjects = new ();
    
    [Header("Inscribed Settings")]
    
    [Tooltip("Vertical rotation range clamp for the camera orientation")]
    [SerializeField] private Vector2 verticalRotRange = new (-89, 89);
    
    [Space]
    
    [Tooltip("Layers rendered by general player cameras.")]
    [SerializeField] private LayerMask generalPlayerCullingMask = -1;
    
    [Tooltip("The layer assigned to general player objects to be used for culling in relevant camera types.")]
    [SerializeField] private string generalPlayerLayer = "Player";
    
    [Space]
    
    [Tooltip("Layers rendered by player 1 camera.")]
    [SerializeField] private LayerMask player1CullingMask = -1;
    
    [Tooltip("The layer assigned to player 1 objects to be used for culling in relevant camera types.")]
    [SerializeField] private string player1Layer = "Player1Specific";
    
    [Space]
    
    [Tooltip("Layers rendered by player 2 camera.")]
    [SerializeField] private LayerMask player2CullingMask = -1;
    
    [Tooltip("The layer assigned to player 2 objects to be used for culling in relevant camera types.")]
    [SerializeField] private string player2Layer ="Player2Specific";
    
    [Space]
        
    [Tooltip("Layers rendered by player 3 camera.")]
    [SerializeField] private LayerMask player3CullingMask = -1;
    
    [Tooltip("The layer assigned to player 3 objects to be used for culling in relevant camera types.")]
    [SerializeField] private string player3Layer = "Player3Specific";
    
    [Space]
    
    [Tooltip("Layers rendered by player 4 camera.")]
    [SerializeField] private LayerMask player4CullingMask = -1;
    
    [Tooltip("The layer assigned to player 4 objects to be used for culling in relevant camera types.")]
    [SerializeField] private string player4Layer = "Player4Specific";
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [Tooltip("The PlayerInput component attached to Pio.")]
    [SerializeField] private PlayerInput playerInputComponent;
    
    [Tooltip("The CineMachineBrain component attached to playerObjectCamera.")]
    [SerializeField] private CinemachineBrain brainComponent;
    
    [Tooltip("The CineMachineOrbitalFollow component attached to thirdPersonOrbitVirtual.")]
    [SerializeField] private CinemachineOrbitalFollow thirdOrbitalComponent;
    
    [Tooltip("The PlayerCursorPioComponent attached to Pio.")]
    [SerializeField] private PlayerCursorPioComponent playerCursorComponent;
    
    [Tooltip("Current Virtual Camera if the current type supports it, null otherwise.")]
    [SerializeField] private CinemachineCamera currentVirtualCam;
    
    [Tooltip("Current position target for the camera type. If null will not move/follow.")]
    [SerializeField] private Transform currentTargetPosition;
    
    [Tooltip("The SceneCamera script instance in the scene.")]
    [SerializeField] private SceneCamera currentSceneCameraScript;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("Current look input from player.")]
    [SerializeField] private Vector2 currentLookInput;
    
    [Tooltip("Current target rotation for the camera type in EulerAngles.")]
    [SerializeField] private Vector3 targetEulerRotation;
    
    [Tooltip("The current camera type being used.")]
    [field: SerializeField] public EPlayerCameraType CurrentCameraType  { get; private set; }
    
    [Tooltip("The sensitivity multiplier for camera look input")]
    [Range(0.01f, 99.99f)] [SerializeField] private float currentCameraSensitivity = 1f;
    
    [Tooltip("The target full screen rect, changed on screen size changes," +
             " used to calculate new player cam rect on screen size changes.")]
    [SerializeField] private Rect targetFullScreenArea;
    
    [Tooltip("The received camera rect for this playerObjectCamera from the last player join/leave event." +
             " Used to calculate new rect on screen size changes.")]
    [SerializeField] private Rect receivedPlayerCamRect;
    
    [Tooltip("The target camera rect for this playerObjectCamera to be set to on screen size changes," +
             " calculated from receivedPlayerCamRect and targetAspect.")]
    [SerializeField] private Rect targetPlayerCamRect;
    
    [Tooltip("The target aspect ratio for the Player Camera. Used to force the Player Camera to this ratio.")]
    [SerializeField] private float targetAspect = 16f / 9f;
    
    [Tooltip("If true, in split screen. Player will take up half, or as large a chunk as possible to fill screen.")]
    [SerializeField] private bool fillSplitScreen;
    
    // todo; add another split screen behaviour for top / bottom instead of left and right?

    /// <summary>
    /// The current look orientation (transform) based on the current camera type. Can be used to know
    /// exactly what orientation the player is seeing at.
    /// </summary>
    public Transform CurrentLookOrientation => CurrentCameraType == EPlayerCameraType.SceneCamera ? 
        currentSceneCameraScript.Camera.transform : playerObjectLookOrientation;
    
    //ADDED FOR SQUIRREL GAME
    public void SetLockedInPlaceCam(bool isLockedInPlace) // true means third fixed, false means third orbit
    {
        if (isLockedInPlace)
        {
            ConfigureCamera(EPlayerCameraType.PlayerThirdFixed, playerObjectCameraPosition);
        }
        else
        {
            ConfigureCamera(EPlayerCameraType.PlayerThirdOrbit, playerObjectCameraPosition);
        }
    }
    
    /// <summary>
    /// Ensures certain settings are correct
    /// </summary>
    private void OnValidate()
    {
        // ensure vertical rot range is valid (cannot be more than 89 degrees up or down)
        if (verticalRotRange.x < -89)
        {
            verticalRotRange.x = -89;
        }
        
        if (verticalRotRange.y > 89)
        {
            verticalRotRange.y = 89;
        }
    }
    
    /// <summary>
    /// Received by message sent from PlayerInput component on Pio when Look input action is performed.
    /// </summary>
    public void OnLook(InputValue value)
    {
        // get and store input vector
        Vector2 inputVector = value.Get<Vector2>();
        
        currentLookInput = inputVector;
    }
    
    /// <summary>
    /// Message to be received from InputManager Message Broadcast when a player joins.
    /// </summary>
    public void OnPlayerJoined(PlayerInput joinedPlayerInput)
    {
        // update the received rect from the PlayerManager changing the player cam settings on join
        receivedPlayerCamRect = this.playerObjectCamera.rect;
        
        // player bounds will likely change, update
        UpdateCameraBounds();
    }
    
    /// <summary>
    /// Message to be received from InputManager Message Broadcast when a player leaves.
    /// </summary>
    public void OnPlayerLeft(PlayerInput joinedPlayerInput)
    {
        receivedPlayerCamRect = this.playerObjectCamera.rect;
        
        // player bounds will likely change, update
        UpdateCameraBounds();
    }
    
    protected override void Initialize()
    {
        if (!CheckInscribedReferences())
        {
            return;
        }
        
        if (!SetDynamicReferences())
        {
            return;
        }
        
        InitCameras();
        
        targetFullScreenArea = new Rect(Screen.mainWindowPosition.x, Screen.mainWindowPosition.y, Screen.width, Screen.height);
        
        Initialized = true;
        
        return;
        
        // Checks inscribed refs and logs error if any are missing
        bool CheckInscribedReferences()
        {
            if (playerObjectComponent == null ||
                playerObjectLookOrientation == null ||
                playerObjectCamera == null ||
                firstPersonVirtual == null ||
                thirdPersonOrbitVirtual == null ||
                thirdPersonFixedVirtual == null)
            {
                Debug.LogError($"{ GetType().Name}:" +
                               $" One or more inscribed references are not set in the Inspector on {gameObject.name}.");
                return false;
            }
            
            return true;
        }
        
        // Sets dynamic refs and logs error if any are missing
        bool SetDynamicReferences()
        {
            try
            {
                // get third person orbital component from virtual camera
                thirdOrbitalComponent = thirdPersonOrbitVirtual.GetComponent<CinemachineOrbitalFollow>();
                
                // get PlayerInput component from Pio
                playerInputComponent = Pio.GetComponent<PlayerInput>();
                
                // get CineMachineBrain component from playerObjectCamera
                brainComponent = playerObjectCamera.GetComponent<CinemachineBrain>();
                
                // get PlayerCursorPioComponent from Pio
                playerCursorComponent = Pio.GetComponent<PlayerCursorPioComponent>();
                
                // get SceneCamera instance in scene
                currentSceneCameraScript = SceneCamera.Instance;
                
                if (thirdOrbitalComponent == null ||
                    playerInputComponent == null ||
                    brainComponent == null ||
                    playerCursorComponent == null ||
                    currentSceneCameraScript == null)
                {
                    Debug.LogError($"{ GetType().Name}: Error setting dynamic references.");
                    
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{ GetType().Name}: Error setting dynamic references: {e.Message}");
                
                return false;
            }
            
            return true;
        }
        
        // initializes all default camera position, tracking, and channel settings
        void InitCameras()
        { 
            // set default target position
            currentTargetPosition = playerObjectCameraPosition;
            
            // ensure playerObjectCamera starts off disabled
            playerObjectCamera.enabled = false;
            playerObjectCamera.gameObject.SetActive(false);
            
            // look at is how CineMachine cameras rotate/orient themselves
            firstPersonVirtual.LookAt = playerObjectLookOrientation;
            
            // ensure start off
            firstPersonVirtual.enabled = false;
            firstPersonVirtual.gameObject.SetActive(false);
        
            thirdPersonOrbitVirtual.LookAt = playerObjectLookOrientation;
            thirdPersonOrbitVirtual.enabled = false;
            thirdPersonOrbitVirtual.gameObject.SetActive(false);
            
            thirdPersonFixedVirtual.LookAt = playerObjectLookOrientation;
            thirdPersonFixedVirtual.enabled = false;
            thirdPersonFixedVirtual.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Takes a CineMachine Virtual Camera and pairs its output channel with the attached CineMachineBrain
    /// </summary>
    private void PairCameraChannels(CinemachineCamera virtualCam)
    {
        //cannot pair if not initialized
        if (!Initialized) return;
        
        //cannot pair null camera
        if (virtualCam == null) return;
        
        // set brain channel mask based on Pio visual index
        brainComponent.ChannelMask = (OutputChannels)(1 << Pio.VisualIndex); // 1,2,4,8 for channels 0-3
        
        // set virtual cam output channel to match brain
        virtualCam.OutputChannel = brainComponent.ChannelMask;
    }
    
    /// <summary>
    /// Takes a CineMachine Virtual Camera and unpairs its output channel from the attached CineMachineBrain
    /// </summary>
    private void UnpairCameraChannels(CinemachineCamera virtualCam)
    {
        //cannot unpair if not initialized
        if (!Initialized) return;
        
        //cannot unpair null camera
        if (virtualCam == null) return;

        // reset brain channel mask to default
        virtualCam.OutputChannel = OutputChannels.Default;
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {
            case PlayerInputObject.EPlayerInputObjectState.Off:
                
                if (enabled)
                {
                    // deactivate Player Camera if it exists
                    if (playerObjectCamera != null)
                    {
                        playerObjectCamera.enabled = false;
                        playerObjectCamera.gameObject.SetActive(false);
                    }
        
                    // deactivate the current virtual camera if it exists
                    if (currentVirtualCam != null)
                    {
                        currentVirtualCam.enabled = false;
                        currentVirtualCam.gameObject.SetActive(false);
                        UnpairCameraChannels(currentVirtualCam);
                        currentVirtualCam = null;
                    }
                    
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
            case PlayerInputObject.EPlayerInputObjectState.PlayerUi:
            case PlayerInputObject.EPlayerInputObjectState.PlayerSceneUi:
            case PlayerInputObject.EPlayerInputObjectState.SceneUi: // Also on for this bc screen bounds updates

                // if off, activate from defaults
                if (!enabled)
                {
                    // logic varies if player using SceneCamera type
                    bool targetTypeIsSceneCamera = Pio.CurrentPlayerSettings.CameraType == EPlayerCameraType.SceneCamera;
                
                    if (targetTypeIsSceneCamera)
                    {
                        // on switches to Scene camera, update and validate reference to currentSceneCameraScript
                        currentSceneCameraScript = SceneCamera.Instance;
                        
                        // cannot switch if no SceneCamera in scene
                        if (currentSceneCameraScript == null)
                        {
                            Debug.LogError($"{ GetType().Name}: Cannot switch to Scene Camera because" +
                                           $" it does not exist in the scene.");
                            
                            return;
                        }
                        
                        // configure to use SceneCamera type and its transform as target position
                        ConfigureCamera(EPlayerCameraType.SceneCamera, currentSceneCameraScript.Camera.transform);
                    }
                    else
                    {
                        // configure to use player object camera type and its default target position
                        ConfigureCamera(Pio.CurrentPlayerSettings.CameraType, playerObjectCameraPosition);
                    }
                    
                    // set initial target rotation based on current player object euler rotation
                    targetEulerRotation = new Vector3( 0, playerObjectComponent.ObjectEulerRotation.y, 0);
                    
                    enabled = true;
                }
                    
                break;
        }
    }

    protected override void OnAfterPioSettingsChange(PlayerSettingsSo playerSettings)
    {
        try
        {
            // if enabled, configure active camera settings
            if (enabled)
            {
                // logic varies if player using SceneCamera type
                bool targetTypeIsSceneCamera = playerSettings.CameraType == EPlayerCameraType.SceneCamera;
                
                if (targetTypeIsSceneCamera)
                {
                    // on switches to Scene camera, update and validate reference to currentSceneCameraScript
                    currentSceneCameraScript = SceneCamera.Instance;
                    
                    // cannot switch if no SceneCamera in scene
                    if (currentSceneCameraScript == null)
                    {
                        Debug.LogError($"{ GetType().Name}: Cannot switch to Scene Camera because" +
                                       $" it does not exist in the scene.");
                        
                        return;
                    }
                    
                    // configure to use SceneCamera type and its transform as target position
                    ConfigureCamera(EPlayerCameraType.SceneCamera, currentSceneCameraScript.Camera.transform);
                }
                else
                {
                    // configure to use player object camera type and its default target position
                    ConfigureCamera(playerSettings.CameraType, playerObjectCameraPosition);
                }
            }
            
            // apply sensitivity setting
            currentCameraSensitivity = playerSettings.CameraSensitivity;
            
            fillSplitScreen = playerSettings.FillSplitScreen;
        }
        catch (Exception e)
        {
            Debug.LogError($"{ GetType().Name}:{name}: Error applying new player settings: {e.Message}");
        }
    }
    
    /// <summary>
    /// Configures player camera with specified type and position target. Must be initialized.
    /// </summary>
    private void ConfigureCamera(EPlayerCameraType targetType, Transform targetPos)
    {
        // cannot configure if not initialized
        if (!Initialized)
        {
            Debug.LogError($"{ GetType().Name}: Cannot configure camera before initialization.");
            
            return;
        }
        
        //set the position target. null (no follow) by default is allowed.
        currentTargetPosition = targetPos;
        
        // Set the player cam type to the target type.
        CurrentCameraType = targetType;
        
        // deactivate the current virtual camera if it exists
        if (currentVirtualCam != null)
        {
            currentVirtualCam.enabled = false;
            currentVirtualCam.gameObject.SetActive(false);
            UnpairCameraChannels(currentVirtualCam);
            currentVirtualCam = null;
        }
        
        int oneBasedPlayerIndex = Pio.VisualIndex;
        
        // logic based on camera type
        switch (CurrentCameraType)
        {
            // for fixed, set position and rotation directly and that's it
            case EPlayerCameraType.PlayerFixed:
                
                //set cam position and rotation if target assigned
                if (currentTargetPosition != null)
                {
                    playerObjectCamera.transform.
                        SetPositionAndRotation(currentTargetPosition.position, currentTargetPosition.rotation);
                }
                
                break;
            // for player camera types, assign the correct virtual cam and culling mask
            case EPlayerCameraType.PlayerFirstPerson:
                
                currentVirtualCam = firstPersonVirtual;
                
                switch (oneBasedPlayerIndex)
                {
                    default:
                         Debug.LogWarning($"{ GetType().Name}: Unexpected player visual index {oneBasedPlayerIndex} for setting culling mask.");
                         playerObjectCamera.cullingMask = player1CullingMask;
                         break;
                    case 1:
                        playerObjectCamera.cullingMask = player1CullingMask;
                        break;
                    case 2:
                        playerObjectCamera.cullingMask = player2CullingMask;
                        break;
                    case 3:
                        playerObjectCamera.cullingMask = player3CullingMask;
                        break;
                    case 4:
                        playerObjectCamera.cullingMask = player4CullingMask;
                        break;
                }
                
                break;
            case EPlayerCameraType.PlayerThirdOrbit:
                
                currentVirtualCam = thirdPersonOrbitVirtual;

                playerObjectCamera.cullingMask = generalPlayerCullingMask;
                
                break;
            case EPlayerCameraType.PlayerThirdFixed:
                
                currentVirtualCam = thirdPersonFixedVirtual;

                playerObjectCamera.cullingMask = generalPlayerCullingMask;
                
                break;
            // for Scene camera types, no modification of position at all, we assume it is purposefully done by 
            // scene camera management
            case EPlayerCameraType.SceneCamera:
                
                break;
                
        }
        
        // managing setting layers of specified gameObjects from inscribed rendering settings
        int playerSpecificRenderObjectLayerInt;

        switch (oneBasedPlayerIndex)
        {
            default:
                 Debug.LogWarning($"{ GetType().Name}: Unexpected player visual index {oneBasedPlayerIndex}" +
                                  $" for setting specific object layers.");
                 playerSpecificRenderObjectLayerInt = LayerMask.NameToLayer(generalPlayerLayer);
                 break;
            case 1:
                playerSpecificRenderObjectLayerInt = LayerMask.NameToLayer(player1Layer);
                break;
            case 2:
                playerSpecificRenderObjectLayerInt = LayerMask.NameToLayer(player2Layer);
                break;
            case 3:
                playerSpecificRenderObjectLayerInt = LayerMask.NameToLayer(player3Layer);
                break;
            case 4:
                playerSpecificRenderObjectLayerInt = LayerMask.NameToLayer(player4Layer);
                break;
        }
        
        foreach (Transform obj in playerSpecificLayerObjects)
        {
            SetGameObjectLayerRecursively(obj, playerSpecificRenderObjectLayerInt);
        }

        // If we have a valid virtualCam, configure it
        if (currentVirtualCam != null)
        { 
            PairCameraChannels(currentVirtualCam);
            
            //turn on virtual cam
            currentVirtualCam.enabled = true;
            currentVirtualCam.gameObject.SetActive(true);
        }
        
        // ensure ScenePlayerCam is active if not already
        if (!playerObjectCamera.enabled)
        {
            playerObjectCamera.enabled = true;
            playerObjectCamera.gameObject.SetActive(true);
        }
        
        // logic to enable/disable ScenePlayerCam varies if SceneCamera type or not
        bool isSceneCameraType = CurrentCameraType == EPlayerCameraType.SceneCamera;
        
        // if not SceneCamera, ensure ScenePlayerCam is active if not already
        if (!isSceneCameraType)
        {
            playerObjectCamera.enabled = true;
            playerObjectCamera.gameObject.SetActive(true);
        }
        // if is SceneCamera, ensure ScenePlayerCam is off
        else
        {
            playerObjectCamera.enabled = false;
            playerObjectCamera.gameObject.SetActive(false);
        }
        
        UpdateCameraBounds();
    }
    private void Update()
    {
        // if screen size changes update to match
        if (Screen.width != (int) targetFullScreenArea.width || Screen.height != (int)targetFullScreenArea.height)
        {
            UpdateCameraBounds();
        }   
        
        // skip update if not initialized or using non-controllable camera type
        bool isNonControllableCameraType = CurrentCameraType == EPlayerCameraType.PlayerFixed || 
                                       CurrentCameraType == EPlayerCameraType.SceneCamera;
        
        if (!Initialized || isNonControllableCameraType) {return;}
        
        UpdateOrientationPosition();
        
        UpdateOrientationRotation();
        
        return; 
        
        // following position target if assigned
        void UpdateOrientationPosition()
        {
            if (currentTargetPosition != null)
            {
                playerObjectLookOrientation.position = currentTargetPosition.transform.position;
            }
        }
        
        // updating orientation rotation based on look input
        void UpdateOrientationRotation()
        {
            targetEulerRotation.y += currentLookInput.x * currentCameraSensitivity * Time.deltaTime;
            
            targetEulerRotation.x -= currentLookInput.y * currentCameraSensitivity * Time.deltaTime;
            
            targetEulerRotation.x = Mathf.Clamp(targetEulerRotation.x, verticalRotRange.x, verticalRotRange.y);
            
            targetEulerRotation.y = Mathf.Repeat(targetEulerRotation.y, 360);
            
            playerObjectLookOrientation.rotation = Quaternion.Euler(targetEulerRotation.x, targetEulerRotation.y, 0);
            
            // drive third person orbital component based on orientation if active
            bool thirdOrbitActive = CurrentCameraType == EPlayerCameraType.PlayerThirdOrbit &&
                                    thirdOrbitalComponent != null;
        
            if (thirdOrbitActive)
            {
                thirdOrbitalComponent.HorizontalAxis.Value = targetEulerRotation.y;
                
                thirdOrbitalComponent.VerticalAxis.Value = targetEulerRotation.x;
            }
        }
    }
    
    private void UpdateCameraBounds()
    {
        targetFullScreenArea = new Rect(Screen.mainWindowPosition.x, Screen.mainWindowPosition.y, Screen.width, Screen.height);

        bool isTwoPlayerSplit = false;

        try
        {
            if (ApplicationManager.Instance != null)
            {
                targetAspect = ApplicationManager.Instance.ActiveSceneSettings.TargetAspectRatio;
                isTwoPlayerSplit = ApplicationManager.Instance.ActiveSceneSettings.PlayerManagerSettings.TargetPlayers == 2;
            }
            
            if (PlayerManager.Instance != null)
            {
                if (PlayerManager.Instance.NumPlayers == 2)
                {
                    isTwoPlayerSplit = true;
                }
            }
            
            fillSplitScreen = Pio.CurrentPlayerSettings.FillSplitScreen;
        }
        catch
        {
            // nada, use inscribed
        }

        float windowAspect = (float)Screen.width / Screen.height;

        Rect container = receivedPlayerCamRect;
        
        // fix for single non managed player case reading disabled camera rect as zeroed out
        if (container.height == 0 || container.width == 0)
        {
            container = new Rect(0, 0, 1, 1);
        }
        
        Rect targetRect = container;
        
        // edge cases on handling for two player splitscreen typing
        // todo: add vertical split rather than horizontal?
        if (isTwoPlayerSplit)
        {
            float cw = container.width;
            float ch = container.height;

            // filling split screen area fills half of main screen or as much as possible
            if (fillSplitScreen)
            {
                if (windowAspect > targetAspect)
                {
                    float scale = targetAspect / windowAspect;
                    float fittedWidth = cw * scale;

                    targetRect.width  = fittedWidth;
                    targetRect.height = ch;

                    targetRect.x = container.x + (cw - fittedWidth) / 2f;
                    targetRect.y = container.y;
                }
                else
                {
                    float scale = windowAspect / targetAspect;
                    float fittedHeight = ch * scale;

                    targetRect.width  = cw;
                    targetRect.height = fittedHeight;

                    targetRect.x = container.x;
                    targetRect.y = container.y + (ch - fittedHeight) / 2f;
                }
            }
            else
            // if not, maintains aspect ratio for half the screen area
            {
                float pixelWidth  = cw * Screen.width;
                float pixelHeight = ch * Screen.height;

                float containerAspect = pixelWidth / pixelHeight;

                if (containerAspect > targetAspect)
                {
                    float fittedPixelWidth = pixelHeight * targetAspect;
                    float fittedWidth = fittedPixelWidth / Screen.width;

                    targetRect.width  = fittedWidth;
                    targetRect.height = ch;

                    targetRect.x = container.x + (cw - fittedWidth) / 2f;
                    targetRect.y = container.y;
                }
                else
                {
                    float fittedPixelHeight = pixelWidth / targetAspect;
                    float fittedHeight = fittedPixelHeight / Screen.height;

                    targetRect.width  = cw;
                    targetRect.height = fittedHeight;

                    targetRect.x = container.x;
                    targetRect.y = container.y + (ch - fittedHeight) / 2f;
                }
            }

            ApplyRect(targetRect);
            return;
        }
        
        // normal aspect ratio fitting for non-split screen or single player
        
        // fitting to height if taller than target aspect
        if (windowAspect < targetAspect)
        {
            float scale = windowAspect / targetAspect;
            float fittedHeight = container.height * scale;

            targetRect.width  = container.width;
            targetRect.height = fittedHeight;

            targetRect.x = container.x;
            targetRect.y = container.y + (container.height - fittedHeight) / 2f;
        }
        // fitting to width if wider than target aspect
        else
        {
            float scale = targetAspect / windowAspect;
            float fittedWidth = container.width * scale;

            targetRect.width  = fittedWidth;
            targetRect.height = container.height;

            targetRect.x = container.x + (container.width - fittedWidth) / 2f;
            targetRect.y = container.y;
        }

        ApplyRect(targetRect);
        
        return;
        
        void ApplyRect(Rect rect)
        {
            targetPlayerCamRect = rect;
            playerObjectCamera.rect = rect;
            playerCursorComponent?.UpdateCursorBounds();
        }
    }
    
    // private void SetGameObjectLayer(Transform obj, int layer)
    // {
    //     if (obj == null)
    //     {
    //         Debug.LogWarning($"{ GetType().Name}: Cannot set layer for null object.");
    //         return;
    //     }
    //     
    //     obj.gameObject.layer = layer;
    // }
    
    private void SetGameObjectLayerRecursively(Transform obj, int layer)
    {
        obj.gameObject.layer = layer;
        
        if (obj == null)
        {
            Debug.LogWarning($"{ GetType().Name}: Cannot set layer recursively for null object.");
            return;
        }
    
        foreach (Transform child in obj)
        {
            SetGameObjectLayerRecursively(child, layer);
        }
    }
}