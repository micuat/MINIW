using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DataManager : MonoBehaviour {

    static public DataManager instance;

    [Header("Game Parameters")]
    public float gameTime;
    public int duckPoints;
    public int duckNumber;
    private float score;

    public float sessionGameTime { get; private set; }
    public int sessionDuckNumber { get; private set; }
    private List<KeyValuePair<Vector3, Quaternion>> ducksPositions;

    private GUIManager guiManager;
    private GameManager gameManager;

    public void Awake()
    {
        instance = this;

        ducksPositions = new List<KeyValuePair<Vector3, Quaternion>>();
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
            ResetParameters();
            gameManager.EndGame(true);
        }
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
            ResetParameters();
            gameManager.EndGame(false);
            
        }
	}

    public void AddToScore()
    {
        score += (duckPoints * sessionGameTime);
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

    private void ResetParameters()
    {
        score = 0;
        sessionDuckNumber = duckNumber;
        sessionGameTime = gameTime;

        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");

        for(int i = 0; i < ducks.Length; i++)
        {
            KeyValuePair<Vector3, Quaternion> k = ducksPositions[i];
            ducks[i].transform.localPosition = k.Key;
            ducks[i].transform.localRotation = k.Value;
            ducks[i].GetComponent<DuckMovement>().DefinePath();
            ducks[i].SetActive(true);
        }
    }

    public void AddDuckPosition(KeyValuePair<Vector3, Quaternion> t)
    {
        ducksPositions.Add(t);
    }
}
