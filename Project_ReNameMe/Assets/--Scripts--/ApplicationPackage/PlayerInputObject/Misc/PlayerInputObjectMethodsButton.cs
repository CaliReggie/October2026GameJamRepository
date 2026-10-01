using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayerInputObjectMethodsButton : MonoBehaviour
{
    private enum EPlayerManagementButtonType
    {
        [Tooltip("Alternate between player settings configuration, (ex: usually the toggle between unpaused / paused.")]
        AlternateState
    }
    
    [Header("Inscribed References")]
    
    [SerializeField] private EPlayerManagementButtonType buttonBehaviour = EPlayerManagementButtonType.AlternateState;
    
    
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
        switch (buttonBehaviour)
        {
            case EPlayerManagementButtonType.AlternateState:
                
                try
                {
                    GetComponentInParent<PlayerInputObject>().TogglePlayerSettingsConfigurationType();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error alternating PlayerInputObject state: {e.Message}.");
                }
                
                break;
        }
    }
}
