using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {

    bool isHit = false;
    public int dieCountMax = 1000000;
    int dieCount = 0;

    void OnCollisionEnter(Collision collision)
    {
        if (isHit) return;
        isHit = true;
        
        if (collision.gameObject.tag == "Duck")
        {
            collision.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if(isHit)
        {
            dieCount++;
            if(dieCount == dieCountMax)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
