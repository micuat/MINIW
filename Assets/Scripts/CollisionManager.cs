using UnityEngine;
using System.Collections;

public class CollisionManager : MonoBehaviour {

    [Header("Game Parameters")]
    /// Time to be waited in order to start a new game
    public int RestartTime;
    /// <summary>
    /// Flag indicating whether the can has hit something
    /// </summary>
    private bool isHit = false;
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

        // Variables inizialization
        doubleMultiplier = 0;
        ducksHit = 0;
        isHit = false;
        canShake = false;
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

            // Keep track of the hit duck before disabling it
            dataManager.AddDisabledDuck(collision.gameObject);
            duck = collision.gameObject;
            // Duck has been hit
            RecordDuckHit(collision.gameObject.name);
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
            isHit = false;
        }
    }

    private void RecordDuckHit(string name)
    {
        // Record a hit the xml file
        if (gameManager.floorType == FloorReceiver.FloorType.Normal)
        {
            dataManager.RecordDuckHit(canID);
        }
        else
        {
            dataManager.RecordDuckHit(canID, name);
        }
    }

    private void DisableCan()
    {
        // Is it the last can?
        // If this control was inside Data Manager, the game would end immediatly after that the last can has been thrown.
        // In this way, the game ends only after the last can hits wether the terrain or the second duck
        if ((canID + 1) == dataManager.canNumber)
        {
            // Notiy it to the Data Manager
            dataManager.LastCan();
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
        SetCanActive(false);
    }

    private int count = 1;
    private void Shake()
    {
        // Make the duck rotate ...
        duck.transform.rotation *= Quaternion.Euler(0, Time.time * rotationVelocity, 0);
        // ... While it goes under the water level
        duck.transform.localPosition = new Vector3(duck.transform.localPosition.x, duck.transform.localPosition.y - sinkingVelocity * count++, duck.transform.localPosition.z);
    }

    private void SetID(int ID)
    {
        canID = ID;
    }

    private void SetCanActive(bool value)
    {
        // Enable/Disable MeshRenderer compotenent, so that the can will/will not be rendered
        gameObject.GetComponent<MeshRenderer>().enabled = value;
        // Enable/Disable box collider
        gameObject.GetComponent<BoxCollider>().enabled = value;
        // Enable/Disable gravity
        gameObject.GetComponent<Rigidbody>().useGravity = value;
    }
}
