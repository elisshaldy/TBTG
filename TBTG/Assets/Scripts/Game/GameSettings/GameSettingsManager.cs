using UnityEngine;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }
    
    public SceneState CurrentMode { get; set; }
    public GameSettings CurrentSettings { get; set; }
    
    [SerializeField] public bool IsDebug = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
