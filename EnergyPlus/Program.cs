﻿using System;
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
            /*
            //Doe test
            INPDatabase building = new INPDatabase();
            building.LoadInputFile(@"C:\XXX\T2408SamplePrelim.inp", "");
            building.write_clean_output_file(@"C:\XXX\doe.inp");
            building.RunSimulation();

            //SQLite test
            SqliteDB db = new SqliteDB(@"C:\EnergyPlusV6-0-0\ExampleFiles\Testing\simrun\RefBldgSecondarySchoolNew2004_Chicago.sql");
            db.LoadDataSet();
            DataTable List = db.ListDataTables();
            DataTable Errors = db.LoadDataTable("Errors");
             */
            IDFDatabase idf = new IDFDatabase();
            idf.WeatherFilePath = @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
            idf.SimulationEngineRootFolder = @"C:\EnergyPlusV6-0-0\";
            idf.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            Program.RunFilesInFolder(@"C:\EnergyPlusV6-0-0\ExampleFiles");

        }


        static void RunFilesInFolder(string folder)
        {



            if (Directory.Exists(folder))
            {
            DirectoryInfo di = new DirectoryInfo(folder);
            FileInfo[] rgFiles = di.GetFiles("*.idf");
            foreach (FileInfo fi in rgFiles)
            {
                IDFDatabase idf = new IDFDatabase();

                idf.WeatherFilePath = @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
                idf.SimulationEngineRootFolder = @"C:\EnergyPlusV6-0-0\";
                idf.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
                idf.LoadInputFile(fi.FullName);
                idf.ChangeSimulationControl();
                idf.ChangeSimulationPeriod(1, 1, 12, 31);
                idf.AddSQLiteOutput();

                idf.RunSimulation();
            }
            }
            else
            {
                Console.WriteLine("{0} is not a valid file or directory.", folder);
                Console.WriteLine("Press any key to continue"); 
                Console.Read();
            }

        }

    }
}
