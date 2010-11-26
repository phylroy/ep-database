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
            idd.LoadIDDFile(@"C:\EnergyPlusV5-0-0\Energy+.idd");
            idd.LoadIDFFile(@"C:\EnergyPlusV5-0-0\ExampleFiles\BasicsFiles\Exercise1A.idf");
            idd.SaveIDFFile(@"C:\test\test.idf");
            idd.GetObjectList();
        }
    }
}
