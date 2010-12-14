using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
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

            

            IDFDatabase idf = new IDFDatabase();
            idf.WeatherFilePath = @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
            idf.EnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\";
            idf.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            string test =
                @"  Schedule:Day:List,
    ListScheduleExample,     
    Any Number,              
    No,                      
    60,                      
    1,                       
    2,                       
    3,                       
    4,                       
    5,                       
    6,                       
    7,                       
    8,                       
    9,                       
    10,                      
    11,                      
    12,                      
    13,                      
    14,                      
    15,                      
    16,                      
    17,                      
    18,                      
    19,                      
    20,                      
    21,                      
    22,                      
    23,                      
    24;                      
";

            string result =
                @"  Schedule:Day:List,
    ListScheduleExample,     
    Any Number,              
    No,                      
    60,                      
    1,                       
    2,                       
    3,                       
    4,                       
    5,                       
    6,                       
    7,                       
    8,                       
    9,                       
    10,                      
    11,                      
    12,                      
    13,                      
    14,                      
    15,                      
    16,                      
    17,                      
    18,                      
    19,                      
    20,                      
    21,                      
    22,                      
    23,                      
    24;                      
";


            List<string> test2 = idf.CleanCommandStrings(Regex.Split(test, "\r\n")).ToList();
            IDFCommand command = idf.GetCommandFromTextString(test2.FirstOrDefault());
            string test3 = command.ToIDFString().Trim();
            bool test4 = ( result.Trim() == command.ToIDFString().Trim());




            Program.RunFilesInFolder(@"C:\EnergyPlusV6-0-0\ExampleFiles\testing");

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
                //idf.AddSQLiteOutput();
                idf.ProcessEnergyPlusSimulation();
            }

        }

    }
}
