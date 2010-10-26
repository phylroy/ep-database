using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Reflection;

namespace EnergyPlusLib
{
    public class EPIDF
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

            //Create DataStructure to hold IDF file. 
            DataTable commandsTable = idfDataSet.Tables.Add("commands");
            DataColumn command_column = commandsTable.Columns.Add("command_id", typeof(Int32));
            command_column.AutoIncrement = true; command_column.Unique = true;
            DataColumn object_id_column = commandsTable.Columns.Add("object_id", typeof(Int32));



            foreach (DataRow objectRow in objects.Rows)
            {
                //create new table for all commands of object type. 
                DataTable newTable = idfDataSet.Tables.Add(objectRow["object_name"].ToString());
                //Add a column to keep the unique command identifier. 
                DataColumn command_column1 = newTable.Columns.Add("command_id", typeof(Int32));
                command_column1.Unique = true;

                //Create Command <-> Object Relationship.
                idfDataSet.Relations.Add(new DataRelation("CommandIDto" + objectRow["object_name"].ToString(),
                    idfDataSet.Tables["commands"].Columns["command_id"],
                    newTable.Columns["command_id"])
                    );

                DataRow[] fields = objectRow.GetChildRows(epidd.ObjectsToFieldsRelation);
                foreach (DataRow fieldRow in fields)
                {
                    DataRow[] fieldSwitchRows = fieldRow.GetChildRows("FieldSwitches");
                    DataColumn column = new DataColumn(fieldRow["field_name"].ToString());
                    //Console.WriteLine(objectRow["object_name"].ToString() + fieldRow["field_name"].ToString());
                    string datatype = null;
                    int field_id = (int)fieldRow["field_id"];
                    
                    foreach (DataRow row in fieldSwitchRows)
                    {

                        if (row[2].ToString() == @"\begin-extensible")
                        {
                            //Create new table to contain extensible data. 
                            newTable = idfDataSet.Tables.Add(objectRow["object_name"].ToString()+"_Extensible");
                            //Add command_id Column
                            newTable.Columns.Add("command_id", typeof(Int32));
                            //Create Command <-> Command_extensible Relationship.
                            idfDataSet.Relations.Add(new DataRelation("CommandIDto" + objectRow["object_name"].ToString()+"_extensible",
                                idfDataSet.Tables["commands"].Columns["command_id"],
                                newTable.Columns["command_id"])
                                );
                        }
                    }




                    foreach (DataRow row in fieldSwitchRows)
                    {
                        
                        if (row[2].ToString() == @"\type")
                        {
                            datatype = row[3].ToString(); 
                        }
                    }
                            
                    if (datatype == null) datatype = "alpha";

                            switch (datatype)
                            {
                                //Dump everything into a string since some selections can be string and number.
                                case "choice":
                                case "object-list":
                                case "alpha":
                                case "integer":
                                case "real":
                                    column.DataType = System.Type.GetType("System.String");
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
            
            // Reads and parses the file into string list. 
            List<string> idfListString = new List<string>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    idfListString.Add(line);          // Add to list.

                }
            }
            
            string tempstring = "";
            foreach (string line in idfListString)
            {
                //Remove comments. 
                string sline = line;
                sline = Regex.Replace(line, @"(^\s*.*)(!.*)", @"$1");

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
                        //find object name.
                        string object_name = items[0].Trim();
                        //find object id. 
                        int object_id = epidd.GetObjectIDFromObjectName(object_name);
                        int NumberOfFields = epidd.GetNumberOfFieldsFromObjectID(object_id);
                        int NumberOfExtensible = epidd.GetObjectExtensibleNumber(object_id);
                        int MinNumOfFields = epidd.GetObjectsMinNumOfFields(object_id);
                        //Add command to command table. 
                        DataRow command_row = idfDataSet.Tables["commands"].Rows.Add();
                        command_row["object_id"] = object_id; 


                        //add row to its datatable
                        //TODO have non-extensible row items to the table and add the extensible item added to the extensible table with command_id. 
                        DataTable table = idfDataSet.Tables[items[0].Trim()];
                        DataRow row = table.Rows.Add();
                        row["command_id"] = command_row["command_id"];
                        int iFieldCount = 0; 
                        if (NumberOfExtensible == 0)
                        {

                            iFieldCount = items.Length;

                        }
                        else
                        {
                            iFieldCount = NumberOfFields - NumberOfExtensible +1;
                        }

                        for (int i = 1; i < iFieldCount; i++)
                        {

                            row[i] = items[i];
                        }
                        tempstring = "";               
                    }
                }
            }
        }

        public void WriteIDFFile(string path)
        {
            TextWriter tw = new StreamWriter(path);

            foreach (DataTable table in idfDataSet.Tables)
            {
                if (table.TableName != "commands")
                {
                    foreach(DataRow row in table.Rows) 
                    {
                        string tempstring1 = table.TableName;
                        foreach (DataColumn col in table.Columns)
                        {
                            tempstring1 += ",\r\n      " + row[col].ToString();
                        }
                        tempstring1 += ";"; 
                        tw.WriteLine(tempstring1);
                    }
                }
            }
            tw.Close();

        }
    }
}