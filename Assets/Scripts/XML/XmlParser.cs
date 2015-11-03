using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;

public class XmlParser
{
    static int id;

    private XDocument doc;
    private XElement currentSession;
    private List<Data> steps;
    private Dictionary<int, AdaptiveData> adaptiveSteps;
    private AdaptiveData currentStep;
    private string path;
    private Object semaphore = new Object();

    private struct Data
    {
        public float x_value;
        public float y_value;
        public int tot;
        public float force;
        public bool hasHit;
    }

    private struct AdaptiveData
    {
        public string startTime;
        public string endTime;
        public float x_value;
        public float y_value;
        public float force;
        public bool hasHit;
        public List<string> ducksHit;

    }

    public XmlParser(string path)
    {
        if (File.Exists(path))
        {
            doc = XDocument.Load(path);
            id = GetCounter();
        }
        else
        {
            doc = new XDocument();
            doc.Add(new XElement("Data"));
            id = 1;
        }
        
        doc.Save(path);

        steps = new List<Data>();
        adaptiveSteps = new Dictionary<int, AdaptiveData>();
        this.path = path;
    }

    public void AddSession(string start_time, string mode)
    {
        currentSession = new XElement("Session", new XAttribute("Mode", mode), new XAttribute("ID", id++), new XAttribute("start_time", start_time));
    }

    public void SaveSession(string end_time)
    {
        doc = XDocument.Load(path);

        currentSession.Add(new XAttribute("end_time", end_time));
        currentSession.Add(
            from c in steps
            select new XElement("Step", new XAttribute("x_value", c.x_value),
                                        new XAttribute("y_value", c.y_value),
                                        new XAttribute("tot", c.tot),
                                        new XAttribute("force", c.force),
                                        new XAttribute("has_hit", c.hasHit))
        );

        doc.Descendants("Data").First().Add(currentSession);
        doc.Save(path);

        steps.Clear();
    }

    public void SaveAdaptiveSession(string end_time)
    {
        doc = XDocument.Load(path);

        currentSession.Add(new XAttribute("end_time", end_time));
        currentSession.Add(
            from c in adaptiveSteps
            select new XElement("Step", new XAttribute("start_time", c.Value.startTime),
                                        new XAttribute("end_time", c.Value.endTime),
                                        new XAttribute("x_value", c.Value.x_value),
                                        new XAttribute("y_value", c.Value.y_value),
                                        new XAttribute("force", c.Value.force),
                                        new XAttribute("has_hit", c.Value.hasHit),
                                        from d in c.Value.ducksHit
                                        select new XElement("Duck", new XAttribute("name", d)))
        );

        doc.Descendants("Data").First().Add(currentSession);
        doc.Save(path);

        adaptiveSteps.Clear();
    }

    /// <summary>
    /// This function is to be used just for non-adaptive game sessions. 
    /// It is used to record a can thrown
    /// </summary>
    /// <param name="x_value">Detected x-value</param>
    /// <param name="y_value">Detected y-value</param>
    /// <param name="tot">Fsr value detected</param>
    /// <param name="force">Detected force</param>
    public void AddStep(float x_value, float y_value, int tot, float force)
    {
        Data d = new Data();

        d.x_value = x_value;
        d.y_value = y_value;
        d.tot = tot;
        d.force = force;
        d.hasHit = false;

        steps.Add(d);
    }

    /// <summary>
    /// This function is to be used in order to start recording data for an adaptive game session
    /// </summary>
    /// <param name="candID">Can ID to which we are referring</param>
    /// <param name="startTime">Time at wich the recording has started</param>
    public void AddStep(int canID, string startTime)
    {
        AdaptiveData a = new AdaptiveData();
        a.startTime = startTime;

        lock (semaphore)
        {
            adaptiveSteps[canID] = a; 
        }
    }

    /// <summary>
    /// This function is to be used together with the "AddStep" adaptive function. 
    /// It is used to save a step previously started
    /// </summary>
    /// <param name="x_value">Detected x-value</param>
    /// <param name="y_value">Detected y-value</param>
    /// <param name="force">Detected force</param>
    /// <param name="end_time">Time at which the step has been detected</param>
    public void SaveStep(int canID, float x_value, float y_value, float force, string end_time)
    {
        AdaptiveData a = new AdaptiveData();

        string start_time;
        lock (semaphore)
        {
            start_time = adaptiveSteps[canID].startTime;
        }

        a.startTime = start_time;
        a.endTime = end_time;
        a.x_value = x_value;
        a.y_value = y_value;
        a.force = force;
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
        Data d = new Data();

        d.x_value = steps[canId].x_value;
        d.y_value = steps[canId].y_value;
        d.tot = steps[canId].tot;
        d.force = steps[canId].force;
        d.hasHit = true;

        steps[canId] = d;
    }

    public void SetDuckHit(int canId, string name)
    {
        // It is not possible to just update data contained in the list.
        // It is necessary to create a new element, and then add it
        AdaptiveData d = new AdaptiveData();

        // Get data regarding the desired canID.
        // It is essential to use a lock in order to avoid race condition
        AdaptiveData data;
        lock (semaphore)
        {
            data = adaptiveSteps[canId];
        }

        // Those values needn't to be updated
        d.startTime = data.startTime;
        d.endTime = data.endTime;
        d.x_value = data.x_value;
        d.y_value = data.y_value;
        d.force = data.force;
        // A duck has been hit
        d.hasHit = true;
        // Keep track of the previously hit duck (if any)
        d.ducksHit = new List<string>(data.ducksHit);
        // Add the new one
        d.ducksHit.Add(name);

        // Update element in the list
        lock (semaphore)
        {
            adaptiveSteps[canId] = d; 
        }
    }
}
