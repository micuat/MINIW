using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class DataManager : MonoBehaviour {

    static public DataManager instance;

    [Header("Game Parameters")]
    public float gameTime;
    public int duckPoints;
    public int duckNumber;
    private float score;

    public float sessionGameTime { get; private set; }
    public int sessionDuckNumber { get; private set; }
    private List<GameObject> disabledDucks;
    private Dictionary<String, KeyValuePair<Vector3, Quaternion>> ducksPositions;

    private GUIManager guiManager;
    private GameManager gameManager;

    public void Awake()
    {
        instance = this;

        ducksPositions = new Dictionary<String, KeyValuePair<Vector3, Quaternion>>();
        disabledDucks = new List<GameObject>();
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
        if(duckNumber == 0 && guiManager.guiState == GUIManager.GUIState.Void)
        {
            ResetParameters();
            gameManager.EndGame(true);
        }
    }

    void FixedUpdate ()
    {
	    if(sessionGameTime > 0 && guiManager.guiState == GUIManager.GUIState.Void)
        {
            sessionGameTime -= Time.deltaTime;
            if(sessionGameTime < 0)
            {
                sessionGameTime = 0;
            }
            guiManager.SetInGameTime(sessionGameTime);
        }
        else if(guiManager.guiState == GUIManager.GUIState.Void)
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

    public void SetScore(float f)
    {
        score = f;
        guiManager.SetInGameScore((int)score);
    }

    private void ResetParameters()
    {
        // score = 0;
        sessionDuckNumber = duckNumber;
        sessionGameTime = gameTime;
        
        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");
        
        for(int i = 0; i < ducks.Length; i++)
        {
            ducks[i].GetComponent<DuckMovement>().StopPath();
        }

        for (int i = 0; i < disabledDucks.Count; i++)
        {
            disabledDucks[i].GetComponent<DuckMovement>().StopPath();
        }
    }

    public void AddDuckPosition(String name, KeyValuePair<Vector3, Quaternion> t)
    {
        ducksPositions[name] = t;
    }

    public void AddDisabledDuck(GameObject g)
    {
        disabledDucks.Add(g);
    }

    public void ResetDucks()
    {
        GameObject[] ducks = GameObject.FindGameObjectsWithTag("Duck");

        for (int i = 0; i < ducks.Length; i++)
        { 
            ducks[i].transform.localPosition = ducksPositions[ducks[i].name].Key;
            ducks[i].transform.localRotation = ducksPositions[ducks[i].name].Value;
            ducks[i].GetComponent<DuckMovement>().DefinePath();
            ducks[i].SetActive(true);
        }

        for (int i = 0; i < disabledDucks.Count; i++)
        {
            disabledDucks[i].GetComponent<DuckMovement>().StopPath();
            disabledDucks[i].transform.localPosition = ducksPositions[disabledDucks[i].name].Key;
            disabledDucks[i].transform.localRotation = ducksPositions[disabledDucks[i].name].Value;
            disabledDucks[i].GetComponent<DuckMovement>().DefinePath();
            disabledDucks[i].SetActive(true);
        }

        disabledDucks.Clear();
    }
}
