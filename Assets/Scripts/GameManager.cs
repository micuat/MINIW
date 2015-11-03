using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Game Parameters")]
    public float restartTime;
    public bool limitTime = false;
    public bool updateForceBar;
    public bool isPlaying { get; private set; }
    public bool canReceive { get; private set; }
    public FloorReceiver.FloorType floorType { get; private set; }

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

    public void PlayStandardMode(bool value)
    {
        updateForceBar = value;
        guiManager.UpdateForceBarSprite();
    }

    public void SetFloorType(FloorReceiver.FloorType floorType)
    {
        this.floorType = floorType;
    }

    public void StartGame()
    {
        if (!limitTime)
        {
            guiManager.SetInGameCanNumber(dataManager.canNumber);
        }

        guiManager.ShowGUI(GUIManager.GUIState.Void);

        // isPlaying = true;
        SetPlayingStatus(true);
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

    public string GetModeGame()
    {
        return updateForceBar ? "Standard" : "Extreme";
    }
}
