using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [HideInInspector]
    public bool isPlaying { get; private set; }
    public bool canReceive { get; private set; }
    public float restartTime;

    private GUIManager guiManager;

    public void Awake()
    {
        instance = this;

        isPlaying = false;
        canReceive = true;
        restartTime = 0;

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
        guiManager.ShowGUI(GUIManager.GUIState.EndGame, hasWon);

        canReceive = false;
        isPlaying = false;
        StartCoroutine(RestartReceive());
    }

    IEnumerator RestartReceive()
    {
        yield return new WaitForSeconds(restartTime);

        guiManager.ShowGUI(GUIManager.GUIState.MainMenu);
        canReceive = true;
    }
}
