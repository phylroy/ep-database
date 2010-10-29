using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using EnergyPlusLib;
namespace SuperEE4
{
    public static class IDDDataCreator
    {
        public static DataSet CreateDataSet()
        {
            EPIDD epidd = new EPIDD();

            return epidd.IDD;
        }
    }
}
