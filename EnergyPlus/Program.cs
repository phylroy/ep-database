using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml.Serialization;
using EnergyPlusLib;
using gbXMLLib;
using EnergyPlusLib.DataAccess;
using EnergyPlusLib.DataModel.IDF; 


namespace EnergyPlus
{
    class Program
    {
        static void Main(string[] args)
        {

            DirectoryInfo di = new DirectoryInfo(@"C:\EnergyPlusV6-0-0\ExampleFiles\basicsfiles");
            FileInfo[] rgFiles = di.GetFiles("*.idf");
            foreach (FileInfo fi in rgFiles)
            {
                IDFDatabase idf = new IDFDatabase();

                idf.WeatherFilePath = @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
                idf.EnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\";
                idf.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
                idf.LoadIDFFile(fi.FullName);
                List<IDFCommand> CommandError = idf.FindCommandsWithRangeErrors().ToList<IDFCommand>();
                idf.ProcessEnergyPlusSimulation();   
            }
        }
    }
}
