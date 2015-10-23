using UnityEngine;
using System.Collections;

public class CollisionManager : MonoBehaviour {

    [Header("Game Parameters")]
    /// Time to be waited in order to start a new game
    public int RestartTime = 5;
    /// <summary>
    /// Flag indicating whether the can has hit something
    /// </summary>
    private bool isHit = false;
    /// <summary>
    /// Flag indicating whether the can has hit a duck object
    /// </summary>
    private bool duckHit = false;
    /// <summary>
    /// The duck object hit by the can
    /// </summary>
    private GameObject duck;
    /// <summary>
    /// Velocity at wich the duck will rotate once it is hit
    /// </summary>
    public float rotationVelocity = 50.0f;
    /// <summary>
    /// Velocity at wich the duck will go under the water level once it is hit
    /// </summary>
    public float sinkingVelocity = 0.1f;

    private static int c = 0;

    private DataManager dataManager;

    public void Start()
    {
        // Get DataManager instance
        dataManager = DataManager.instance;
    }

    public void Update()
    {
        // When the duck is hit...
        if(duckHit)
        {
            // .. Shake it!
            Shake();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // If something has already been hit, don't do anything
        if (isHit) return;
        
        // If we get here, it means that the can has hit something for the first time
        isHit = true;
        
        // Is the hit GameObject a Duck?
        if (collision.gameObject.tag == "Duck")
        {
            // Keep track of the hit duck before disabling 
            dataManager.AddDisabledDuck(collision.gameObject);
            duck = collision.gameObject;
            // Duck has been hit
            duckHit = true;
            // Start courite that will make the duck inactive
            StartCoroutine(EliminateDuck(collision.gameObject));
            // Update score
            dataManager.AddToScore();
            // Update ducks number
            dataManager.DuckHit();
        }

        if(duckHit)
        {
            dataManager.RecordDuckHit(c);
        }
        
        // Is it the last can?
        if(++c == dataManager.canNumber)
        {
            // Notiy it to the Data Manager
            dataManager.LastCan();
            c = 0;
        }

        // Disable can after n seconds in any case, even if the user didn't hit any duck
        StartCoroutine(EliminateCan());
    }

    IEnumerator EliminateDuck(GameObject g)
    {
        yield return new WaitForSeconds(1);

        // Deactivate duck
        g.SetActive(false);

        //Reset values
        duckHit = false;
        count = 1;
    } 

    IEnumerator EliminateCan()
    {
        yield return new WaitForSeconds(RestartTime);

        // Deactivate thrown can
        gameObject.SetActive(false);
    }

    private int count = 1;
    private void Shake()
    {
        // Make the duck rotate ...
        duck.transform.rotation *= Quaternion.Euler(0, Time.time * rotationVelocity, 0);
        // ... While it goes under the water level
        duck.transform.localPosition = new Vector3(duck.transform.localPosition.x, duck.transform.localPosition.y - sinkingVelocity * count++, duck.transform.localPosition.z);
    }
}
