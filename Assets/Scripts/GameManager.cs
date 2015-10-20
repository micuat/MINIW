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
    private DataManager dataManager;

    public void Awake()
    {
        instance = this;

        isPlaying = false;
        canReceive = false;

        guiManager = GUIManager.instance;
        dataManager = DataManager.instance;

        StartCoroutine(SetReceive());
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
        canReceive = false;
        isPlaying = false;

        guiManager.ShowGUI(GUIManager.GUIState.EndGame, hasWon);
        
        StartCoroutine(RestartReceive());
    }

    IEnumerator RestartReceive()
    {
        yield return new WaitForSeconds(restartTime);
        
        guiManager.ShowGUI(GUIManager.GUIState.MainMenu);
        dataManager.ResetDucks();

        StartCoroutine(SetReceive());
    }

    IEnumerator SetReceive()
    {
        yield return new WaitForSeconds(2);

        guiManager.ShowBottomLine();
        canReceive = true;
    }
}
