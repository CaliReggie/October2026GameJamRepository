using System;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// The PlayerObjectPioComponent extends PioComponent to manage the player object GameObject. Includes a basic
/// movement framework, but could be changed or extended for different use cases. Includes
/// logic to implement animations on the player object based on movement state.
/// </summary>
public class PlayerObjectPioComponent : PioComponent
{
    /// <summary>
    /// The various states the player object can be in based on player input and object conditions.
    /// </summary>
    public enum EPlayerObjectState
    {
        Inactive,
        Idle,
        Walking,
        Jumping,
        Falling
    }
    
    [Header("Inscribed References")]
    
    [Tooltip("The Animator component attached to the player object for state based animations.")]
    [SerializeField] private Animator animator;
    
    [Tooltip("The transform of the player object to be controlled by this component.")]
    [SerializeField] private Transform playerObject;
    
    [Tooltip("The Rigidbody of the playerObject, used for movement and physics interactions.")]
    [SerializeField] private Rigidbody playerObjectRigidbody;
    

    [Tooltip("The Collider of the playerObject, used for physics interactions and raycast detection.")]
    [SerializeField] private CapsuleCollider playerObjectCollider;

    [Tooltip("The physics layers to count as grounded for the player object when checking if grounded with raycasts.")]
    [SerializeField] private LayerMask groundedLayers;
    
    [SerializeField]
    private float raycastYOffset = 0.01f; // todo; this and below good change for base?
    
    [Tooltip("The radius of the spherecast to check for grounded.")]
    [SerializeField] private float raycastRadius = 0.1f;
    
    [Tooltip("The extra distance from the bottom of the player object collider that the raycast will check for grounded.")]
    [SerializeField] private float raycastExtraDistance = 0.01f;
    
    private readonly int idleAnimatorHash = Animator.StringToHash("isIdle");
    
    private readonly int walkingAnimatorHash = Animator.StringToHash("isWalking");
    
    private readonly int jumpingAnimatorHash = Animator.StringToHash("isJumping");
    
    private readonly int fallingAnimatorHash = Animator.StringToHash("isFalling");
    
    private readonly int groundedAnimatorHash = Animator.StringToHash("isGrounded");
    
    private readonly int lockedInPlaceAnimatorHash = Animator.StringToHash("isLockedInPlace");
    
    private readonly int flippedAnimatorHash = Animator.StringToHash("flipped");
    
    [Header("Inscribed Settings")]
    
    [Tooltip("The speed and which player object will move when move controls are used.")]
    [SerializeField] private float walkSpeed = 4f;
    
    //ADDED FOR SQUIRREL GAME. 
    [SerializeField] private float moveLerpGrounded = 1f;
    
    [SerializeField] private float moveLerpAirborne = 0.1f;
    
    [Tooltip("The real height the player will jump dependant on Physics gravity settings.")]
    [SerializeField] private float jumpHeight = 1.5f;
    
    [Tooltip("The time after pressing jump that playerObject will still attempt to jump (for jump forgiveness in air)")]
    [SerializeField] private float jumpBufferDuration = 0.2f;
    
    [Tooltip("The time after exiting grounded that playerObject will still attempt to jump (for coyote time)")]
    [SerializeField] private float jumpCoyoteBufferDuration = 0.2f;
    
    [Tooltip("The speed at which the player object will rotate towards target move when in a non-fixed player camera.")]
    [SerializeField] [Range(0,1)] private float playerRotationEasing = 0.25f;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [Tooltip("The references to this Pio's PlayerCameraPioComponent.")]
    [SerializeField] private PlayerCameraPioComponent playerCameraComponent;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("The target spawn position for the player object.")]
    [SerializeField] private Vector3 targetSpawnPosition;

    [Tooltip("The target spawn euler rotation for the player object.")]
    [SerializeField] private Vector3 targetSpawnEulerRotation;
    
    [Tooltip("The raw move input received from players input regardless of view orientation.")]
    [SerializeField] private Vector3 rawMoveInput;
    
    [Tooltip("The move input oriented to the current CurrentLookOrientation.")]
    [SerializeField] private Vector3 orientedMoveInput;
    
    [Tooltip("The target world space move vector the player object will move towards.")]
    [SerializeField] private Vector3 targetMove;
    
    [Tooltip("The target euler rotation the player object will rotate towards.")]
    [SerializeField] private Vector3 targetEulerRotation;
    
    [Tooltip("The current state of the player object based on input/player conditions.")]
    [SerializeField] private EPlayerObjectState currentState;

    [Tooltip("Is the player object currently grounded according to Collider.")]
    [SerializeField] private bool isGrounded;

    [Tooltip("Was the player object grounded last frame according to Collider.")]
    [SerializeField] private bool wasGrounded;
    
    [Tooltip("The time left that the player will still attempt to jump.")]
    [SerializeField] private float jumpBufferTimer;
    
    [Tooltip("The time left that the player can still attempt to jump if not grounded.")]
    [SerializeField] private float jumpCoyoteBufferTimer;
    
    //ADDED FOR SQUIRREL GAME
    [SerializeField] private float lockedInPlaceDuration = 0.35f;

    [SerializeField] private float lockedInPlaceTimer;
    
    [SerializeField] private bool isLockedInPlace;

    [SerializeField] private bool wasLockedInPlace;
    
    [SerializeField] private float maximumLockedInPlaceInteractionAngle = 75f;

    [SerializeField] private float lockedInPlaceBounceHeight = 3f;
    
    [field: SerializeField] public int BaseHealth { get; set; } = 1;

    [SerializeField] private int currentHealth;
    
    [SerializeField] private float healthRegenTickDuration = 7f;
    
    [SerializeField] private float incapacitatedDuration = 28f;
    
    [SerializeField] private float healthRegenTickTimer;

    [SerializeField] private GameObject incapacitatedEffectGameObject;
    
    [SerializeField] private Slider healthSlider;

    [SerializeField] private Interactor interactor;

    public bool IsIncapacitated => currentHealth <= 0;
    
    public Animator Animator { get => animator; set => animator = value; }
    
    public float RayCastRadius
    {
        get => raycastRadius;
        set => raycastRadius = value;
    }
    
    public float ColliderRadius
    {
        get
        {
            if (playerObjectCollider == null)
            {
                Debug.LogError($"{GetType().Name}: playerObjectCollider is null.");
            }
            return playerObjectCollider != null ? playerObjectCollider.radius : 1f;
        }
        set
        {
            if (playerObjectCollider == null)
            {
                Debug.LogError($"{GetType().Name}: playerObjectCollider is null.");
                return;
            }
            playerObjectCollider.radius = value;
        }
    }
    
    public float ColliderHeight
    {
        get
        {
            if (playerObjectCollider == null)
            {
                Debug.LogError($"{GetType().Name}: playerObjectCollider is null.");
            }
            return playerObjectCollider != null ? playerObjectCollider.height : 1f;
        }
        set
        {
            if (playerObjectCollider == null)
            {
                Debug.LogError($"{GetType().Name}: playerObjectCollider is null.");
                return;
            }
            playerObjectCollider.height = value;
        }
    }
    
    public float RigidbodyMass
    {
        get
        {
            if (playerObjectRigidbody == null)
            {
                Debug.LogError($"{GetType().Name}: playerObjectRigidbody is null.");
            }
            return playerObjectRigidbody != null ? playerObjectRigidbody.mass : 1f;
        }
        set
        {
            if (playerObjectRigidbody == null)
            {
                Debug.LogError($"{GetType().Name}: playerObjectRigidbody is null.");
                return;
            }
            playerObjectRigidbody.mass = value;
        }
    }
    
    public float WalkSpeed { get => walkSpeed; set => walkSpeed = value; }
    
    public float MoveLerpGrounded { get => moveLerpGrounded; set => moveLerpGrounded = value; }
    
    public float MoveLerpAirborne { get => moveLerpAirborne; set => moveLerpAirborne = value; }
    
    public float JumpHeight { get => jumpHeight; set => jumpHeight = value; }
    
    public float PlayerRotationEasing { get => playerRotationEasing; set => playerRotationEasing = value; }
    
    /// <summary>
    /// True if grounded and jump requested within jump buffer time,
    /// or if not grounded but jump requested within jump buffer time and
    /// jump coyote time is still active.
    /// </summary>
    private bool JumpRequested => isGrounded ? jumpBufferTimer > 0f : 
        (jumpBufferTimer > 0f && jumpCoyoteBufferTimer > 0f);
    
    /// <summary>
    /// The look orientation transform of current camera view per dynamically set playerCameraComponent,
    /// if it exists, otherwise null. Used to orient move input and player object rotation based on camera view direction.
    /// </summary>
    private Transform CurrentLookOrientation => playerCameraComponent != null ? playerCameraComponent.CurrentLookOrientation : null;
    
    /// <summary>
    /// The current world position of the player object.
    /// </summary>
    public Vector3 ObjectPosition => playerObject.position;
    
    /// <summary>
    /// The current euler rotation of the player object.
    /// </summary>
    public Vector3 ObjectEulerRotation => playerObject.rotation.eulerAngles;
    
    /// <summary>
    /// Message to be received from players clone of InputActions when move input is performed.
    /// </summary>
    public void OnMove(InputValue value)

    {
        Vector2 inputValue = value.Get<Vector2>();

        
        rawMoveInput = new Vector3(inputValue.x, 0f, inputValue.y);
    }

    /// <summary>
    /// Message to be received from players clone of InputActions when jump button is pressed.
    /// </summary>
    public void OnJump(InputValue buttonValue)
    {
        if (buttonValue.isPressed)
        {
            jumpBufferTimer = jumpBufferDuration;
        }
    }
    
    public void OnLockInPlace(InputValue buttonValue)
    {
        if (buttonValue.isPressed)
        {
            isLockedInPlace = !isLockedInPlace;
        }
    }
    
    //ADDED FOR SQUIRREL GAME
    
    public void GetHitForDamage(int damageAmount)
    {
        int previousHealth = currentHealth;
        
        currentHealth -= damageAmount;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            
            if (previousHealth > 0)
            {
                // player just became incapacitated
                healthRegenTickTimer = incapacitatedDuration;
                
                incapacitatedEffectGameObject.SetActive(true);

                PlayerManager.Instance.CheckGameOverLost();

                interactor.TryForceDropHeldInteractable();

                //todo: became incapacitated stuff
            }
        }
        else if (currentHealth < BaseHealth)
        {
            // player is damaged but not incapacitated
            healthRegenTickTimer = healthRegenTickDuration;
            
            //todo: get hit stuff
        }
        
        if (healthSlider != null)
        {
            healthSlider.value = (float)currentHealth / BaseHealth;
        }
    }
    
    public void HealForHealth(int healAmount)
    {
        currentHealth += healAmount;
        
        if (currentHealth > BaseHealth)
        {
            currentHealth = BaseHealth;
        }
        
        if (currentHealth > 0)
        {
            incapacitatedEffectGameObject.SetActive(false);
        }
        
        if (currentHealth < BaseHealth)
        {
            healthRegenTickTimer = healthRegenTickDuration;
        }
        
        if (healthSlider != null)
        {
            healthSlider.value = (float)currentHealth / BaseHealth;
        }
    }
    
    /// <summary>
    /// Teleports the player object to the target location. Optionally also rotates to match target Euler rotation.
    /// </summary>
    public void TpPlayerObject(Vector3 targetTpPosition, Vector3 targetTpEulerRotation, bool alsoRotate, bool alsoResetDynamicSettings = false)
    {
        if (alsoResetDynamicSettings) // todo: Add this before in main skeleton?
        {
            ResetDynamicSettings();
        }
        
        // if the rigidbody is enabled and active we have to use its move method
        if (playerObject.gameObject.activeSelf)
        {
            playerObjectRigidbody.Move(targetTpPosition, alsoRotate ? Quaternion.Euler(targetTpEulerRotation) : playerObject.rotation);
        }
        // otherwise use transform directly
        else
        {
            playerObject.position = targetTpPosition;
            
            playerObject.SetPositionAndRotation(targetTpPosition, alsoRotate ? Quaternion.Euler(targetTpEulerRotation) : playerObject.rotation);
        }
        
        targetEulerRotation = targetTpEulerRotation; // todo: add this after in main skeleton?
    }
    
    protected override void Initialize()
    {
        // check inscribed references
        if (!CheckInscribedReferences()) return;

        //set dynamic references
        if (!SetDynamicReferences()) return;
        
        // ensure player starts despawned
        DeSpawn();
        
        Initialized = true;
        
        return;
        
        // check that all inscribed references are set
        bool CheckInscribedReferences()
        {
            if (playerObject == null ||
                animator == null ||
                playerObjectRigidbody == null ||
                playerObjectCollider == null)
            {
                Debug.LogError($"{GetType().Name}: Error checking inscribed references.");
                
                return false;
            }
            
            return true;
        }
        
        // set and check dynamic references
        bool SetDynamicReferences()
        {
            try
            {
                playerCameraComponent = Pio.GetComponent<PlayerCameraPioComponent>();
                
                if (playerCameraComponent == null)
                {
                    Debug.LogError($"{GetType().Name}: Error setting dynamic references.");
                    
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{GetType().Name}: Exception setting dynamic references: {e.Message}");
                
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
            case PlayerInputObject.EPlayerInputObjectState.SceneUi:
                
                // if on, deactivate
                if (enabled)
                {
                    DeSpawn();
                    
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
                
                // if off, activate
                if (!enabled)
                {
                    Spawn();

                    enabled = true;
                }
                
                break;
        }
    }

    protected override void OnAfterPioSettingsChange(PlayerSettingsSo playerSettings)
    {
        //
    }

    /// <summary>
    /// Spawns player at the current target spawn point (default if none set).
    /// </summary>
    private void Spawn()
    {
        // on spawn, getting most recent spawn point from player settings SO in case it was changed while despawned
        if (Pio.CurrentPlayerSettings != null)
        {
            if (Pio.CurrentPlayerSettings.UseEnvironmentSpawnPoint)
            {
                SceneEnvironment sceneEnvironment = FindAnyObjectByType<SceneEnvironment>();
                
                if (sceneEnvironment != null)
                {
                    targetSpawnPosition = sceneEnvironment.GetSpawnPoint(Pio.VisualIndex).position;
                    
                    targetSpawnEulerRotation = sceneEnvironment.GetSpawnPoint(Pio.VisualIndex).rotation.eulerAngles;
                }
                else
                {
                    Debug.LogError($"{GetType().Name}: No SceneEnvironment found in scene," +
                                   $" cannot use environment spawn point.");
                    
                    targetSpawnPosition = Pio.CurrentPlayerSettings.SpawnPosition;
            
                    targetSpawnEulerRotation = Pio.CurrentPlayerSettings.SpawnEulerRotation;
                }
            }
            else
            {
                targetSpawnPosition = Pio.CurrentPlayerSettings.SpawnPosition;
            
                targetSpawnEulerRotation = Pio.CurrentPlayerSettings.SpawnEulerRotation;
            }
        }
        
        // teleport to spawn point
        TpPlayerObject(targetSpawnPosition, targetSpawnEulerRotation, true, true);
        
        // turn on player object
        playerObject.gameObject.SetActive(true);
    }

    /// <summary>
    /// Resets dynamic movement settings and deactivates player object.
    /// </summary>
    private void DeSpawn()
    {
        // teleport to spawn point
        TpPlayerObject(targetSpawnPosition, targetSpawnEulerRotation, true, true);
        
        // turn off player object
        playerObject.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Used to reset all dynamic settings related to movement and state of the player object and go back to idle.
    /// DOES NOT reset object spawn position/rotation.
    /// </summary>
    private void ResetDynamicSettings()
    {
        rawMoveInput = Vector3.zero;
            
        orientedMoveInput = Vector3.zero;
            
        targetMove = Vector3.zero;
            
        targetEulerRotation = Vector3.zero;
            
        isGrounded = false;
            
        jumpBufferTimer = 0f;
        
        jumpCoyoteBufferTimer = 0f;
        
        // object rigidbody should be reset
        if (!playerObjectRigidbody.isKinematic)
        {
            playerObjectRigidbody.linearVelocity = Vector3.zero;
            playerObjectRigidbody.angularVelocity = Vector3.zero;
        }
        
        //ADDED FOR SQUIRREL GAME
        lockedInPlaceTimer = 0f;
        
        currentHealth = BaseHealth;

        healthRegenTickTimer = 0;
        
        if (healthSlider != null)
        {
            healthSlider.value = (float)currentHealth / BaseHealth;
        }
        
        animator.SetBool(idleAnimatorHash, false);
        animator.SetBool(walkingAnimatorHash, false);
        animator.SetBool(jumpingAnimatorHash, false);
        animator.SetBool(fallingAnimatorHash, false);
        animator.SetBool(groundedAnimatorHash, false);
        animator.SetBool(lockedInPlaceAnimatorHash, false);
        
        isLockedInPlace = false;
        
        incapacitatedEffectGameObject.SetActive(false);
        
        if (Pio.IsSmallSquirrel)
        {
            playerCameraComponent.SetLockedInPlaceCam(false);
        }
        
        //
        
        HandleChangeState(EPlayerObjectState.Idle);
    }
    
    private void FixedUpdate()
    {
        // cannot move if not initialized
        if (!Initialized) { return; }

        if (IsIncapacitated) { return; }
        
        ManageMove();

        ManageRotation();
        
        return;
        
        void ManageMove()
        {
            // playerObjectRigidbody.linearVelocity = targetMove;
            
            // jump can be requested with variable conditions so check that first
            if (JumpRequested)
            {
                // resetting jump buffers (also resets JumpRequested)
                jumpBufferTimer = 0f;
                jumpCoyoteBufferTimer = 0f;
                // use grav to calc jump height
                // targetMove.y = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y); 
                
                // CHANGED FOR SQUIRREL GAME. using force instead of setting velocity directly
                playerObjectRigidbody.AddForce(Vector3.up * Mathf.Sqrt(2f * jumpHeight
                    * -Physics.gravity.y), ForceMode.VelocityChange);
            }
            
            // CHANGED FOR SUIRREL GAME. using physics forces.
            Vector3 currentVelocity = playerObjectRigidbody.linearVelocity;
            
            Vector3 addedVelocity = new Vector3(targetMove.x - currentVelocity.x, 0f, targetMove.z - currentVelocity.z);
            
            Vector3 lerpedAdditionalVelocity = Vector3.Lerp(Vector3.zero, addedVelocity, isGrounded ? moveLerpGrounded : moveLerpAirborne);

            playerObjectRigidbody.AddForce(lerpedAdditionalVelocity, ForceMode.VelocityChange);
            
        }
            
        
        void ManageRotation()
        {
            // logic variable based on camera type and if player object should face move direction
            bool shouldPoFaceMoveDirection = 
                !isLockedInPlace ||
                (playerCameraComponent.CurrentCameraType == PlayerCameraPioComponent.EPlayerCameraType.PlayerThirdOrbit ||
                 playerCameraComponent.CurrentCameraType == PlayerCameraPioComponent.EPlayerCameraType.SceneCamera ||
                playerCameraComponent.CurrentCameraType == PlayerCameraPioComponent.EPlayerCameraType.PlayerFixed);

            // body faces move direction in certain camera types, otherwise faces look orientation.
            if (shouldPoFaceMoveDirection)
            {
                // don't rotate if no move input
                if (orientedMoveInput != Vector3.zero)
                {
                    // target rotation is move direction
                    Vector3 moveRotationEulerAngles = Quaternion.LookRotation(orientedMoveInput).eulerAngles;
                    
                    // //lerp to target rotation for smoothness
                    targetEulerRotation = new Vector3(0f, Mathf.LerpAngle(targetEulerRotation.y, moveRotationEulerAngles.y, playerRotationEasing), 0f);
                }
            }
            else
            {
                // face look orientation dir
                targetEulerRotation = Quaternion.LookRotation(CurrentLookOrientation.forward).eulerAngles;
                
                targetEulerRotation = new Vector3(0f, targetEulerRotation.y, 0f);
            }
            
            playerObjectRigidbody.MoveRotation(Quaternion.Euler(0f, targetEulerRotation.y, 0f));
        }
    }

    private void Update()
    {
        if (!Initialized) { return; }
        
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState.State != GameManager.EGameState.Playing)
        {
            return;
        }

        ManageCooldownsAndTimers();

        ManageGrounded();
        
        ManageLockedInPlace();

        ManageOrientedInput();

        ManageTargetMove();
        
        ManageState();
        
        return;
        
        void ManageCooldownsAndTimers()
        {
            if (healthRegenTickTimer > 0f)
            {
                healthRegenTickTimer -= Time.deltaTime;
                
                if (healthRegenTickTimer <= 0f && currentHealth < BaseHealth)
                {
                    HealForHealth(1);
                }
            }
            
            // counting down jump cooldowns and timers
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
            }
            
            if (jumpCoyoteBufferTimer > 0f)
            {
                jumpCoyoteBufferTimer -= Time.deltaTime;
            }
        }

        void ManageGrounded()
        {
            // raycasting down from bottom bounds of player object collider to check if grounded
            Vector3 rayOrigin = playerObjectCollider.bounds.center + Vector3.up * raycastYOffset;
            
            // float distToBottom = playerObjectCollider.bounds.extents.y;
            
            //CHANGED FOR SQUIRREL HORIZONTAL CAPSULE
            float distToBottom = playerObjectCollider.bounds.extents.x / 2;
            
            // grounded check, can do other checks for things like slope in future here
            // isGrounded = Physics.SphereCast(rayOrigin, raycastRadius, Vector3.down, out _, distToBottom + raycastExtraDistance, groundedLayers);
            
            //CHANGED FOR SQUIRREL HORIZONTAL CAPSULE..todo: good change for base?
            isGrounded = Physics.CapsuleCast (rayOrigin + Vector3.forward * playerObjectCollider.bounds.extents.z, rayOrigin - Vector3.forward * playerObjectCollider.bounds.extents.z, raycastRadius, Vector3.down, out _, distToBottom + raycastExtraDistance, groundedLayers);
            
            // debug raycast rays on bounds extents of player object collider // todo: comment
            Debug.DrawRay(rayOrigin + Vector3.forward * playerObjectCollider.bounds.extents.z, Vector3.down * (distToBottom + raycastExtraDistance), isGrounded ? Color.green : Color.red);
            Debug.DrawRay(rayOrigin - Vector3.forward * playerObjectCollider.bounds.extents.z, Vector3.down * (distToBottom + raycastExtraDistance), isGrounded ? Color.green : Color.red);
            Debug.DrawRay(rayOrigin + Vector3.right * playerObjectCollider.bounds.extents.x, Vector3.down * (distToBottom + raycastExtraDistance), isGrounded ? Color.green : Color.red);
            Debug.DrawRay(rayOrigin - Vector3.right * playerObjectCollider.bounds.extents.x , Vector3.down * (distToBottom + raycastExtraDistance), isGrounded ? Color.green : Color.red);
            
            if (debugMode)
            {
                Debug.DrawRay(rayOrigin, Vector3.down * (distToBottom + raycastExtraDistance), isGrounded ? Color.green : Color.red);
            }
            
            // if just left grounded, start coyote timer
            // if (!isGrounded && wasGrounded && targetMove.y <= 0f)
            // {
            //     jumpCoyoteBufferTimer = jumpCoyoteBufferDuration;
            // }
            
            //CHANGED FOR SQUIRREL GAME
            if (!isGrounded && wasGrounded && playerObjectRigidbody.linearVelocity.y <= 0f)
            {
                jumpCoyoteBufferTimer = jumpCoyoteBufferDuration;
            }
            
            wasGrounded = isGrounded;
        }
        
        void ManageLockedInPlace()
        {
            if (lockedInPlaceTimer > 0f)
            {
                lockedInPlaceTimer -= Time.deltaTime;
            }
            
            if (isLockedInPlace && !wasLockedInPlace)
            {
                lockedInPlaceTimer = lockedInPlaceDuration;
                
                if (Pio.IsBigSquirrel)
                {
                    animator.SetTrigger(flippedAnimatorHash);
                }
                else if (Pio.IsSmallSquirrel)
                {
                    playerCameraComponent.SetLockedInPlaceCam(true);
                }
            }
            else if (!isLockedInPlace && wasLockedInPlace)
            {
                lockedInPlaceTimer = 0f;
                
                if (Pio.IsSmallSquirrel)
                {
                    playerCameraComponent.SetLockedInPlaceCam(false);
                }
            }

            wasLockedInPlace = isLockedInPlace;
            
            if (lockedInPlaceTimer <= 0f && isLockedInPlace && rawMoveInput.magnitude > 0f)
            {
                isLockedInPlace = false;
            }
        }

        void ManageOrientedInput()
        {
            // oriented input is raw input relative to look orientation
            orientedMoveInput = CurrentLookOrientation.forward * rawMoveInput.z + CurrentLookOrientation.right * rawMoveInput.x;
        
            // don't allow vertical movement from move input
            orientedMoveInput.y = 0f;
        
            // normalize to prevent faster diagonal movement
            orientedMoveInput.Normalize();
        }

        void ManageTargetMove()
        {
            // lateral management
            targetMove.x = orientedMoveInput.x * walkSpeed;
            
            targetMove.z = orientedMoveInput.z * walkSpeed;
            
            // jump can be requested with variable conditions so check that first
            // if (JumpRequested)
            // {
            //     // resetting jump buffers (also resets JumpRequested)
            //     jumpBufferTimer = 0f;
            //     jumpCoyoteBufferTimer = 0f;
            //     // use grav to calc jump height
            //     // targetMove.y = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y); 
            //     
            //     // CHANGED FOR SQUIRREL GAME. using force instead of setting velocity directly
            //     playerObjectRigidbody.AddForce(Vector3.up * Mathf.Sqrt(2f * jumpHeight
            //         * -Physics.gravity.y), ForceMode.VelocityChange);
            // }
            // else if (isGrounded)
            // {
            //     if (targetMove.y <= 0f)
            //     {
            //         targetMove.y = 0f;
            //     }
            //     else // don't think needed / think is smoother ?
            //     {
            //         targetMove.y += Physics.gravity.y * Time.deltaTime;
            //     }
            // }
            // else
            // {
            //     targetMove.y += Physics.gravity.y * Time.deltaTime;
            // }
        }
        
        void ManageState()
        {
            EPlayerObjectState previousState = currentState;

            EPlayerObjectState targetState;
            
            animator.SetBool(groundedAnimatorHash, isGrounded);
        
            animator.SetBool(lockedInPlaceAnimatorHash, isLockedInPlace);
            
            if (isGrounded)
            {
                // if grounded and trying to move walkingAnimatorHash
                if (orientedMoveInput.magnitude > 0f && !IsIncapacitated)
                {
                    targetState = EPlayerObjectState.Walking;
                }
                // otherwise idle
                else
                {
                    targetState = EPlayerObjectState.Idle;
                }
            }
            else
            {
                // if not grounded and target move is up, jumping
                // if (targetMove.y > 0f)
                // {
                //     targetState = EPlayerObjectState.Jumping;
                // }
                
                //CHANGED FOR SQUIRREL GAME
                if (playerObjectRigidbody.linearVelocity.y > 0f)
                {
                    targetState = EPlayerObjectState.Jumping;
                }
                // otherwise must be falling
                else
                {
                    targetState = EPlayerObjectState.Falling;
                }
            }

            // change state if needed
            if (targetState != previousState)
            {
                HandleChangeState(targetState);
            }
        }
    }
    
    /// <summary>
    /// Handle logic switching from the current state to a new target state. Cannot switch to same state.
    /// Should not be used to force a state, only to handle logic when a state change is needed.
    /// </summary>
    private void HandleChangeState(EPlayerObjectState toState)
    {
        if (currentState == toState) { return; }
        
        if (debugMode)
        {
            Debug.Log($"{GetType().Name}: {Pio.gameObject.name} changing to state: {toState}");
        }
        
        EPlayerObjectState previousState = currentState;

        // logic for exiting these states
        switch (previousState)
        {
            case EPlayerObjectState.Inactive:
                break;
            case EPlayerObjectState.Idle:
                animator.SetBool(idleAnimatorHash, false);
                break;
            case EPlayerObjectState.Walking:
                animator.SetBool(walkingAnimatorHash, false);
                break;
            case EPlayerObjectState.Jumping:
                animator.SetBool(jumpingAnimatorHash, false);
                break;
            case EPlayerObjectState.Falling:
                animator.SetBool(fallingAnimatorHash, false);
                break;
        }
        
        // logic for entering these states
        switch (toState)
        {
            case EPlayerObjectState.Inactive:
                break;
            case EPlayerObjectState.Idle:
                animator.SetBool(idleAnimatorHash, true);
                break;
            case EPlayerObjectState.Walking:
                animator.SetBool(walkingAnimatorHash, true);
                break;
            case EPlayerObjectState.Jumping:
                animator.SetBool(jumpingAnimatorHash, true);
                break;
            case EPlayerObjectState.Falling:
                animator.SetBool(fallingAnimatorHash, true);
                break;
        }
        
        currentState = toState;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        //want to check for other player hit
        PlayerObjectPioComponent otherPlayerObjectPioComponent = collision.gameObject.GetComponent<PlayerObjectPioComponent>();
        
        if (otherPlayerObjectPioComponent == null 
            || !otherPlayerObjectPioComponent.isLockedInPlace
            || !IsAboveOtherPlayer(otherPlayerObjectPioComponent))
        {
            return;
        }
        
        // small checks for landing on top of big squirrel, while big squirrel is locked in place, and bounces off
        if (Pio.IsSmallSquirrel && otherPlayerObjectPioComponent.Pio.IsBigSquirrel)
        {
            playerObjectRigidbody.AddForce(Vector3.up * Mathf.Sqrt(2f * lockedInPlaceBounceHeight * -Physics.gravity.y), ForceMode.VelocityChange);

        }
        // big checks for landing on top of small squirrel, while small squirrel is locked in place, and tries
        // to make it shoot out a held item if it has one
        else if (Pio.IsBigSquirrel && otherPlayerObjectPioComponent.Pio.IsSmallSquirrel)
        {
            Interactor otherInteractor = otherPlayerObjectPioComponent.Pio.GetComponentInChildren<Interactor>();
            
            if (otherInteractor != null)
            {
                otherInteractor.TryShootRemoveHeldInteractable();
            }
            else
            {
                Debug.LogError($"{GetType().Name}: Other player object interactor is null.");
            }
        }

        return;
        
        bool IsAboveOtherPlayer(PlayerObjectPioComponent other)
        {
            Vector3 dirFromOther = (playerObject.position - other.playerObject.position).normalized;
            
            float angleToOther = Vector3.Angle(Vector3.up, dirFromOther);
            
            return angleToOther <= maximumLockedInPlaceInteractionAngle;
        }
    }
}