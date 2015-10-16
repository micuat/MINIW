using UnityEngine;
using System.Collections;

public class CollisionManager : MonoBehaviour {

    [Header("Game Parameters")]
    public int Time = 5;
    private bool isHit = false;

    private DataManager dataManager;

    public void Start()
    {
        dataManager = DataManager.instance;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isHit) return;
        isHit = true;
        
        if (collision.gameObject.tag == "Duck")
        {
            collision.gameObject.SetActive(false);
            dataManager.AddToScore();
            dataManager.DuckHit();
        }

        // Disable can after n seconds in any case, even if the user didn't hit any duck
        StartCoroutine(Eliminate());
    }

    IEnumerator Eliminate()
    {
        yield return new WaitForSeconds(Time);

        gameObject.SetActive(false);
    }
}
