using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EnergyPlus
{
    class Program
    {
        static void Main(string[] args)
        {
            EPIDF epidf = new EPIDF();
            epidf.epidd.writeIDDXML();
            epidf.ReadIDFFile(@"C:\EnergyPlusV5-0-0\ExampleFiles\BenchmarkOutpatientNew_USA_IL_CHICAGO-OHARE.idf");

        }
    }
}
