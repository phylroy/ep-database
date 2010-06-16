using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
namespace EnergyPlus
{
    class EPIDD
    {
        private static EPIDD instance = new EPIDD();
        public static EPIDD GetInstance(){return instance;}

        public DataSet IDD;
        string[] FileAsString;
        private EPIDD()
        {
            //Read file into a string. 
            FileAsString = File.ReadAllLines(@"C:\EnergyPlusV5-0-0\energy+.idd");

            IDD = new DataSet("DataDictionary");
            //Create Objects Table.
            DataTable objectsTable = IDD.Tables.Add("objects");
            DataColumn column = objectsTable.Columns.Add("object_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            objectsTable.Columns.Add("object_name", typeof(string));
            objectsTable.Columns.Add("group", typeof(string));

            //Create object Switches Table.
            DataTable objectsSwitchesTable = IDD.Tables.Add("object_switches");
            column = objectsSwitchesTable.Columns.Add("object_switch_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            objectsSwitchesTable.Columns.Add("object_id", System.Type.GetType("System.Int32"));
            objectsSwitchesTable.Columns.Add("object_switch", typeof(string));
            objectsSwitchesTable.Columns.Add("object_switch_value", typeof(string));

            //Create Object <-> Switches Relationship.
            IDD.Relations.Add(new DataRelation("ObjectSwitches",
                IDD.Tables["objects"].Columns["object_id"],
                IDD.Tables["object_switches"].Columns["object_id"]));

            //Create Fields Table.
            DataTable fieldsTable = IDD.Tables.Add("fields");
            column = fieldsTable.Columns.Add("field_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            fieldsTable.Columns.Add("object_id", System.Type.GetType("System.Int32"));
            fieldsTable.Columns.Add("field_name", typeof(string));
            fieldsTable.Columns.Add("field_order", System.Type.GetType("System.Int32"));

            //Create Object <-> Fields Relationship. 
            IDD.Relations.Add(new DataRelation("ObjectFields", 
                IDD.Tables["objects"].Columns["object_id"], 
                IDD.Tables["fields"].Columns["object_id"]));

            //Create Field switches table. 
            DataTable fieldSwitchesTable = IDD.Tables.Add("field_switches");
            column = fieldSwitchesTable.Columns.Add("switch_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            fieldSwitchesTable.Columns.Add("field_id", System.Type.GetType("System.Int32"));
            fieldSwitchesTable.Columns.Add("field_switch", typeof(string));
            fieldSwitchesTable.Columns.Add("field_switch_value", typeof(string));

            //Create Field to Fieldswitches relationship.
            IDD.Relations.Add(new DataRelation("FieldSwitches", 
                IDD.Tables["fields"].Columns["field_id"], 
                IDD.Tables["field_switches"].Columns["field_id"]));

            PopulateDatabase();
        }


            

        public void storeUnits()
        {
            DataTable conversionTable = IDD.Tables.Add("conversion_units");
            conversionTable.Columns.Add("metric", System.Type.GetType("System.String"));
            conversionTable.Columns.Add("imperial", System.Type.GetType("System.String"));
            conversionTable.Columns.Add("factor", System.Type.GetType("System.Double"));
            string pat = @"^!\s*(\S*)\s*=>\s*(\S*)\s*(.*)$";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase);
            foreach (string line in FileAsString)
            {
                Match m = r.Match(line);
                //Console.WriteLine(line);
                if (m.Success)
                {
                    conversionTable.Rows.Add(m.Groups[1], m.Groups[2], Convert.ToDouble(m.Groups[3].ToString()));
                    Console.WriteLine(conversionTable);
                    Console.WriteLine(m.Groups[2]); 
                    Console.WriteLine(m.Groups[3]);
                }
            }
            
        }
        public List<string> removeComments()
        {
            List<string> tempString = new List<string>();
            foreach (string line in FileAsString)
            {
                tempString.Add(Regex.Replace(line, @"^(.*)(\!.*)$", "$1"));
            }

            return tempString;
        }
        public void PopulateDatabase()
        {
            List<string> StringList = removeComments();
        // blank regex. 
            Regex blank = new Regex(@"^$");

        // object name regex.
           Regex object_regex = new Regex(@"^\s*(\S*),\s*$", RegexOptions.IgnoreCase);
           DataTable objectTable = IDD.Tables["objects"];
           int current_obj_id = -1;

        // Field start and switch.
           Regex field_regex = new Regex(@"^(\s*(A|N)\d*)\s*(\,|\;)\s*\\field\s(.*)?!?", RegexOptions.IgnoreCase);
           DataTable fieldsTable = IDD.Tables["fields"];
           int current_field_id = -1;

        // switch. 
           Regex switch_regex = new Regex(@"^\s*(\\\S*)(\s*(.*)?)", RegexOptions.IgnoreCase);

        // group switch. 
           Regex group_regex = new Regex(@"^\s*(\\group)\s*(.*)$", RegexOptions.IgnoreCase);
           String current_group = "";
           foreach (string line in StringList)
           {
               //check if blank line
               Match match = blank.Match(line);
               if (!match.Success)
               {
                   Match group_match = group_regex.Match(line);
                   if (group_match.Success)
                   {
                       current_group = group_match.Groups[2].ToString().Trim();
                   }

                   Match object_match = object_regex.Match(line);
                   if (object_match.Success)
                   {
                       //Found new object.
                       DataRow row = objectTable.Rows.Add();
                       row["object_name"] = object_match.Groups[1].ToString().Trim();
                       row["group"] = current_group;
                       current_obj_id = (int)row["object_id"];
                       current_field_id = -1;
                   }

                   Match field_match = field_regex.Match(line);
                   if (field_match.Success)
                   {
                       //Found new field.
                       DataRow row = fieldsTable.Rows.Add();
                       current_field_id = (int)row["field_id"];
                       row["field_name"] = field_match.Groups[4].ToString().Trim();
                       row["object_id"] = current_obj_id;
                   }

                   Match switch_match = switch_regex.Match(line);
                   if (switch_match.Success && !group_match.Success)
                   {
                       //found switch. 
                       //check if object switch. 
                       if (current_field_id == -1) 
                       {
                           //Since this is an object switch, save to object switch table. 
                           DataRow row = IDD.Tables["object_switches"].Rows.Add();
                           row["object_id"] = current_obj_id;
                           row["object_switch"] = switch_match.Groups[1].ToString().Trim();
                           row["object_switch_value"] = switch_match.Groups[2].ToString().Trim();

                       }
                       else {
                           //Since this is an field switch, save to object switch table. 
                           DataRow row = IDD.Tables["field_switches"].Rows.Add();
                           row["field_id"] = current_field_id;
                           row["field_switch"] = switch_match.Groups[1].ToString().Trim();
                           row["field_switch_value"] = switch_match.Groups[2].ToString().Trim();
                       }
                   }
               }
           }
        }
        public void writeIDDXML()
        {
            foreach (DataTable table in IDD.Tables)
            {
                string filename = "C:\\test\\" + table.ToString().Replace(":", "-") + ".xml";
                table.WriteXml(filename);
                 filename = "C:\\test\\" + table.ToString().Replace(":", "-") + ".xsd";
                table.WriteXmlSchema(filename);
            }
        }

    }
}
