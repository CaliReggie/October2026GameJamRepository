using UnityEngine;

public class AudioManager : PersistentSingleton<AudioManager>
{
    public enum EMusicType
    {
        None,
        MainMenu,
        InGame
    }
    
    [Header("Inscribed References")]
    
    [SerializeField] private AudioSource mainMenuMusicSource;
    
    [SerializeField] private AudioSource inGameMusicSource;
    
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Inscribed Settings")]
    
    [Tooltip("If true, this AudioManager will persist across scene loads," +
             " if not, will only exist in the current scene.")]
    [SerializeField] private bool bePersistent = true;
    
    [Header("Dynamic Settings - Don't Modify In Inspector")]
    
    [SerializeField] [Range(0f,1f)] private float masterVolume = 0.5f;
    
    [SerializeField] private float lastRawMusicVolumeScale = 1f;
    
    public void PlayMusic(EMusicType musicType, float volume = 1f)
    {
        switch (musicType)
        {
            case EMusicType.None:
                mainMenuMusicSource.Stop();
                inGameMusicSource.Stop();
                break;
            case EMusicType.MainMenu:
                inGameMusicSource.Stop();
                if (mainMenuMusicSource.clip == null)
                {
                    return;
                }
                mainMenuMusicSource.Play();
                if (volume >= 0f)
                {
                    mainMenuMusicSource.volume = Mathf.Clamp01(volume * masterVolume);
                }
                lastRawMusicVolumeScale = volume;
                break;
            case EMusicType.InGame:
                mainMenuMusicSource.Stop();
                if (inGameMusicSource.clip == null)
                {
                    return;
                }
                inGameMusicSource.Play();
                if (volume >= 0f)
                {
                    inGameMusicSource.volume = Mathf.Clamp01(volume * masterVolume);
                }
                lastRawMusicVolumeScale = volume;
                break;
        }
    }
    
    public void PlaySfx(AudioClip clip, float volume = 1f, Vector3 location = default)
    {
        if (clip == null)
        {
            Debug.Log("SoundManager: PlaySfx called with null clip");
            return;
        }
        else if (sfxSource == null)
        {
            Debug.LogWarning("SoundManager: PlaySfx called with null audioSource");
        }
        else
        {
            sfxSource.transform.position = location;
            
            volume = Mathf.Clamp01(volume * masterVolume);
            
            sfxSource.PlayOneShot(clip, volume);
        }
    }
    
    public void UpdateSoundSettings(string accountName)
    {
        masterVolume = PlayerPrefsManager.TryGetAccountMasterVolume(accountName);
        
        // update music volume based on new master volume and last raw music volume scale
        if (mainMenuMusicSource.isPlaying)
        {
            mainMenuMusicSource.volume = Mathf.Clamp01(lastRawMusicVolumeScale * masterVolume);
        }
        else if (inGameMusicSource.isPlaying)
        {
            inGameMusicSource.volume = Mathf.Clamp01(lastRawMusicVolumeScale * masterVolume);
        }
    }
    
    protected override void Awake()
    {
        //check inscribed references
        if (mainMenuMusicSource == null ||
            inGameMusicSource == null ||
            sfxSource == null)
        {
            Debug.LogError("SoundManager: Error Checking Inscribed References. Destroying SoundManager.");
            
            Destroy(gameObject);
            
            return;
        }
        
        if (bePersistent)
        {
            base.Awake();
        }
        else // non-persistent singleton behavior, destroy new instances and keep the original
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                
                return;
            }
            
            Instance = this;
        }
    }

    private void Start()
    {
        PlayMusic(EMusicType.MainMenu);
    }
}
