using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;

public class XmlParser : MonoBehaviour
{
    /// <summary>
    /// Global Session ID
    /// </summary>
    static int id;
    /// <summary>
    /// Document in wich data is stored
    /// </summary>
    private XDocument doc;
    /// <summary>
    /// Current working session
    /// </summary>
    private XElement currentSession;
    /// <summary>
    /// Collection used to record all the steps. It is used when the user is playing a non-adaptive game session
    /// </summary>
    private List<Utility.Data> steps;
    /// <summary>
    /// Collection storing all the steps. It is used when the user is playing an adaptive game session
    /// </summary>
    private Dictionary<int, Utility.AdaptiveData> adaptiveSteps;
    /// <summary>
    /// Path where the xml file is saved
    /// </summary>
    private string path;
    /// <summary>
    /// This object is used in order to avoid race conditions
    /// </summary>
    private Object semaphore = new Object();

    public XmlParser(string path)
    {
        // If file exist...
        if (File.Exists(path))
        {
            // ... Load it, and retrieve ID ...
            doc = XDocument.Load(path);
            id = GetCounter();
        }
        else
        {
            // ... Otherwise create a new file
            doc = new XDocument();
            doc.Add(new XElement("Data"));
            id = 1;
        }

        // Save file
        doc.Save(path);

        // Inizialize collections storing steps
        steps = new List<Utility.Data>();
        adaptiveSteps = new Dictionary<int, Utility.AdaptiveData>();
        // Save path
        this.path = path;
    }

    /// <summary>
    /// This function is used to start recording a new game session
    /// </summary>
    /// <param name="start_time">Session start time</param>
    /// <param name="mode">Game mode</param>
    public void AddSession(string start_time, string mode)
    {
        currentSession = new XElement("Session", new XAttribute("Mode", mode), new XAttribute("ID", id++), new XAttribute("start_time", start_time));
    }

    /// <summary>
    /// This function is used to store all the recorded steps. It is to be used in case of non-adaptive game sessions
    /// </summary>
    /// <param name="end_time">Session end time</param>
    public void SaveSession(string end_time)
    {
        // Load xml file
        doc = XDocument.Load(path);

        // Save end time
        currentSession.Add(new XAttribute("end_time", end_time));
        // Save all the recorded steps
        currentSession.Add(
            from c in steps
            select new XElement("Step", new XAttribute("x_value", c.endPoint.x),
                                        new XAttribute("y_value", c.endPoint.y),
                                        new XAttribute("tot", c.tot),
                                        new XAttribute("force", c.force),
                                        new XAttribute("has_hit", c.hasHit))
        );

        // Add session at the bottom of the xml file
        doc.Descendants("Data").First().Add(currentSession);
        // Save file
        doc.Save(path);
        // Clear collection
        steps.Clear();
    }

    /// <summary>
    /// This function is used to store all the recorded steps. It is to be used in case of adaptive game sessions
    /// </summary>
    /// <param name="end_time">Session end time</param>
    public void SaveAdaptiveSession(string end_time)
    {
        try
        {
            // Load xml file
            doc = XDocument.Load(path);

            // Save end time
            currentSession.Add(new XAttribute("end_time", end_time));
            // Save all the recorded steps
            currentSession.Add(
                from c in adaptiveSteps
                select new XElement("Step", new XAttribute("start_time", c.Value.startTime),
                                            new XAttribute("end_time", c.Value.endTime),
                                            new XAttribute("preset", c.Value.preset),
                                            new XAttribute("x_value_start", c.Value.startPoint.x),
                                            new XAttribute("y_value_start", c.Value.startPoint.y),
                                            new XAttribute("x_value_end", c.Value.endPoint.x),
                                            new XAttribute("y_value_end", c.Value.endPoint.y),
                                            new XAttribute("distance", c.Value.distanceTravelled),
                                            new XAttribute("force", c.Value.force),
                                            new XAttribute("force_variation", c.Value.forceAccumulated),
                                            new XAttribute("has_hit", c.Value.hasHit),
                                            from d in c.Value.ducksHit
                                            select new XElement("Duck", new XAttribute("name", d)))
            );

            // Add session at the bottom of the xml file
            doc.Descendants("Data").First().Add(currentSession);
            // Save file
            doc.Save(path);
            // Clear collection
            adaptiveSteps.Clear();
        }
        catch (System.Exception e)
        {
            // Notify error
            Debug.Log("Parser exception: " + e.ToString());
            // Save file
            doc.Save(path);
            // Clear collection
            adaptiveSteps.Clear();
        }
    }

    /// <summary>
    /// This function is to be used just for non-adaptive game sessions. 
    /// It is used to record a thrown can
    /// </summary>
    /// <param name="x_value">Detected x-value</param>
    /// <param name="y_value">Detected y-value</param>
    /// <param name="tot">Fsr value detected</param>
    /// <param name="force">Detected force</param>
    public void AddStep(float x_value, float y_value, int tot, float force)
    {
        Utility.Data d = new Utility.Data();

        d.endPoint = new Vector2(x_value, y_value);
        d.tot = tot;
        d.force = force;
        d.hasHit = false;

        lock (semaphore)
        {
            steps.Add(d);
        }
    }

    /// <summary>
    /// This function is to be used in order to start recording data for an adaptive game session
    /// </summary>
    /// <param name="candID">Can ID to which we are referring</param>
    /// <param name="startTime">Time at wich the recording has started</param>
    public void AddStep(int canID, string start_time, float x_value_start, float y_value_start, string preset)
    {
        Utility.AdaptiveData a = new Utility.AdaptiveData();
        a.startTime = start_time;
        a.startPoint = new Vector2(x_value_start, y_value_start);
        a.preset = preset;
        a.endTime = "";
        a.endPoint = Vector2.zero;
        a.distanceTravelled = 0;
        a.force = 0;
        a.forceAccumulated = 0;
        a.hasHit = false;
        a.ducksHit = new List<string>();

        lock (semaphore)
        {
            adaptiveSteps[canID] = a;
        }
    }

    /// <summary>
    /// This function is to be used together with the "AddStep" adaptive function. 
    /// It is used to save a step previously started
    /// </summary>
    /// <param name="canID">Can ID at which this step is associated</param>
    /// <param name="x_value">Detected x-value</param>
    /// <param name="y_value">Detected y-value</param>
    /// <param name="force">Detected force</param>
    /// <param name="force_accumulator">Force accumulated by the user by moving his foot up and down</param>
    /// <param name="movement_accumulator">Movement accumulate by the user by dragging his foot on the floor</param>
    /// <param name="end_time">Time at which the step has been detected</param>
    public void SaveStep(int canID, float x_value, float y_value, float force, double movement_accumulator, float force_accumulator, string end_time)
    {
        Utility.AdaptiveData a = new Utility.AdaptiveData();

        Utility.AdaptiveData data;
        lock (semaphore)
        {
            data = adaptiveSteps[canID];
        }

        // a = data;
        a.startTime = data.startTime; ;
        a.startPoint = new Vector2(data.startPoint.x, data.startPoint.y);
        a.preset = data.preset;

        a.endTime = end_time;
        a.endPoint = new Vector2(x_value, y_value);
        a.distanceTravelled = movement_accumulator;
        a.force = force;
        a.forceAccumulated = force_accumulator;
        a.hasHit = false;
        a.ducksHit = new List<string>();

        lock (semaphore)
        {
            adaptiveSteps[canID] = a;
        }
    }

    public int GetCounter()
    {
        try
        {
            XNode lastNode = doc.Descendants("Data").Descendants("Session").Last();

            return int.Parse((lastNode as XElement).Attribute("ID").Value) + 1;
        }
        catch (System.Exception)
        {
            return 1;
        }
    }

    public void SetDuckHit(int canId)
    {
        Utility.Data d = new Utility.Data();

        Utility.Data data;
        lock (semaphore)
        {
            data = steps[canId];
        }

        d = data;
        d.hasHit = true;

        lock (semaphore)
        {
            steps[canId] = d;
        }
    }

    public void SetDuckHit(int canId, string name)
    {
        // It is not possible to just update data contained in the list.
        // It is necessary to create a new element, and then add it
        Utility.AdaptiveData a = new Utility.AdaptiveData();

        // Get data regarding the desired canID.
        // It is essential to use a lock in order to avoid race condition
        Utility.AdaptiveData data;
        lock (semaphore)
        {
            data = adaptiveSteps[canId];
        }

        // Many values needn't to be changed
        // d = data;
        a.startTime = data.startTime; ;
        a.startPoint = new Vector2(data.startPoint.x, data.startPoint.y);
        a.preset = data.preset;
        a.endTime = data.endTime;
        a.endPoint = new Vector2(data.endPoint.x, data.endPoint.y);
        a.distanceTravelled = data.distanceTravelled;
        a.force = data.force;
        a.forceAccumulated = data.forceAccumulated;
        // A duck has been hit
        a.hasHit = true;
        // Keep track of the previously hit duck (if any)
        a.ducksHit = new List<string>(data.ducksHit);
        // Add the new one
        a.ducksHit.Add(name);

        // Update element in the list
        lock (semaphore)
        {
            adaptiveSteps[canId] = a;
        }
    }
}
