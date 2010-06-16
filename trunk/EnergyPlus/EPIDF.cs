using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;


namespace EnergyPlus
{
    class EPIDF
    {
        public EPIDD epidd;

        public EPIDF()
        {
            epidd = EPIDD.GetInstance();
            DataSet idfDataSet = new DataSet();
            //iterate through object database
            DataTable objects = epidd.IDD.Tables["objects"];
            foreach (DataRow objectRow in objects.Rows)
            {
                DataRow[] objectSwitchRows = objectRow.GetChildRows("objectSwitches");
                DataTable newTable = idfDataSet.Tables.Add(objectRow["object_name"].ToString());
                DataRow[] fields = objectRow.GetChildRows("ObjectFields");
                foreach (DataRow fieldRow in fields)
                {
                    DataRow[] fieldSwitchRows = fieldRow.GetChildRows("FieldSwitches");
                    DataColumn column = new DataColumn(fieldRow["field_name"].ToString());
                    //Console.WriteLine(objectRow["object_name"].ToString() + fieldRow["field_name"].ToString());

                    // find dataType
                    string dataType = "alpha";
                    switch (dataType)
                    {
                        case "choice":
                        case "object-list":
                        case "alpha":
                            column.DataType = System.Type.GetType("System.String");
                            break;
                        case "integer":
                            column.DataType = System.Type.GetType("System.Int32");
                            break;
                        case "real":
                            column.DataType = System.Type.GetType("System.Double");
                            break;
                        default:
                            break;
                    }
                    newTable.Columns.Add(column);
                }
            }
        }

    }
}
