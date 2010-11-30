﻿using System;
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
            int processors = Environment.ProcessorCount;
            IDFDataModel idf = new IDFDataModel();
            idf.sEnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\"; 
            idf.idd.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            idf.LoadIDFFile(@"C:\EnergyPlusV6-0-0\ExampleFiles\BasicsFiles\Exercise2C-Solution.idf");

            List<Command> BuildingSurfaces = idf.FindCommandsFromObjectName(@"BuildingSurface:Detailed");
            //BuildingSurfaces.ForEach(delegate(Command s) { s.IsMuted = true; });
            //BuildingSurfaces.ForEach(delegate(Command s) { s.SetArgument(@"Surface Type","Wall"); });
            //BuildingSurfaces.ForEach(delegate(Command s) { s.SetArgumentbyDataName(@"A2", "Floor"); });
            idf.ChangeSimulationPeriod(1, 1, 12, 31);
            idf.SaveIDFFile(@"C:\test\test.idf");

            idf.ProcessEnergyPlusSimulation();
        }
    }
}
