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
            int processors = Environment.ProcessorCount;
            IDFDataModel idf = new IDFDataModel();
            //Set weather file.
            idf.sWeatherFile= @"C:\EnergyPlusV6-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
            idf.sEnergyPlusRootFolder = @"C:\EnergyPlusV6-0-0\"; 
            idf.idd.LoadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            idf.LoadIDFFile(@"C:\EnergyPlusV6-0-0\ExampleFiles\5ZoneGeometryTransform.idf");

            //Tweak Building. 
            List<Command> BuildingSurfaces = idf.FindCommandsFromObjectName(@"FenestrationSurface:Detailed");
            BuildingSurfaces.ForEach(delegate(Command s) { s.IsMuted = true; });
            BuildingSurfaces.ForEach(delegate(Command s) { s.SetArgument(@"Surface Type","Wall"); });
            //BuildingSurfaces.ForEach(delegate(Command s) { s.SetArgumentbyDataName(@"A2", "Floor"); });
            idf.ChangeSimulationPeriod(1, 1, 1, 31);
            idf.ChangeAspectRatioXY(1.0, 2.0);
            idf.SaveIDFFile(@"C:\test\test.idf");

            idf.ProcessEnergyPlusSimulation();
        }
    }
}
