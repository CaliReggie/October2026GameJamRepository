 using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

/// <summary>
/// The PlayerCursorPioComponent class extends PioComponent to manage a player-specific cursor GameObject
/// for UI interaction. Can handle both Keyboard&Mouse and Gamepad control schemes, but does not currently
/// support swapping between schemes at runtime or other scheme types.
/// </summary>
public class PlayerCursorPioComponent : PioComponent
{
    /// <summary>
    /// The string name of the Gamepad control scheme
    /// </summary>
    private const string GamepadScheme = "Gamepad";
    
    /// <summary>
    /// The string name of the Keyboard&Mouse control scheme
    /// </summary>
    private const string KeyboardScheme = "Keyboard&Mouse";
    
    /// <summary>
    /// The string name of the Touch control scheme
    /// </summary>
    private const string TouchScheme = "Touch";
    
    [Header("Inscribed References")]
    
    [Tooltip("The prefab to spawn as player cursor")]
    [SerializeField] private GameObject cursorPrefab;
    
    [SerializeField] private LayerMask cursorRaycastLayerMask;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [Tooltip("The PlayerInput component of the Pio")]
    [SerializeField] private PlayerInput pioPlayerInput;
    
    [Tooltip("The currently managed RectTransform references associated with the cursor GameObject")]
    [field: SerializeField] public RectTransform CursorInstance {get; private set;}
    
    [Tooltip("The PlayerUiPioComponent of the Pio")]
    [SerializeField] private PlayerUiPioComponent playerUiComponent;
    
    [Tooltip("The SceneCamera, if present in scene, used for cursor screen to world calculations if needed.")]
    [SerializeField] private SceneCamera sceneCameraReference;
    
    [Tooltip("The canvas the cursor will be used on for player ui")]
    [SerializeField] private Canvas playerUiCanvas;
    
    [Tooltip(" The rect transform of the canvas the cursor will be used on for player ui")]
    [SerializeField] private RectTransform playerUiCanvasRectTransform;
    
    [Tooltip("The camera used to draw cursor on the player ui")]
    [SerializeField] private Camera playerObjectCamera;
    
    [Tooltip("The canvas the cursor will be used on for scene ui")]
    [SerializeField] private Canvas sceneUiCanvas;
    
    [Tooltip(" The rect transform of the canvas the cursor will be used on for scene ui")]
    [SerializeField] private RectTransform sceneUiCanvasRectTransform;
    
    [Tooltip("The tracked previous string control scheme name.")]
    [SerializeField] private string currentControlScheme;
    
    /// <summary>
    /// Returns the current canvas the cursor should be used on based on Pio state. Null if in a state
    /// not set up for cursor use.
    /// </summary>
    private Canvas TargetCursorCanvas
    {
        get
        {
            // cannot get current canvas if no current state
            if (Pio.CurrentState == null) return null;
            
            // if in player ui state, logic varies
            if (Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.PlayerUi)
            {
                // if player using main camera, use scene ui canvas
                if (Pio.CurrentPlayerSettings != null && // don't like null ref check, todo: clean?
                    Pio.CurrentPlayerSettings.CameraType == PlayerCameraPioComponent.EPlayerCameraType.SceneCamera)
                {
                    return sceneUiCanvas;
                }
                // otherwise use player ui canvas
                else
                {
                    return playerUiCanvas;
                }
                
            }
            // if in scene ui or player scene ui state, use scene ui canvas
            else if (Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.SceneUi ||
                     Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.PlayerSceneUi)
            {
                return sceneUiCanvas;
            }
            // otherwise currently no cursor logic setup in another state
            else
            {
                return null;
            }
        }
    }
    
    /// <summary>
    /// Returns the current canvas rect transform the cursor should be used on based on Pio state. Null if in a state
    /// not set up for cursor use.
    /// </summary>
    private RectTransform TargetCanvasRectTransform
    {
        get
        {
            // cannot get current canvas if no current state
            if (Pio.CurrentState == null) return null;
            
            // if in player ui state, logic varies
            if (Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.PlayerUi)
            {
                // if player using main camera, use scene ui canvas rect transform
                if (Pio.CurrentPlayerSettings != null && // don't like null ref check, todo: clean?
                    Pio.CurrentPlayerSettings.CameraType == PlayerCameraPioComponent.EPlayerCameraType.SceneCamera)
                {
                    return sceneUiCanvasRectTransform;
                }
                // otherwise use player ui canvas rect transform
                else
                {
                    return playerUiCanvasRectTransform;
                }
            }
            // if in scene ui or player scene ui state, use scene ui canvas rect transform
            else if (Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.SceneUi ||
                     Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.PlayerSceneUi)
            {
                return sceneUiCanvasRectTransform;
            }
            // otherwise currently no cursor logic setup in another state
            else
            {
                return null;
            }
        }
    }
    
    private Camera TargetDrawCamera
    {
        get
        {
            // cannot get current draw camera if no current state
            if (Pio.CurrentState == null) return null;
            
            // if in player ui state, logic varies
            if (Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.PlayerUi)
            {
                // if player using main camera, use scene camera
                if (Pio.CurrentPlayerSettings != null && // don't like null ref check, todo: clean?
                    Pio.CurrentPlayerSettings.CameraType == PlayerCameraPioComponent.EPlayerCameraType.SceneCamera)
                {
                    return sceneCameraReference.Camera;
                }
                // otherwise use player ui draw camera
                else
                {
                    return playerObjectCamera;
                }
            }
            // if in scene ui or player scene ui state, use scene camera
            else if (Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.SceneUi ||
                     Pio.CurrentState.State == PlayerInputObject.EPlayerInputObjectState.PlayerSceneUi)
            {
                return sceneCameraReference.Camera;
            }
            // otherwise currently no cursor logic setup in another state
            else
            {
                return null;
            }
        }
    }

    [Tooltip("The mouse being managed. Either the Hardware Mouse of K&M player or Virtual Mouse of Gamepad/Touch player")]
    private Mouse _mouse;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("The players specific min and max Vector2 screen bounds")]
    [SerializeField]
    private Vector2[] playerScreenArea;
    
    [Tooltip("The main canvas min and max Vector2 screen bounds")]
    [SerializeField]
    private Vector2[] mainScreenArea;
    
    [Tooltip("If true, cursor movement is constrained to player screen bounds")]
    [SerializeField] private bool cursorConstrained;
    
    [Tooltip("If true, the hardware mouse will be forced to stay within screen bounds when cursor is active. " +
             "If false, the hardware mouse can move freely (meaning freely click elsewhere / off screen), " +
             "but the cursor will still be constrained to relative player bounds.")]
    [SerializeField] private bool realMouseClamped;
    
    [Tooltip("The most recently updated position of K&M / Touch scheme")]
    [SerializeField] private Vector2 pointPos;
    
    [Tooltip("The most recently updated target delta/direction of Gamepad / Touch scheme Virtual Mouse")]
    [SerializeField] private Vector2 virtualPointDir;
    
    [Tooltip("The sensitivity multiplier for gamepad cursor input")]
    [Range(0.01f, 9.99f)] [SerializeField] private float currentCursorSensitivity = 1f;
    
    [Tooltip("The most recently updated bool if any scheme click is pressed")]
    [SerializeField] private bool cursorPressed;
    
    /// <summary>
    /// Message to be received from PlayerInput Message Broadcast when real mouse moves on Keyboard&Mouse scheme.
    /// </summary>
    public void OnPoint(InputValue pointValue)
    {
        pointPos = pointValue.Get<Vector2>();
    }
    
    /// <summary>
    /// Message to be received from PlayerInput Message Broadcast for moving cursor on Gamepad scheme.
    /// </summary>
    public void OnGamepadPoint(InputValue inputVector)
    {
        virtualPointDir = inputVector.Get<Vector2>();
        
        // old method used for converting scaled values on this to 0-1 VVV
        
        // if (pioPlayerInput == null) { return; }
        //
        // InputAction pointAction = pioPlayerInput.actions["GamepadPoint"];
        //
        // // Cast to generic control
        // if (pointAction.activeControl is InputControl<Vector2> vectorControl)
        // {
        //     Vector2 rawValue = vectorControl.ReadUnprocessedValue();
        //     
        //     // scaling the magnitude so there is more range of control for smaller values
        //     float rawValueMagnitude = rawValue.magnitude;
        //
        //     if (rawValue.magnitude <= 0.1f) { return; }
        //     
        //     float scaledMagnitude = Mathf.Pow(rawValueMagnitude, 3f); 
        //     
        //     scaledMagnitude = Mathf.Clamp(scaledMagnitude, 0f, 1f);
        //     
        //     Vector2 scaledRawValue = rawValue.normalized * scaledMagnitude;
        //     
        //     rawVirtualPointDir = scaledRawValue;
        // }
    }
    
    /// <summary>
    /// Message to be received from PlayerInput Message Broadcast for clicking on any scheme.
    /// </summary>
    public void OnClick(InputValue buttonValue)
    {
        cursorPressed = buttonValue.isPressed;
        
        //hide real mouse when using cursor
        if (currentControlScheme == KeyboardScheme && cursorPressed && enabled)
        {
            Cursor.visible = false;
        }
    }
    
    /// <summary>
    /// Message to be received from InputManager Message Broadcast when a player joins.
    /// </summary>
    public void OnPlayerJoined(PlayerInput joinedPlayerInput)
    {
        // player bounds will likely change, update
        UpdateCursorBounds();
    }
    
    /// <summary>
    /// Message to be received from InputManager Message Broadcast when a player leaves.
    /// </summary>
    public void OnPlayerLeft(PlayerInput joinedPlayerInput)
    {
        // player bounds will likely change, update
        UpdateCursorBounds();
    }
    
    /// <summary>
    /// Gets the world position of the cursor based on a raycast from a given camera through the cursor's screen position.
    /// </summary>
    public Vector3 GetCursorHitPosition(Camera fromCamera, float distance, out bool hit)
    {
        Ray ray = GetCursorDirectionRay(fromCamera);
        
        if (Physics.Raycast(ray, out RaycastHit hitInfo, distance, cursorRaycastLayerMask))
        {
            hit = true;
            
            // if debug mode, draw ray to hit point
            if (debugMode)
            {
                Debug.DrawLine(ray.origin, hitInfo.point, Color.green, 1f);
            }
            
            return hitInfo.point;
        }
        else
        {
            // if nothing hit, return a point at the max distance in the direction of the ray
            hit = false;
            
            //if debug mode, draw ray to max distance
            if (debugMode)
            {
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance, Color.red, 1f);
            }
            
            return ray.origin + ray.direction * distance;
        }
    }
    
    /// <summary>
    /// Gets the ray from a given camera to the cursor's screen position, for use in raycasts or other calculations.
    /// </summary>
    public Ray GetCursorDirectionRay(Camera fromCamera)
    {
        Vector3 screenPos = fromCamera.WorldToScreenPoint(CursorInstance.position);
        
        Ray ray = fromCamera.ScreenPointToRay(screenPos);
        
        return ray;
    }
    
        /// <summary>
    /// Way to set the cursor sprite from other scripts if needed,
    /// passing in null will set it to the current player settings sprite.
    /// </summary>
    public void SetOverrideCursor(Sprite newCursorSprite)
    {
        try
        {
            // try to get cursor image and assign new sprite
            Image cursorImage = CursorInstance.GetComponent<Image>();
                
            if (newCursorSprite == null)
            {
                // assign player settings sprite if null
                cursorImage.sprite = Pio.CurrentPlayerSettings.CursorSprite;
            }
            else
            {
                cursorImage.sprite = newCursorSprite;
            }
        }
        catch (Exception)
        {
            return;
        }
    }
    
    /// <summary>
    /// Call to update the screen bounds for this player initially or after their camera space changes. 
    /// </summary>
    public void UpdateCursorBounds()
    {
        // cannot update if not initialized
        if (!Initialized)
        {
            return;
        }
        
        //setting main bounds
        mainScreenArea = new Vector2[2];

        Rect sceneCameraRect;
        
        // using cam viewport from scene camera if exists. main cam should alr account for letterboxing w/rect
        if (sceneCameraReference != null)
        {
            sceneCameraRect = sceneCameraReference.Camera.pixelRect;
            
            mainScreenArea[0] = new Vector2(sceneCameraRect.xMin, sceneCameraRect.yMin);
            
            mainScreenArea[1] = new Vector2(sceneCameraRect.xMax, sceneCameraRect.yMax);
        }
        // otherwise just sample from players camera
        else if (playerObjectCamera != null)
        {
            Rect playerCamRect = playerObjectCamera.pixelRect;
            
            mainScreenArea[0] = new Vector2(playerCamRect.xMin, playerCamRect.yMin); // minimum position
            
            mainScreenArea[1] = new Vector2(playerCamRect.xMax, playerCamRect.yMax); // maximum position
        }
        
        //setting player bounds
        playerScreenArea = new Vector2[2];
        
        // if player ui draw camera exists, sample its rect
        if (playerObjectCamera != null)
        {
            Rect playerCamRect = playerObjectCamera.pixelRect;
            
            playerScreenArea[0] = new Vector2(playerCamRect.xMin, playerCamRect.yMin); // minimum position
            
            playerScreenArea[1] = new Vector2(playerCamRect.xMax, playerCamRect.yMax); // maximum position
        }
        // if not, use main bounds for player bounds
        else
        {
            playerScreenArea[0] = mainScreenArea[0]; // min pos

            playerScreenArea[1] = mainScreenArea[1]; // max pos
        }
    }
    
    protected override void Initialize()
    {
        // make sure inscribed refs are good
        if (!CheckInscribedReferences()) return;
        
        // ensure dynamic refs set
        if (!SetDynamicReferences()) return;
        
        // get mouse ref or add if gamepad
        ConfigureMouseReference();
        
        // ensure starts off
        ToggleCursorInstance(false);
        
        // configure cursor defaults
        ConfigureCursorSettings(CursorLockMode.Locked, false);
        
        Initialized = true;
        
        // update screen bounds
        UpdateCursorBounds();
        
        return;
        
        // checks if all inscribed references are set
        bool CheckInscribedReferences()
        {
            if (cursorPrefab == null)
            {
                Debug.LogError($"{GetType().Name}: Inscribed references not set.");
                
                return false;
            }
            else
            {
                return true;
            }
        }
        
        // sets and checks dynamic references
        bool SetDynamicReferences()
        {
            try
            {
                // getting PlayerInput ref from Pio
                pioPlayerInput = GetComponent<PlayerInput>();

                // getting SceneCamera ref
                sceneCameraReference = SceneCamera.Instance;
                
                // getting PlayerUiPioComponent ref from Pio
                playerUiComponent = GetComponent<PlayerUiPioComponent>();
                
                // getting player ui canvas ref
                playerUiCanvas = playerUiComponent.PlayerObjectUiCanvas;
                
                // getting player ui canvas rect transform ref
                playerUiCanvasRectTransform = playerUiComponent.PlayerObjectUiCanvas.GetComponent<RectTransform>();
                
                // getting player ui draw camera ref
                playerObjectCamera = playerUiComponent.PlayerObjectUiCanvas.worldCamera;
                
                // getting scene ui canvas ref
                sceneUiCanvas = playerUiComponent.SceneUiCanvas;
                
                // getting scene ui canvas rect transform ref
                sceneUiCanvasRectTransform = sceneUiCanvas.GetComponent<RectTransform>();
                
                if (Pio.CurrentPlayerSettings != null)
                {
                    // getting cursor constrained setting from player settings
                    cursorConstrained = Pio.CurrentPlayerSettings.CurrentConfiguration.CursorConstrained;
                    
                    // getting real mouse clamped setting from player settings
                    realMouseClamped = Pio.CurrentPlayerSettings.RealMouseClamped;
                }
                
                // spawning cursor from prefab if null, starting with gameObject inactive.
                if (CursorInstance == null)
                {
                    GameObject cursorGO = Instantiate(cursorPrefab, transform);
                    
                    //assigning to CursorInstance
                    CursorInstance = cursorGO.GetComponent<RectTransform>();
                    
                    //configuring name
                    CursorInstance.name = "Player" + Pio.VisualIndex + "Cursor";
                    
                    //getting cursor image and assigning sprite from player settings if exists
                    Image cursorImage = CursorInstance.GetComponent<Image>();

                    if (Pio.CurrentPlayerSettings != null &&
                        Pio.CurrentPlayerSettings.CursorSprite != null)
                    {
                        cursorImage.sprite = Pio.CurrentPlayerSettings.CursorSprite;
                    }
                }
                
                //checking all others and logging if any are null
                if (pioPlayerInput == null ||
                    sceneCameraReference == null ||
                    playerUiComponent == null ||
                    playerUiCanvas == null ||
                    playerUiCanvasRectTransform == null ||
                    playerObjectCamera == null ||
                    sceneUiCanvas == null ||
                    sceneUiCanvasRectTransform == null)
                {
                    Debug.LogError($"{GetType().Name}: Exception setting dynamic references. One or more references are null.");
                    
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("PlayerCursor: Exception getting dynamic references: " + e);

                return false;
            }
        }
    }
    
    protected virtual void OnEnable()
    {
        //enable hardware cursor control
        InputSystem.onAfterUpdate += UpdateCursor;
    }

    protected void OnDisable()
    {
        //disable hardware cursor control
        InputSystem.onAfterUpdate -= UpdateCursor;
    }

    protected override void OnDestroy()
    {
        // unsub to input system update
        InputSystem.onAfterUpdate -= UpdateCursor;

        // if virtual cursor player remove virtual mouse
        if (currentControlScheme != KeyboardScheme && _mouse != null)
        {
            InputSystem.RemoveDevice(_mouse);
        }
        
        //if cursor instance exists, destroy it
        if (CursorInstance != null)
        {
            Destroy(CursorInstance.gameObject);
        }
        
        base.OnDestroy();
    }
    
    protected override void OnBeforePioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.Off:
            case PlayerInputObject.EPlayerInputObjectState.Player:
                
                // ensure locked cursor if not in Ui state
                ConfigureCursorSettings(
                    toState == PlayerInputObject.EPlayerInputObjectState.Off ? CursorLockMode.None 
                    : CursorLockMode.Locked,
                    false);
                    
                // re-parent to this transform
                CursorInstance.SetParent(transform, false);
            
                //turn off cursor Go
                if (CursorInstance != null)
                {
                    ToggleCursorInstance(false);
                }
                
                if (toState == PlayerInputObject.EPlayerInputObjectState.Off) // no control when totally off
                {
                    enabled = false;
                }
                else
                {
                    enabled = true; // controlling cursor behind the scenes and need to be on if not totally off
                }
                
                break;
        }
    }
    
    protected override void OnAfterPioStateChange(PlayerInputObject.EPlayerInputObjectState toState)
    {
        switch (toState)
        {   
            case PlayerInputObject.EPlayerInputObjectState.PlayerUi:
            case PlayerInputObject.EPlayerInputObjectState.PlayerSceneUi:
            case PlayerInputObject.EPlayerInputObjectState.SceneUi:
                
                // ensure confined (semi unlocked) cursor in Ui states
                ConfigureCursorSettings(CursorLockMode.Confined, false);
                
                // re-parent to current canvas
                CursorInstance.SetParent(TargetCursorCanvas.transform, false);
                
                // ensure on top of canvas
                CursorInstance.SetAsLastSibling();
                
                // ensuring click is false on enable
                cursorPressed = false;
                
                //turn on cursor Go
                ToggleCursorInstance(true);
                
                MoveCursorToCenter();
                
                // enable component if not already
                if (!enabled) 
                {
                    enabled = true;
                }
                
                break;
        }
    }
    
    protected override void OnAfterPioSettingsChange(PlayerSettingsSo playerSettings)
    {
        try
        {
            //happens consistently, good time to re-sample for SceneCamera reference
            sceneCameraReference = SceneCamera.Instance;
            
            // set gamepad sensitivity from player settings
            currentCursorSensitivity = playerSettings.CursorSensitivity;
            
            // try to get cursor image and assign new sprite from player settings
            Image cursorImage = CursorInstance.GetComponent<Image>();
                
            cursorImage.sprite = playerSettings.CursorSprite;
            
            cursorConstrained = playerSettings.CurrentConfiguration.CursorConstrained;

            realMouseClamped = playerSettings.RealMouseClamped;
        }
        catch (Exception e)
        {
            Debug.LogError($"{GetType().Name}:{name}: " +
                           $"Exception updating cursor sprite on player settings change: " + e);
        }
    }
    
    /// <summary>
    /// Configures the cursor settings
    /// </summary>
    private void ConfigureCursorSettings(CursorLockMode hardwareLockMode, bool hardwareCursorVisible)
    {
        Cursor.visible = hardwareCursorVisible;
        
        // can't have multiple players interfering with each other
        if (Pio.IsPlayerManager && PlayerManager.Instance.CurrentPlayerManagerSettings.TargetPlayers > 1) {return;}
        
        Cursor.lockState = hardwareLockMode;
    }
    
    /// <summary>
    /// Gets mouse refernce, or adds on non K&M schemes, and pairs to player if not K&M
    /// </summary>
    private void ConfigureMouseReference()
    {
        //if player is on keyboard
        if (pioPlayerInput.currentControlScheme == KeyboardScheme)
        {
            // assigning real mouse if not already present
            if (_mouse == null)
            {
                // find the mouse among player input devices
                foreach (var device in pioPlayerInput.devices)
                {
                    if (device is Mouse)
                    {
                        // found it, assign and break
                        _mouse = device as Mouse;
                        
                        break;
                    }
                }
            }
            // adding device if not already added
            else if (!_mouse.added)
            {
                InputSystem.AddDevice(_mouse);
            }
            
            currentControlScheme = pioPlayerInput.currentControlScheme;
        }
        //if player is on gamepad
        else if (pioPlayerInput.currentControlScheme == GamepadScheme ||
                 pioPlayerInput.currentControlScheme == TouchScheme)
        {
            //adding virtual mouse if not already present
            if (_mouse == null)
            {
                _mouse = (Mouse) InputSystem.AddDevice("VirtualMouse");
            }
            // adding device if not already added
            else if (!_mouse.added)
            {
                InputSystem.AddDevice(_mouse);
            }
            
            //pairing virtual mouse to player
            InputUser.PerformPairingWithDevice(_mouse, pioPlayerInput.user);
            
            currentControlScheme = pioPlayerInput.currentControlScheme;
        }
        // unhandled control scheme
        else
        {
            Debug.LogError("PlayerCursor: Unhandled control scheme: " + pioPlayerInput.currentControlScheme);
        }
    }
    
    /// <summary>
    /// Toggles the visible cursor gameObject on or off
    /// </summary>
    private void ToggleCursorInstance(bool active)
    {
        if (CursorInstance == null)
        {
            Debug.LogError("PlayerCursor: Cursor Instance null on toggle.");
            
            return;
        }
        
        CursorInstance.gameObject.SetActive(active);
    }
    
    /// <summary>
    /// Takes a V2 position, min, and max and clamps within bounds
    /// </summary>
    private Vector2 ClampedByBounds(Vector2 pos, Vector2 min, Vector2 max)
    {
        // extended logic, using the clamped world position of player object if in relevant state
        Vector2 loc = new Vector2(Mathf.Clamp(pos.x, min.x, max.x), Mathf.Clamp(pos.y, min.y, max.y));
        
        return loc;
    }
    
    /// <summary>
    /// Takes a target pos and moves the cursor GO anchor to that place on current canvas
    /// </summary>
    private void AnchorCursorImage(Vector2 newPos)
    {
        if (!CursorInstance.gameObject.activeInHierarchy)
        {
            return;
        }
        
        Vector2 anchoredPos;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(TargetCanvasRectTransform, newPos, 
            TargetCursorCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TargetDrawCamera , out anchoredPos);
        
        CursorInstance.anchoredPosition = anchoredPos;
    }
    
    /// <summary>
    /// Moves the cursor to the center of the player screen bounds and matches the real
    /// or virtual mouse to that position.
    /// </summary>
    private void MoveCursorToCenter()
    {
        //placing in center of screen
        Vector2 startingPos = (playerScreenArea[0] + playerScreenArea[1]) / 2;

        AnchorCursorImage(startingPos);
        
        // if k&m
        if (pioPlayerInput.currentControlScheme == KeyboardScheme)
        {
            //matching mouse to cursor
            _mouse.WarpCursorPosition(startingPos);

            pointPos = startingPos;
        }
        // if Gp or Touch
        else if (pioPlayerInput.currentControlScheme == GamepadScheme ||
                 pioPlayerInput.currentControlScheme == TouchScheme)
        {
            //reset delta
            InputState.Change(_mouse.delta, Vector2.zero);
            
            //matching virtual mouse to cursor
            InputState.Change(_mouse.position, startingPos);
            
            // updating target Virtual Mouse delta to reflect no dir movement
            virtualPointDir = Vector2.zero;
            
            // updating pointPos to reflect change in pos
            pointPos = startingPos;
        }
    }

    /// <summary>
    /// Subscribed with input system update, cursor will be moved and managed depending on control scheme type
    /// </summary>
    private void UpdateCursor()
    {
        // cannot update if not initialized or not enabled
        if (!Initialized || !enabled)
        {
            return;
        }
        
        // if cursor instance not visibly active, simply clamp cursor to center by using MoveCursorToCenter and return
        // this is basically doing our own CursorLockMode.Locked functionality for multiplayer cursor use.
        if (!CursorInstance.gameObject.activeInHierarchy && realMouseClamped)
        {
            MoveCursorToCenter();
            
            return;
        }
        
        //if player in on keyboard
        if (pioPlayerInput.currentControlScheme == KeyboardScheme)
        {
            //clamping mouse pos based on desired player cursor space (player portion or whole screen)
            Vector2 clampedMousePos = cursorConstrained
                ? ClampedByBounds(pointPos, playerScreenArea[0], playerScreenArea[1])
                : ClampedByBounds(pointPos, mainScreenArea[0], mainScreenArea[1]);
            
            //updating cursor position
            AnchorCursorImage(clampedMousePos);
            
            if (realMouseClamped)
            {
                // did real mouse go too far?
                bool mouseOffBounds = pointPos != clampedMousePos;
                
                if (mouseOffBounds)
                {
                    // // how far is it off
                    // Vector2 diff = clampedMousePos - pointPos;
                    //
                    // // bring it back that much plus a little extra to avoid getting stuck
                    // Vector2 warpedCursorPos = clampedMousePos + diff.normalized * 10;
                    
                    Vector2 warpedCursorPos = clampedMousePos;
                    
                    //matching mouse to cursor
                    _mouse.WarpCursorPosition(warpedCursorPos);
                    
                    pointPos = warpedCursorPos;
                }
            }
        }
        //if player is on gamepad
        else if (pioPlayerInput.currentControlScheme == GamepadScheme)
        {
            // cannot proceed if mouse is null
            if (_mouse == null)
            {
                Debug.LogError("PlayerCursor: Mouse is null while on GamepadScheme. Disabling PlayerCursor.");
                
                enabled = false;
                
                return;
            }
            
            //reading current virtual mouse position
            Vector2 gamepadMousePos = _mouse.position.ReadValue();
            
            // need to add a float modification to the gamepad movement, it needs to factor in the
            // size of the bounds so that it moves the same speed if fullscreen or part of the screen
            float boundsSizeModifier = cursorConstrained ? (playerScreenArea[1] - playerScreenArea[0]).magnitude / mainScreenArea[1].magnitude : 1f;
            
            //prepping value to store for target position
            Vector2 targetGpMousePos = gamepadMousePos + virtualPointDir * currentCursorSensitivity * boundsSizeModifier * Time.unscaledDeltaTime;
            
            //clamping new pos based on desired player cursor space (player portion or whole screen)
            Vector2 clampedTargetGpPos;
            
            if (cursorConstrained)
            {
                clampedTargetGpPos = ClampedByBounds(targetGpMousePos, playerScreenArea[0], playerScreenArea[1]);
            }
            else
            {
                clampedTargetGpPos = ClampedByBounds(targetGpMousePos, mainScreenArea[0], mainScreenArea[1]);
            }
            
            //updating virtual mouse position
            InputState.Change(_mouse.position, clampedTargetGpPos);
            
            // calculating delta for potential use
            Vector2 delta = clampedTargetGpPos - gamepadMousePos;
            
            //using delta to update virtual mouse delta
            InputState.Change(_mouse.delta, delta);
            
            //updating cursor position
            AnchorCursorImage(clampedTargetGpPos);
            
            //handling gamepad click state
            _mouse.CopyState<MouseState>(out var gamepadMouseState);
            
            //setting left button state based on cursorPressed
            gamepadMouseState = cursorPressed
                ? gamepadMouseState.WithButton(MouseButton.Left)
                : gamepadMouseState.WithButton(MouseButton.Left, false);
            
            //applying state change
            InputState.Change(_mouse, gamepadMouseState);
        }
        //touch
        else if (pioPlayerInput.currentControlScheme == TouchScheme)
        {
            // For now, treat touch the same as gamepad for cursor movement, but with no click since touch is inherently click based and would be weird to have both touch and click at same time.
            
            // cannot proceed if mouse is null
            if (_mouse == null)
            {
                Debug.LogError("PlayerCursor: Mouse is null while on TouchScheme. Disabling PlayerCursor.");
                
                enabled = false;
                
                return;
            }
            
            //reading current virtual mouse position
            Vector2 clampedTouchPos;
            
            if (cursorConstrained)
            {
                clampedTouchPos = ClampedByBounds(pointPos, playerScreenArea[0], playerScreenArea[1]);
            }
            else
            {
                clampedTouchPos = ClampedByBounds(pointPos, mainScreenArea[0], mainScreenArea[1]);
            }
            
             //updating virtual mouse position
            InputState.Change(_mouse.position, clampedTouchPos);
            
            // calculating delta for potential use
            Vector2 delta = clampedTouchPos - _mouse.position.ReadValue();
            
            //using delta to update virtual mouse delta
            InputState.Change(_mouse.delta, delta);
            
            //updating cursor position
            AnchorCursorImage(clampedTouchPos);

            // commented out click, as it registers from Touch map, and this double calls on the virtual cursor
            
            // //handling gamepad click state
            // _mouse.CopyState<MouseState>(out var gamepadMouseState);
            //
            // //setting left button state based on cursorPressed
            // gamepadMouseState = cursorPressed
            //     ? gamepadMouseState.WithButton(MouseButton.Left)
            //     : gamepadMouseState.WithButton(MouseButton.Left, false);
            //
            // //applying state change
            // InputState.Change(_mouse, gamepadMouseState);
        }
        // unhandled control scheme
        else
        {
            Debug.LogError("PlayerCursor: Unhandled control scheme: " + pioPlayerInput.currentControlScheme);
        }
    }
}
    