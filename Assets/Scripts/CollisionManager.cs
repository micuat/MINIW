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
    private bool isDuckHit = false;
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
    /// <summary>
    /// Can counter
    /// </summary>
    private static int c = 0;
    /// <summary>
    /// Can ID
    /// </summary>
    private int canID;
    /// <summary>
    /// Double multiplier
    /// </summary>
    private int doubleMultiplier;
    /// <summary>
    /// Ducks hit using this can. It is possible to hit up to 2 ducks with the same can
    /// </summary>
    private int ducksHit;
    /// <summary>
    /// Flag indicating whetjer the duck can shake
    /// </summary>
    private bool canShake;
    /// <summary>
    /// Sound to be played when a duck is hit
    /// </summary>
    public AudioClip audioDuck;

    private DataManager dataManager;
    private GameManager gameManager;

    public void Start()
    {
        // Get DataManager instance
        dataManager = DataManager.instance;
        // Get GameManager instance
        gameManager = GameManager.instance;

        doubleMultiplier = 0;
        ducksHit = 0;
        isHit = false;
        canShake = false;
        canID = -1;
    }

    public void Update()
    {
        // When the duck is hit...
        if(canShake)
        {
            // .. Shake it!
            Shake();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // If something has already been hit, don't do anything
        if (isHit) return;

        if(canID == -1)
        {
            canID = c;
            c++;
        }
        
        // Is the hit GameObject a Duck?
        if (collision.gameObject.tag == "Duck")
        {
            // Play sound
            AudioSource.PlayClipAtPoint(audioDuck, Camera.main.transform.position);

            // A duck has been hit
            ducksHit++;
            // Increase double multiplier
            doubleMultiplier++;

            // Has the can hit the second duck?
            if(ducksHit == 2)
            {
                // Time to disable the can
                isHit = true;
                DisableCan();
            }

            // Keep track of the hit duck before disabling 
            dataManager.AddDisabledDuck(collision.gameObject);
            duck = collision.gameObject;
            // Duck has been hit
            isDuckHit = true;
            // Time to shake
            canShake = true;
            // Start courite that will make the duck inactive
            StartCoroutine(EliminateDuck(collision.gameObject));
            // Update score
            bool b = ducksHit == 2;
            dataManager.AddToScore(b);
            // Update ducks number
            dataManager.DuckHit();
        }
        else if(collision.gameObject.tag == "Terrain")
        {
            // Disable the can
            isHit = true;
            DisableCan();
        }

        // If a duck has been hit ...
        if(isDuckHit)
        {
            // ... Record it in the xml file
            if (gameManager.floorType == FloorReceiver.FloorType.Normal)
            {
                dataManager.RecordDuckHit(canID);
            }
            else
            {
                dataManager.RecordDuckHit(canID, collision.gameObject.name);
            }

            // Reset flag
            isDuckHit = false;
        }

        // Check if it is necessary to update multiplier
        if (isHit && ducksHit > 0)
        {
            dataManager.IncreaseMultiplier();
        }
        else if (isHit && ducksHit == 0)
        {
            dataManager.ResetMultiplier();
        }

        // Reset values
        if (isHit)
        {
            doubleMultiplier = 0;
            ducksHit = 0;
        }
    }

    private void DisableCan()
    {
        // Is it the last can?
        if (c == dataManager.canNumber)
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
        canShake = false;
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
