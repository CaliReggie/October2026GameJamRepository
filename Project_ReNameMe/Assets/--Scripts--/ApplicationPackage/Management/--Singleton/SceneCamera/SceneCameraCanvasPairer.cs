using UnityEngine;

/// <summary>
/// Put on a camera to automatically pair a found canvas on the same GameObject
/// with a found SceneCamera Camera component in scene. Updates on Application State changes if found.
/// </summary>
public class SceneCameraCanvasPairer : MonoBehaviour
{
    [Header("Inscribed Settings")]
    
    [Tooltip("The distance from the camera that the canvas will be rendered at." +
             " Should be slightly in front of the Cameras near clipping plane to avoid clipping issues.")]
    [SerializeField] private float planeDistance = 1.001f;

    [Header("Dynamic Settings - Don't Modify In Inspector")]

    [Tooltip("The Canvas component found on this GameObject.")]
    [SerializeField] private Canvas canvas;
    
    [Tooltip("The most recently updated SceneCamera instance found in the scene." +
             " Used to detect changes in the SceneCamera and update the canvas pairing accordingly.")]
    [SerializeField] private SceneCamera targetSceneCamera;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        
        PairCanvasWithSceneCamera();
        
        if (ApplicationManager.Instance != null)
        {
            ApplicationManager.Instance.OnBeforeStateChange += OnBeforeApplicationStateChange;
        }
    }

    private void OnDestroy()
    {
        if (ApplicationManager.Instance != null)
        {
            ApplicationManager.Instance.OnBeforeStateChange -= OnBeforeApplicationStateChange;
        }
    }


    private void PairCanvasWithSceneCamera()
    {
        SceneCamera foundSceneCamera = SceneCamera.Instance;
        
        if (foundSceneCamera == null && targetSceneCamera == null)
        {
            Debug.LogWarning("SceneCameraCanvasPairer: No SceneCamera instance found in the scene.");
            return;
        }
        
        if (foundSceneCamera == targetSceneCamera)
        {
            return;
        }
        
        targetSceneCamera = foundSceneCamera;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = targetSceneCamera.Camera;
        canvas.planeDistance = planeDistance;
    }
    
    private void OnBeforeApplicationStateChange(ApplicationManager.EApplicationState newState)
    {
        // going to call it good practice to simple always update on big changes... let's see?
        if (ApplicationManager.Instance.Started)
        {
            PairCanvasWithSceneCamera();
        }
    }
}
