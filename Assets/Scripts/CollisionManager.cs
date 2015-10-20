using UnityEngine;
using System.Collections;

public class CollisionManager : MonoBehaviour {

    [Header("Game Parameters")]
    public int Time = 5;
    private bool isHit = false;
    private bool duckHit = false;
    private float positiveShakeValue = 0.0025f;
    private float negativeShakeValue = -0.0025f;
    private GameObject duck;

    private DataManager dataManager;

    public void Start()
    {
        dataManager = DataManager.instance;
    }

    public void Update()
    {
        if(duckHit)
        {
            Shake();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isHit) return;
        isHit = true;
        
        if (collision.gameObject.tag == "Duck")
        {
            dataManager.AddDisabledDuck(collision.gameObject);
            duck = collision.gameObject;
            duckHit = true;
            StartCoroutine(EliminateDuck(collision.gameObject));
            collision.gameObject.SetActive(false);
            dataManager.AddToScore();
            dataManager.DuckHit();
        }

        // Disable can after n seconds in any case, even if the user didn't hit any duck
        StartCoroutine(EliminateCan());
    }

    IEnumerator EliminateDuck(GameObject g)
    {
        yield return new WaitForSeconds(Time);

        duckHit = false;
        g.SetActive(false);
    } 

    IEnumerator EliminateCan()
    {
        yield return new WaitForSeconds(Time);

        gameObject.SetActive(false);
    }

    private void Shake()
    {
        var xAxisShake = Random.Range(positiveShakeValue, negativeShakeValue);
        var yAxisShake = Random.Range(positiveShakeValue, negativeShakeValue);
        var zAxisShake = Random.Range(positiveShakeValue, negativeShakeValue);

        duck.transform.localPosition += new Vector3(xAxisShake, yAxisShake, zAxisShake);
    }
}
