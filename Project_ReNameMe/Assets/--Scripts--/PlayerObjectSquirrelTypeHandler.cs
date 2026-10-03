using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable] public class PlayerObjectSpecificData
{
    public GameObject modelPrefab;
    
    public Transform cameraModelLocation;
    
    public Transform modelParentLocation;
    
    public float colliderRadius;
    
    public float colliderHeight;

    public float mass;
    
    public float moveSpeed;
    
    public float jumpHeight;
    
    [Range(0,1)] public float playerRotationEasing = 0.25f;

    public float moveLerpGrounded = 1f;
    
    public float moveLerpAirborne = 0.2f;
}

public class PlayerObjectSquirrelTypeHandler : MonoBehaviour
{
    [Header("Inscribed")]
    
    [SerializeField] private Transform cameraPositionGameObject;
    
    [SerializeField] private Transform modelParentPositionGameObject;
    
    [SerializeField] private PlayerObjectSpecificData smallSquirrelData;
    
    [SerializeField] private PlayerObjectSpecificData bigSquirrelData;
    
    [Header("Dynamic")]
    
    [SerializeField] private PlayerObjectPioComponent playerObjectPioComponent;

    [SerializeField] private GameObject playerModel;
    
    private void Start()
    {
        playerObjectPioComponent = GetComponent<PlayerObjectPioComponent>();
        
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged += UpdateSquirrelType;
            
            UpdateSquirrelType();
        }
    }
    
    private void OnDestroy() 
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCharacterAssignmentChanged -= UpdateSquirrelType;
        }
    }

    private void UpdateSquirrelType()
    {
        try
        {
            int playerVisualIndex = GetComponentInParent<PlayerInputObject>().VisualIndex;
            
            if (playerVisualIndex == PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex)
            {
                UpdatePlayerObjectData(smallSquirrelData);
            }
            else if (playerVisualIndex == PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex)
            {
                UpdatePlayerObjectData(bigSquirrelData);
            }
            else if (GameManager.Instance != null) // starting in game scene with no pre assignment from mainmenu
            {
                if (PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex == -1)
                {
                    PlayerManager.Instance.SmallSquirrelAssignedPlayerVisualIndex = playerVisualIndex;
                }
                else if (PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex == -1)
                {
                    PlayerManager.Instance.BigSquirrelAssignedPlayerVisualIndex = playerVisualIndex;
                }
                else
                {
                    Debug.LogError($"Player visual index {playerVisualIndex} does not correspond to any squirrel type." +
                                     $" Defaulting to small squirrel data.");
                    
                    UpdatePlayerObjectData(smallSquirrelData);
                }
            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating squirrel type: {e.Message}");
        }
    }
    
    private void UpdatePlayerObjectData(PlayerObjectSpecificData data)
    {
        if (playerModel != null)
        {
            Destroy(playerModel);
        }
        
        cameraPositionGameObject.position = data.cameraModelLocation.position;
        
        modelParentPositionGameObject.position = data.modelParentLocation.position;
        modelParentPositionGameObject.rotation = data.modelParentLocation.rotation;
        
        playerModel = Instantiate(data.modelPrefab, modelParentPositionGameObject.position, modelParentPositionGameObject.rotation, modelParentPositionGameObject);
        
        playerObjectPioComponent.RayCastRadius = data.colliderRadius;
        
        playerObjectPioComponent.ColliderRadius = data.colliderRadius;
        playerObjectPioComponent.ColliderHeight = data.colliderHeight;
        
        playerObjectPioComponent.RigidbodyMass = data.mass;
        
        playerObjectPioComponent.WalkSpeed = data.moveSpeed;
        
        playerObjectPioComponent.JumpHeight = data.jumpHeight;
        
        playerObjectPioComponent.PlayerRotationEasing = data.playerRotationEasing;
        
        playerObjectPioComponent.MoveLerpGrounded = data.moveLerpGrounded;
        playerObjectPioComponent.MoveLerpAirborne = data.moveLerpAirborne;
    }
}
