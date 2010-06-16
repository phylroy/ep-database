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

        }
    }
}
