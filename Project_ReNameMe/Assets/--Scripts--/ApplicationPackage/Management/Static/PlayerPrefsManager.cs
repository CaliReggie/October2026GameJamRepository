using UnityEngine;

public static class PlayerPrefsManager
{
    #region Account Settings

    #region Player Number and Account

    private const string AccountKeyPrefix = "Account_";
    
    private static string GetAccountKey(string account) => AccountKeyPrefix + account;
    
    private static void SaveAccount(string account)
    {
        string storedAccount = TryGetAccount(account);
        
        if (!string.IsNullOrEmpty(storedAccount))
        {
            return; // account already exists, no need to save again
        }
        else
        {
            // setting up an account for first time, do things?
        }
        
        PlayerPrefs.SetString(GetAccountKey(account), account);
        PlayerPrefs.Save();
    }
    
    private static string TryGetAccount(string account)
    {
        return PlayerPrefs.HasKey(GetAccountKey(account))
            ? PlayerPrefs.GetString(GetAccountKey(account))
            : null;
    }
    
    public static void DeleteAccount(string account)
    {
        PlayerPrefs.DeleteKey(GetAccountKey(account));
        PlayerPrefs.Save();
    }
    
    private const string PlayerAccountKeyPrefix = "PlayerAccount_";
    
    private static string GetPlayerAccountKey(int playerId) => PlayerAccountKeyPrefix + playerId;
    
    public static void SetPlayerAccount(int playerId, string account)
    {
        // can't set null or empty account
        if (playerId < 1 || string.IsNullOrEmpty(account)) { return; }
        
        // can't set an account for an already occupied player number
        for (int i = 1; i <= PlayerManager.MaxPlayers; i++)
        {
            int otherPlayerId = i;
            
            string otherAccount = TryGetAccountFromPlayerNumber(otherPlayerId);
            
            if (!string.IsNullOrEmpty(otherAccount) && otherAccount == account && otherPlayerId != playerId)
            {
                Debug.LogWarning($"Account '{account}' is already assigned to player number {i}." +
                                 $" Cannot assign to player number {playerId}.");
                return;
            }
        }
        
        // need to make sure exists
        SaveAccount(account);
            
        string savedAccount = TryGetAccount(account);
        
        PlayerPrefs.SetString(GetPlayerAccountKey(playerId), savedAccount);
        
        PlayerPrefs.Save();
    }
    
    public static string TryGetAccountFromPlayerNumber(int playerId)
    {
        return PlayerPrefs.HasKey(GetPlayerAccountKey(playerId))
            ? PlayerPrefs.GetString(GetPlayerAccountKey(playerId))
            : null;
    }
    
    public static int TryGetPlayerNumberFromAccount(string account)
    {
        for (int i = 1; i <= PlayerManager.MaxPlayers; i++)
        {
            string playerAccount = TryGetAccountFromPlayerNumber(i);
            
            if (!string.IsNullOrEmpty(playerAccount) && playerAccount == account)
            {
                return i;
            }
        }
        
        return -1; // account not assigned to any player number
    }
    
    public static bool IsPlayerAccountInUse(string account)
    {
        for (int i = 1; i <= PlayerManager.MaxPlayers; i++)
        {
            string playerAccount = TryGetAccountFromPlayerNumber(i);
            
            if (!string.IsNullOrEmpty(playerAccount) && playerAccount == account)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public static void ClearPlayerAccountFromNumber(int playerId)
    {
        PlayerPrefs.DeleteKey(GetPlayerAccountKey(playerId));
        PlayerPrefs.Save();
    }
    
    public static void ClearPlayerAccount(string account)
    {
        int playerId = TryGetPlayerNumberFromAccount(account);
        
        if (playerId != -1)
        {
            ClearPlayerAccountFromNumber(playerId);
        }
    }
    
    public static void ClearAllPlayerAccounts()
    {
        for (int i = 1; i <= PlayerManager.MaxPlayers; i++)
        {
            ClearPlayerAccountFromNumber(i);
        }
    }

    #endregion

    #region Sensitivity Settings

    private const string CameraSensitivityKeyPrefix = "CameraSensitivity_";
    
    public static void SetAccountCameraSensitivity(string account, float sensitivity)
    {
        SaveAccount(account);
        
        string savedAccount = TryGetAccount(account);
        
        string key = CameraSensitivityKeyPrefix + savedAccount;
        
        sensitivity = Mathf.Clamp(sensitivity, 0.01f, 99.99f);
        
        PlayerPrefs.SetFloat(key, sensitivity);
        PlayerPrefs.Save();
    }
    
    public static float TryGetAccountCameraSensitivity(string account)
    {
        string savedAccount = TryGetAccount(account);
        
        if (savedAccount == null)
        {
            return 1f; // account does not exist, return base sensitivity
        }
        
        string key = CameraSensitivityKeyPrefix + savedAccount;
        
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : 1f; // return base if no sensitivity set for account
    }
    
    private const string CursorSensitivityKeyPrefix = "CursorSensitivity_";
    
    public static void SetAccountCursorSensitivity(string account, float sensitivity)
    {
        SaveAccount(account);
        
        string savedAccount = TryGetAccount(account);
        
        string key = CursorSensitivityKeyPrefix + savedAccount;
        
        // cannot be less than 0.01
        sensitivity = Mathf.Clamp(sensitivity, 0.01f, 9.99f);
        
        PlayerPrefs.SetFloat(key, sensitivity);
        PlayerPrefs.Save();
    }
    
    public static float TryGetAccountCursorSensitivity(string account)
    {
        string savedAccount = TryGetAccount(account);
        
        if (savedAccount == null)
        {
            return 1f; // account does not exist, return base sensitivity
        }
        
        string key = CursorSensitivityKeyPrefix + savedAccount;
        
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : 1f; // return base if no sensitivity set for account
    }

    #endregion

    #region AudioSettings

    private const string MasterVolumePrefix = "MasterVolume_";
    
    public static void SetAccountMasterVolume(string account, float volume)
    {
        SaveAccount(account);
        
        string savedAccount = TryGetAccount(account);
        
        string key = MasterVolumePrefix + savedAccount;
        
        // clamp between 0 and 1
        volume = Mathf.Clamp01(volume);
        
        PlayerPrefs.SetFloat(key, volume);
        PlayerPrefs.Save();
    }
    
    public static float TryGetAccountMasterVolume(string account)
    {
        string savedAccount = TryGetAccount(account);
        
        if (savedAccount == null)
        {
            return 0.5f; // account does not exist, return base volume
        }
        
        string key = MasterVolumePrefix + savedAccount;
        
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : 0.5f; // return base if no volume set for account
    }

    #endregion
   
    #endregion
    
    #region Level Score / Availability Settings // todo: update to support player accounts?
    
    // todo: comment / summary
    
    private const string TargetScorePrefix = "LevelTargetScore_";
    
    private static string GetTargetScoreKey(int levelIndex) => TargetScorePrefix + levelIndex;
    
    public static int TryGetSceneTargetScore(int chronologicalId)
    {
        return PlayerPrefs.GetInt(GetTargetScoreKey(chronologicalId), 0);
    }
    
    public static void TrySetSceneTargetScore(int chronologicalId, int targetScore)
    {
        PlayerPrefs.SetInt(GetTargetScoreKey(chronologicalId), targetScore);
        PlayerPrefs.Save();
    }
    
    public static void ResetSceneTargetScore(int chronologicalId)
    {
        PlayerPrefs.DeleteKey(GetTargetScoreKey(chronologicalId));
        PlayerPrefs.Save();
    }
    
    private const string BestScorePrefix = "LevelBestScore_";
    
    private static string GetBestScoreKey(int levelIndex) => BestScorePrefix + levelIndex;
    
    /// <summary>
    /// Try to get the best score for a scene based on its chronologicalId.
    /// Returns -1 if no score found, otherwise returns the best score.
    /// </summary>
    public static int TryGetSceneBestScore(int chronologicalId)
    {
        return PlayerPrefs.GetInt(GetBestScoreKey(chronologicalId), 0);
    }
    
    /// <summary>
    /// Try to set a new best score for a scene if it is better than existing. Can pass a bool to invert
    /// the condition for being better.
    /// </summary>
    public static void TrySetSceneBestScore(int chronologicalId, int score, bool invertBestCondition = false)
    {
        // only set if it is better than the current best score for the scene
        int currentBestScore = TryGetSceneBestScore(chronologicalId);
        
        bool isBetterScore = invertBestCondition ? score < currentBestScore : score > currentBestScore;
        
        if (!isBetterScore)
        {
            return;
        }
        
        PlayerPrefs.SetInt(GetBestScoreKey(chronologicalId), score);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Deletes the best score for a scene based on its chronologicalId, effectively resetting it to -1 (no score).
    /// </summary>
    public static void ResetSceneBestScore(int chronologicalId)
    {
        PlayerPrefs.DeleteKey(GetBestScoreKey(chronologicalId));
        PlayerPrefs.Save();
    }
    
    
    //todo: add summary
    public static bool IsSceneUnlocked(int chronologicalId, bool invertUnlockCondition = false)
    {
        if (chronologicalId == 0 || chronologicalId == 1)
        {
            return true; // First two scenes (0,1) are always unlocked
        }
        
        int previousSceneBestScore = TryGetSceneBestScore(chronologicalId - 1);
        
        int previousSceneTargetScore = TryGetSceneTargetScore(chronologicalId - 1);
        
        bool isUnlockedConditionMet = 
            invertUnlockCondition ? previousSceneBestScore <= previousSceneTargetScore 
                : previousSceneBestScore >= previousSceneTargetScore;
        
        return isUnlockedConditionMet;
    }
    
    #endregion
}

