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
            EPlusDataModel idd = EPlusDataModel.GetInstance();
            idd.ReadIDDFile(@"C:\EnergyPlusV6-0-0\Energy+.idd");
            idd.ReadIDFFile(@"C:\EnergyPlusV6-0-0\ExampleFiles\1ZoneEvapCooler.idf");
        }
    }
}
