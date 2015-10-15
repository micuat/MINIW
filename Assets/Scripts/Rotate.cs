using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour {

    bool hit = false;
    // Update is called once per frame
    void Update () {
        

        if (hit)
        {
            // GetComponent<Rigidbody>().AddForce(new Vector3(0, 1.5f, -0.5f));
            
            // transform.rotation = transform.rotation * Quaternion.AngleAxis(Time.deltaTime * -360f, Vector3.right);
        }
        else
        {
            // transform.rotation = transform.rotation * Quaternion.AngleAxis(Time.deltaTime * 360f, Vector3.right);

        }
    }

    void OnTriggerEnter(Collider other)
    {
        // GetComponent<Rotate>().enabled = false;

        if(other.tag == "Duck")
        {
            other.enabled = false;
            hit = true;
        }
    }
}
