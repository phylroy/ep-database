using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EnergyPlus
{
    class Program
    {
        static void Main(string[] args)
        {
            EPIDF epidf = new EPIDF();
            epidf.epidd.CreateReferenceListTable();
            List<int> test = epidf.epidd.GetChildObjectIDs(65);
            List<string> test2 = epidf.epidd.GetChildObjectIDStrings(76);
            epidf.epidd.writeIDDXML();
            epidf.ReadIDFFile(@"C:\EnergyPlusV5-0-0\ExampleFiles\1ZoneEvapCooler.idf");
            epidf.WriteIDFFile(@"C:\test\output.idf");

        }
    }
}
