using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using EnergyPlusLib;
using EnergyPlusLib.EnergyPlus;

//to-do

/* 1. Add Iron Python
 * 2. Create Pop up Grid view. 
 * 1. CreateParent/Child/Connected Relationships.
 * 2. Create a grid view master detail. 
 * 3. Tie results SQL to IDFdata.  
 */



namespace EnergyPlus
{
    class Program
    {
        static void Main(string[] args)
        {

            IDFDatabase idf = new IDFDatabase();
            idf.WeatherFilePath = @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
            idf.EnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\";
            idf.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            string test =
                @"BUILDINGSURFACE:DETAILED,ZN001:WALL001,WALL,R13WALL,MAIN ZONE,OUTDOORS,,SUNEXPOSED,WINDEXPOSED,0.5000000,4,0,0,4.572000,0,0,0,15.24000,0,0,15.24000,0,4.572000;";

            string expectedresult =
                @"COIL:COOLING:WATER,MAIN COOLING COIL 1,COOLINGCOILAVAILSCHED,AUTOSIZE,AUTOSIZE,AUTOSIZE,AUTOSIZE,AUTOSIZE,AUTOSIZE,AUTOSIZE,MAIN COOLING COIL 1 WATER INLET NODE,MAIN COOLING COIL 1 WATER OUTLET NODE,MIXED AIR NODE 1,MAIN COOLING COIL 1 OUTLET NODE,;";

            bool IsMatched = idf.TestCommand(test, expectedresult);
            Program.RunFilesInFolder(@"C:\EnergyPlusV6-0-0\ExampleFiles\Testing");

        }


        static void RunFilesInFolder(string folder)
        {

            DirectoryInfo di = new DirectoryInfo(folder);
            FileInfo[] rgFiles = di.GetFiles("*.idf");
            foreach (FileInfo fi in rgFiles)
            {
                IDFDatabase idf = new IDFDatabase();

                idf.WeatherFilePath = @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
                idf.EnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\";
                idf.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
                idf.LoadIDFFile(fi.FullName);
                idf.ChangeSimulationControl();
                idf.ChangeSimulationPeriod(1, 1, 1, 2);
                idf.DeleteCommands(@"Site:Location");
                idf.AddSQLiteOutput();
                idf.ProcessEnergyPlusSimulation();
            }

        }

    }
}
