﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml.Serialization;
using EnergyPlusLib;
using gbXMLLib;
using EnergyPlusLib.DataModel.IDF;


namespace EnergyPlus
{
    class Program
    {
        static void Main(string[] args)
        {


            int processors = Environment.ProcessorCount;
            IDFDataModel idf = new IDFDataModel();
            //Set weather file.
            idf.sWeatherFile= @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
            idf.sEnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\"; 
            idf.idd.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            idf.LoadIDFFile(@"C:\EnergyPlusV6-0-0\ExampleFiles\5ZoneGeometryTransform.idf");

            //Find all Zones. 
            List<Command> Zones= idf.FindCommandsFromObjectName(@"Zone");

            //Building
            //  Plant 
            //      AHU
            //      Zones
            //      Loads.
            //Constructions
            //Schedules


            List<Command> BuildingSurfaces = idf.FindCommandsFromObjectName(@"FenestrationSurface:Detailed");
            List<Command> Surfaces = idf.FindCommands(@"FenestrationSurface:Detailed", "Zone Name", "PLENUM-1");
           

            BuildingSurfaces.ForEach(delegate(Command command) { command.IsMuted = true; });
            BuildingSurfaces.ForEach(delegate(Command command) { command.SetArgument(@"Surface Type","Wall"); });
            //BuildingSurfaces.ForEach(delegate(Command s) { s.SetArgumentbyDataName(@"A2", "Floor"); });
            idf.ChangeSimulationPeriod(1, 1, 12, 31);
            idf.ChangeAspectRatioXY(1.0, 2.0);
            idf.UpdateAllObjectLists();
            idf.UpdateAllObjectLists();
            idf.ProcessEnergyPlusSimulation();
        }
    }
}
