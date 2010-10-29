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
        

            DataTable argumentsTable ;
            DataColumn args_command_id_column ;
            DataColumn args_argument_id_column ;
            DataColumn args_field_id_column;
            DataColumn args_argument_order_column;
            DataColumn args_object_id_column ;
            DataColumn args_argument_value_column;


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
            //epidd.IDD = new DataSet();
            //Create the datatables for each IDD table for easier access. 
            #region Pointers to tables. 
            DataTable objects = epidd.IDD.Tables["objects"];
            DataTable objects_switches = epidd.IDD.Tables["object_switches"];
            DataTable objects_fields = epidd.IDD.Tables["fields"];
            DataTable objects_fields_switches = epidd.IDD.Tables["field_switches"];
            #endregion
            #region Commands Table
            DataTable commandsTable = epidd.IDD.Tables.Add("commands");
            DataColumn command_column = commandsTable.Columns.Add("command_id", typeof(Int32));
            command_column.AutoIncrement = true; command_column.Unique = true;
            DataColumn object_id_column = commandsTable.Columns.Add("object_id", typeof(Int32));
            #endregion Commands Table.
            #region Arguments Table.
            argumentsTable = epidd.IDD.Tables.Add("arguments");
            args_command_id_column = argumentsTable.Columns.Add("command_id", typeof(Int32));
            args_argument_id_column = argumentsTable.Columns.Add("argument_id", typeof(Int32));
            args_argument_id_column.AutoIncrement = true; args_argument_id_column.Unique = true;
            args_argument_value_column = argumentsTable.Columns.Add("argument_value", typeof(String));
            args_argument_order_column = argumentsTable.Columns.Add("argument_order", typeof(Int32));
            args_field_id_column = argumentsTable.Columns.Add("field_id", typeof(Int32));
            args_object_id_column = argumentsTable.Columns.Add("object_id", typeof(Int32));
            #endregion

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
                        //get number of fields 
                        int NumberOfFields = epidd.GetNumberOfFieldsFromObjectID(object_id);
                        //get number of extensible fields.
                        int NumberOfExtensible = epidd.GetObjectExtensibleNumber(object_id);
                        //get the min num of fields. 
                        int MinNumOfFields = epidd.GetObjectsMinNumOfFields(object_id);
                        //get Field_ids from object_id. 
                        List<int> FieldIDs = epidd.GetFieldsIDsFromObjectID(object_id);

                        //Add command to command table. 
                        DataRow command_row = epidd.IDD.Tables["commands"].Rows.Add();
                        command_row["object_id"] = object_id; 

                        //add row to its datatable
                        //TODO have non-extensible row items to the table and add the extensible item added to the extensible table with command_id. 

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
                            DataRow Row2 = argumentsTable.Rows.Add();
                            Row2[args_command_id_column] = command_row["command_id"];
                            Row2[args_field_id_column] = FieldIDs[i-1];
                            Row2[args_argument_order_column] = i;
                            Row2[args_object_id_column] = object_id;
                            Row2[args_argument_value_column] = items[i];
                        }
                        tempstring = "";               
                    }
                }
            }
        }

        public void WriteIDFFile(string path)
        {
            TextWriter tw = new StreamWriter(path);

            foreach (DataTable table in epidd.IDD.Tables)
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