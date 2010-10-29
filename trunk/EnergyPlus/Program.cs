using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using EnergyPlusLib;

namespace EnergyPlus
{
    class Program
    {
        static void Main(string[] args)
        {
            EPIDD epidd = new EPIDD();
            epidd.CreateReferenceListTable();
            List<int> test = epidd.GetChildObjectIDs(65);
            List<string> test2 = epidd.GetChildObjectIDStrings(76);
            epidd.writeIDDXML();
            epidd.ReadIDFFile(@"C:\EnergyPlusV5-0-0\ExampleFiles\1ZoneEvapCooler.idf");
            epidd.WriteIDFFile(@"C:\test\output.idf");

        }
    }
}
