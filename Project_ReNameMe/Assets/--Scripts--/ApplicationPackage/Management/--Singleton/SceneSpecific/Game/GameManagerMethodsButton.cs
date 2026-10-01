using UnityEngine;
using UnityEngine.UI;

public class GameManagerMethodsButton : MonoBehaviour
{
    private enum EGameStateMethodCallerType
    {
        CallPlay,
        CallPause,
        CallGameWon,
        CallGameLost,
        CallResetGame
    }
    
    [Header("Inscribed References")]
    
    [SerializeField] private EGameStateMethodCallerType type;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [Tooltip("The button component on this GameObject.")]
    [SerializeField] private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
    
    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonPressed);
        }
    }
    
    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonPressed);
        }
    }
    
    private void OnButtonPressed()
    {
        switch (type)
        {
            case EGameStateMethodCallerType.CallPlay:
                CallPlay();
                break;
            case EGameStateMethodCallerType.CallPause:
                CallPause();
                break;
            case EGameStateMethodCallerType.CallGameWon:
                CallGameWon();
                break;
            case EGameStateMethodCallerType.CallGameLost:
                CallGameLost();
                break;
            case EGameStateMethodCallerType.CallResetGame:
                CallResetGame();
                break;
        }
    }
    
    private void CallPlay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Play();
        }
    }
    
    private void CallPause()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Pause();
        }
    }
    
    private void CallGameWon()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(true);
        }
    }
    
    private void CallGameLost()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(false);
        }
    }
    
    private void CallResetGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
    }
}
