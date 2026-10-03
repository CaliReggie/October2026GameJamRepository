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
}

public class PlayerObjectSquirrelTypeHandler : MonoBehaviour
{
    [Header("Inscribed")]
    
    [SerializeField] private Transform cameraPositionGameObject;
    
    [SerializeField] private Transform modelParentPositionGameObject;
    
    [SerializeField] private PlayerObjectSpecificData smallSquirrelData;
    
    [SerializeField] private PlayerObjectSpecificData bigSquirrelData;
    
    [Header("Dynamic")]
    
    [SerializeField] private CapsuleCollider playerCollider;
    
    [SerializeField] private Rigidbody playerRigidbody;

    [SerializeField] private GameObject playerModel;
    
    private void Start()
    {
        PlayerManager.Instance.OnSquirrelCorrespondingPlayerVisualIndexesChanged += UpdateSquirrelType;
        
        playerCollider = GetComponent<CapsuleCollider>();
        playerRigidbody = GetComponent<Rigidbody>();
        
        UpdateSquirrelType();
    }
    
    private void OnDestroy() 
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnSquirrelCorrespondingPlayerVisualIndexesChanged -= UpdateSquirrelType;
        }
    }

    private void UpdateSquirrelType()
    {
        try
        {
            int playerVisualIndex = GetComponentInParent<PlayerInputObject>().VisualIndex;
            
            if (playerVisualIndex == PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex)
            {
                UpdatePlayerObjectData(smallSquirrelData);
            }
            else if (playerVisualIndex == PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex)
            {
                UpdatePlayerObjectData(bigSquirrelData);
            }
            else if (PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex == -1)
            {
                PlayerManager.Instance.SmallSquirrelCorrespondingPlayerVisualIndex = playerVisualIndex;
            }
            else if (PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex == -1)
            {
                PlayerManager.Instance.BigSquirrelCorrespondingPlayerVisualIndex = playerVisualIndex;
            }
            else
            {
                Debug.LogError($"Player visual index {playerVisualIndex} does not correspond to any squirrel type." +
                                 $" Defaulting to small squirrel data.");
                
                UpdatePlayerObjectData(smallSquirrelData);
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
        
        playerCollider.radius = data.colliderRadius;
        playerCollider.height = data.colliderHeight;
        
        playerRigidbody.mass = data.mass;
    }
}
