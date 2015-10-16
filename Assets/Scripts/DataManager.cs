using UnityEngine;
using System.Collections;

public class DataManager : MonoBehaviour {

    static public DataManager instance;

    [Header("Game Parameters")]
    public float gameTime;
    public int duckPoints;
    public int duckNumber;
    private float score;

    private GUIManager guiManager;
    private GameManager gameManager;


    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        guiManager = GUIManager.instance;
        gameManager = GameManager.instance;

        score = 0;
    }

    void Update()
    {
        if(duckNumber == 0)
        {
            gameManager.EndGame(true);
        }
    }

    void FixedUpdate ()
    {
	    if(gameTime > 0)
        {
            gameTime -= Time.deltaTime;
            if(gameTime < 0)
            {
                gameTime = 0;
            }
            guiManager.SetInGameTime(gameTime);
        }
        else
        {
            gameManager.EndGame(false);
        }
	}

    public void AddToScore()
    {
        score += (duckPoints * gameTime);
        guiManager.SetInGameScore(Mathf.RoundToInt(score));
    }

    public int GetScore()
    {
        return Mathf.RoundToInt(score);
    }

    public void DuckHit()
    {
        duckNumber--;
    }
}
