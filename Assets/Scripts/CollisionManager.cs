using UnityEngine;
using System.Collections;

public class CollisionManager : MonoBehaviour {

    bool isHit = false;
    public int Time = 5;
    int dieCount = 0;

    void OnCollisionEnter(Collision collision)
    {
        if (isHit) return;
        isHit = true;
        
        if (collision.gameObject.tag == "Duck")
        {
            collision.gameObject.SetActive(false);
        }

        StartCoroutine(Eliminate());
    }

    IEnumerator Eliminate()
    {
        yield return new WaitForSeconds(Time);

        gameObject.SetActive(false);
    }
}
