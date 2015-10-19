using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class GUIManager : MonoBehaviour
{
    static public GUIManager instance;

    [HideInInspector]
    public enum GUIState { MainMenu, EndGame, Void}

    [Header("Ingame Menu")]
    public GameObject inGameTopPanel;
    public Text inGameTimeText;
    public Text inGameScoreText;

    [Header("Main Menu")]
    public GameObject mainMenu;
    public Text topText;
    public Text centerText;
    public Text bottomText;

    [HideInInspector]
    public GUIState guiState;

    private DataManager dataManager;

    public void Awake()
    {
        instance = this;
    }

    // Use this for initialization
    void Start()
    {
        dataManager = DataManager.instance;

        ShowGUI(GUIState.MainMenu);
    }

    public void ShowGUI(GUIState state, bool HasWon = false)
    {
        switch (state)
        {
            case GUIState.MainMenu:
                Time.timeScale = 0; // Stop the game
                inGameTopPanel.SetActive(false);
                mainMenu.SetActive(true);
                topText.enabled = true;
                centerText.enabled = false;
                bottomText.enabled = true;
                break;
            case GUIState.EndGame:
                Time.timeScale = 0;
                inGameTopPanel.SetActive(false);
                // Show final score
                mainMenu.SetActive(true);
                topText.enabled = false;
                centerText.text = !HasWon ? "Game Over!\nFinal score: " + dataManager.GetScore() : "You Won!\nFinal score: " + dataManager.GetScore();
                centerText.enabled = true;
                bottomText.enabled = false;
                break;
            case GUIState.Void:
                inGameTopPanel.SetActive(true);
                mainMenu.SetActive(false);
                Time.timeScale = 1;
                break;
        }

        guiState = state;
    }

    public void SetInGameTime(float time)
    {
        inGameTimeText.text = time.ToString("0.00");
    }

    public void SetInGameScore(int score)
    {
        inGameScoreText.text = score.ToString();
    }
}
