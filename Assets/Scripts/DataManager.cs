using UnityEngine;
using System.Collections;

public class DataManager : MonoBehaviour {

    static public DataManager instance;

    [Header("Game Parameters")]
    public float gameTime;
    public int duckPoints;
    public int duckNumber;
    private float score;

    private float sessionGameTime;
    private int sessionDuckNumber;

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

        sessionGameTime = gameTime;
        sessionDuckNumber = duckNumber;
    }

    void Update()
    {
        if(duckNumber == 0)
        {
            gameManager.EndGame(true);
            ResetParameters();        }
    }

    void FixedUpdate ()
    {
	    if(sessionGameTime > 0)
        {
            sessionGameTime -= Time.deltaTime;
            if(sessionGameTime < 0)
            {
                sessionGameTime = 0;
            }
            guiManager.SetInGameTime(sessionGameTime);
        }
        else
        {
            gameManager.EndGame(false);
            ResetParameters();        }
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
        sessionDuckNumber--;
    }

    public void ResetParameters()
    {
        score = 0;
        sessionDuckNumber = duckNumber;
        sessionGameTime = gameTime;
    }
}
