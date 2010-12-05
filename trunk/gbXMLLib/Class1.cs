using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace gbXMLLib
{
    public class Class1
    {
        public Class1()
        {
            XmlSerializer s = new XmlSerializer(typeof(gbXML));
            gbXML gbxml;
            System.IO.TextReader r = new System.IO.StreamReader(@"\\DLINK-669596\Volume_1\plopez\Desktop\gbXML\excercisefacility.xml");
            gbxml = (gbXML)s.Deserialize(r);
            r.Close();
        }
    }
}
