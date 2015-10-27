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
    private List<AdaptiveData> adaptiveSteps;
    private AdaptiveData currentStep;
    private string path;

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
        public string duckName;

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
        adaptiveSteps = new List<AdaptiveData>();
        this.path = path;
    }

    public void AddSession(string start_time)
    {
        currentSession = new XElement("Session", new XAttribute("ID", id++), new XAttribute("start_time", start_time));
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
            select new XElement("Step", new XAttribute("start_time", c.startTime),
                                        new XAttribute("end_time", c.endTime),
                                        new XAttribute("x_value", c.x_value),
                                        new XAttribute("y_value", c.y_value),
                                        new XAttribute("force", c.force),
                                        new XAttribute("has_hit", c.hasHit),
                                        new XAttribute("duck_name", c.duckName))
        );

        doc.Descendants("Data").First().Add(currentSession);
        doc.Save(path);

        steps.Clear();
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
    /// <param name="startTime">Time at wich the recording has started</param>
    public void AddStep(string startTime)
    {
        currentStep = new AdaptiveData();

        currentStep.startTime = startTime;
    }

    /// <summary>
    /// This function is to be used together with the "AddStep" adaptive function. 
    /// It is used to save a step previously started
    /// </summary>
    /// <param name="x_value">Detected x-value</param>
    /// <param name="y_value">Detected y-value</param>
    /// <param name="force">Detected force</param>
    /// <param name="end_time">Time at which the step has been detected</param>
    public void SaveStep(float x_value, float y_value, float force, string end_time)
    {
        currentStep.endTime = end_time;
        currentStep.x_value = x_value;
        currentStep.y_value = y_value;
        currentStep.force = force;
        currentStep.hasHit = false;
        currentStep.duckName = "";

        adaptiveSteps.Add(currentStep);
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
        AdaptiveData d = new AdaptiveData();

        d.startTime = adaptiveSteps[canId].startTime;
        d.endTime = adaptiveSteps[canId].endTime;
        d.x_value = adaptiveSteps[canId].x_value;
        d.y_value = adaptiveSteps[canId].y_value;
        d.force = adaptiveSteps[canId].force;
        d.hasHit = true;
        d.duckName = name;

        adaptiveSteps[canId] = d;
    }
}
