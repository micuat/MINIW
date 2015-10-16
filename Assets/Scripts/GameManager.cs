using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [HideInInspector]
    public bool isPlaying { get; private set; }

    private GUIManager guiManager;

    public void Start()
    {
        instance = this;

        isPlaying = false;

        guiManager = GUIManager.instance;
    }

    public void SetPlayingStatus(bool b)
    {
        isPlaying = b;
    }

    public void StartGame()
    {
        guiManager.ShowGUI(GUIManager.GUIState.Void);
        isPlaying = true;
    }

    public void EndGame(bool hasWon)
    {
        guiManager.ShowGUI(GUIManager.GUIState.EndGame, true);
        isPlaying = false;
    }


}
