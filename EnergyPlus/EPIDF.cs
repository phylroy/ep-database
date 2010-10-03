using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Reflection;


namespace EnergyPlus
{
    class EPIDF
    {
        public EPIDD epidd;
        public DataSet idfDataSet;

        public DataTable ConvertToDataTable<T>(IEnumerable<T> varlist)
        {
            DataTable dtReturn = new DataTable();

            // column names   
            PropertyInfo[] oProps = null;

            if (varlist == null) return dtReturn;

            foreach (T rec in varlist)
            {
                // Use reflection to get property names, to create table, Only first time, others will follow   
                if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();

                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null ? DBNull.Value : pi.GetValue
                    (rec, null);
                }

                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }

        public EPIDF()
        {

            //Create a IDD object instance and read in the IDD file to create object and field switches.
            epidd = EPIDD.GetInstance();
            //Create a blank dataset for the idf datatables. 
            idfDataSet = new DataSet();
            //Create the datatables for each IDD table for easier access. 
            DataTable objects = epidd.IDD.Tables["objects"];
            DataTable objects_switches = epidd.IDD.Tables["object_switches"];
            DataTable objects_fields = epidd.IDD.Tables["fields"];
            DataTable objects_fields_switches = epidd.IDD.Tables["field_switches"];
            //List<string> test = epidd.GetChoices(1, 1, 1);



            //Create DataStructure to hold IDF file. 

            foreach (DataRow objectRow in objects.Rows)
            {
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


        public void ReadIDFFile(string path)
        {
            TextWriter tw = new StreamWriter(@"C:\date.txt");
            // Reads and parses the file into string list. 
            List<string> idfListString = new List<string>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    idfListString.Add(line);          // Add to list.
                    Console.WriteLine(line); // Write to console.
                }
            }
            
            string tempstring = "";
            foreach (string line in idfListString)
            {
                //Remove comments. 
                string sline = line;
                sline = Regex.Replace(line, @"(^\s*.*)(!.*)", @"$1");
                tw.WriteLine(sline);
                //check if line is a blank or whitespace only.
                if (sline != "" || !Regex.IsMatch(sline,@"^\s*$") )
                {

                    // is this an , line. if true then
                    if (Regex.IsMatch(sline, @"^.*,\s*$")) {
                        //Trim whitespace.
                        sline = sline.Trim();
                        //add to tempstring. 
                        tempstring = tempstring + sline;
                    }

                    if (Regex.IsMatch(sline, @"^.*;\s*$"))
                    {
                        //Trim whitespace.
                        sline = sline.Trim();
                        //remove ;
                        sline = Regex.Replace(sline, @"(^.*)(;\s*$)", @"$1").Trim();
                        //add to tempstring. 
                        tempstring = tempstring + sline;
                        //split along ,
                        string[] items = tempstring.Split(',');
                        //add row to its datatable
                        DataTable table = idfDataSet.Tables[items[0].Trim()];
                        DataRow row = table.Rows.Add();
                        for (int i = 1; i < items.Length; i++)
                        {
                            row[i - 1] = items[i];
                        }
                        tempstring = "";               
                    }
                }
            }
        }
    }
}