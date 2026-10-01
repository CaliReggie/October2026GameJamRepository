using System;
using UnityEngine;

/// <summary>
/// Helps to force a found Camera to a desired aspect ratio and orientation via letterboxing.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ForceCameraAspectRatio : MonoBehaviour
{
    [Header("Inscribed Settings")]
    
    [Tooltip("The target aspect ratio for a found Camera on this GameObject. Used to force the Camera to this ratio.")]
    [SerializeField] private float targetAspect = 16f / 9f;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [Tooltip("The found Camera on the GameObject this script is attached to")]
    [SerializeField] private Camera gameObjectCamera;
    
    [Tooltip("The most recently updated Screen width. Used to detect changes in screen width and update the viewport accordingly.")]
    [SerializeField] private  int targetScreenWidth;
    
    [Tooltip("The most recently updated Screen height. Used to detect changes in screen height and update the viewport accordingly.")]
    [SerializeField] private int targetScreenHeight;

    void Awake()
    {
        gameObjectCamera = GetComponent<Camera>();
    }

    
    void Start()
    {
        gameObjectCamera = GetComponent<Camera>();
        
        UpdateViewport();
    }
    
    void Update()
    {
        if (Screen.width != targetScreenWidth || Screen.height != targetScreenHeight)
        {
            UpdateViewport();
        }
    }
    
    private void UpdateViewport()
    {
        targetScreenWidth = Screen.width;
        targetScreenHeight = Screen.height;

        try
        {
            if (ApplicationManager.Instance != null)
            {
                targetAspect = ApplicationManager.Instance.ActiveSceneSettings.TargetAspectRatio;
            }
        }
        catch (Exception)
        {
            // do nada, just use the inscribed settings
        }
        
        Rect targetRect = gameObjectCamera.rect;
        
        float windowAspect = (float)Screen.width / Screen.height;

        if (windowAspect < targetAspect)
        {
            // LETTERBOX (top & bottom)

            float scale = windowAspect / targetAspect;

            targetRect.width = 1f;
            targetRect.height = scale;
            targetRect.x = 0;
            targetRect.y = (1f - scale) / 2f;
        }
        else
        {
            // PILLARBOX (left & right)

            float scale = targetAspect / windowAspect;

            targetRect.width = scale;
            targetRect.height = 1f;
            targetRect.x = (1f - scale) / 2f;
            targetRect.y = 0;
        }

        gameObjectCamera.rect = targetRect;
    }
}
