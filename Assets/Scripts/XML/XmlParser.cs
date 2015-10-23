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
    private string path;

    private struct Data
    {
        public float x_value;
        public float y_value;
        public int tot;
        public float force;
        public bool hasHit;
    }

    public XmlParser(string path)
    {
        if (File.Exists(path))
        {
            doc = XDocument.Load(path);
            id = GetCounter();
            Debug.Log(id);
        }
        else
        {
            doc = new XDocument();
            doc.Add(new XElement("Data"));
            id = 1;
        }
        
        doc.Save(path);

        steps = new List<Data>();
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
}
