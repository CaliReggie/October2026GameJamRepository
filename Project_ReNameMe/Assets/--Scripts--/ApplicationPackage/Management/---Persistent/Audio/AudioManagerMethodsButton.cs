using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AudioManagerMethodsButton : MonoBehaviour
{
    [Header("Inscribed References")]
    
    [SerializeField] private AudioClip sfxClip;
    
    [SerializeField] [Range(0,1)] private float sfxVolume = 1f;
    
    [Tooltip("The optional position to play the audio at when called." +
             " If left null, will play at default position (AudioManager's position)")]
    [SerializeField] private Transform optionalPositionTarget;
    
    [Header("Dynamic References - Don't Modify In Inspector")]
    
    [SerializeField] private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }
    
    private void OnEnable()
    {
        button.onClick.AddListener(PlayButtonClickSfx);
    }
    
    private void OnDisable()
    {
        button.onClick.RemoveListener(PlayButtonClickSfx);
    }
    
    private void PlayButtonClickSfx()
    {
        if (sfxClip == null)
        {
            return;
        }
        
        AudioManager.Instance.PlaySfx(sfxClip, sfxVolume, optionalPositionTarget ? optionalPositionTarget.position : default);
    }
}
