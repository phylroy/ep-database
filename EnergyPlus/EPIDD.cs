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
    class EPIDD
    {
        //Members
        public DataSet IDD;
        DataTable objectsTable;
        DataTable objectsSwitchesTable;
        DataTable fieldsTable;
        DataTable fieldSwitchesTable;

        string[] FileAsString;

        //Methods to Create Databases.
        private static EPIDD instance = new EPIDD();
        public static  EPIDD GetInstance() { return instance; }
        public DataTable    ConvertToDataTable<T>(IEnumerable<T> varlist)
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
        private             EPIDD()
        {

            //Read file into a string. 
            FileAsString = File.ReadAllLines(@"C:\EnergyPlusV5-0-0\energy+.idd");

            IDD = new DataSet("DataDictionary");
            //Create Objects Table.
            objectsTable = IDD.Tables.Add("objects");
            DataColumn column = objectsTable.Columns.Add("object_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            objectsTable.Columns.Add("object_name", typeof(string));
            objectsTable.Columns.Add("group", typeof(string));

            //Create object Switches Table.
            objectsSwitchesTable = IDD.Tables.Add("object_switches");
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
            fieldsTable = IDD.Tables.Add("fields");
            column = fieldsTable.Columns.Add("field_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            fieldsTable.Columns.Add("object_id", System.Type.GetType("System.Int32"));
            fieldsTable.Columns.Add("field_name", typeof(string));
            fieldsTable.Columns.Add("field_position", System.Type.GetType("System.Int32"));

            //Create Object <-> Fields Relationship. 
            IDD.Relations.Add(new DataRelation("ObjectFields",
                IDD.Tables["objects"].Columns["object_id"],
                IDD.Tables["fields"].Columns["object_id"]));

            //Create Field switches table. 
            fieldSwitchesTable = IDD.Tables.Add("field_switches");
            column = fieldSwitchesTable.Columns.Add("field_switch_id", typeof(Int32));
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
        public void         storeUnits()
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
        public void         PopulateDatabase()
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
            int field_counter = 0;

            Regex extensible_regex = new Regex(@"^(.*):(\d*)");


            // switch. 
            Regex switch_regex = new Regex(@"^\s*(\\\S*)(\s*(.*)?)", RegexOptions.IgnoreCase);

            // group switch. 
            Regex group_regex = new Regex(@"^\s*(\\group)\s*(.*)$", RegexOptions.IgnoreCase);
            String current_group = "";
            int iExtensible = 0;
            bool bBeginExtensible = false;
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
                    if (object_match.Success )
                    {
                        //Found new object.

                        DataRow row = objectTable.Rows.Add();
                        iExtensible = 0; 
                        bBeginExtensible = false;
                        row["object_name"] = object_match.Groups[1].ToString().Trim();
                        row["group"] = current_group;
                        current_obj_id = (int)row["object_id"];
                        current_field_id = -1;
                        field_counter = 0;
                    }

                    Match field_match = field_regex.Match(line);
                    // I want to skip this if bBeginExtensible is true and iExtensible <=0
                    if (field_match.Success && !(bBeginExtensible == true && iExtensible <= 0) )
                    {

                        //Found new field.
                        DataRow row = fieldsTable.Rows.Add();
                        current_field_id = (int)row["field_id"];
                        row["field_name"] = field_match.Groups[4].ToString().Trim();
                        row["object_id"] = current_obj_id;
                        row["field_position"] = field_counter++;

                        DataRow switchrow = IDD.Tables["field_switches"].Rows.Add();
                        switchrow["field_id"] = current_field_id;
                        switchrow["field_switch"] = @"\field";
                        switchrow["field_switch_value"] = field_match.Groups[4].ToString().Trim();
                        if ( bBeginExtensible == true) iExtensible--;

                    }

                    Match switch_match = switch_regex.Match(line);
                    if (switch_match.Success && !group_match.Success)
                    {
                        //found switch. 
                        //check if object switch. 
                        if (current_field_id == -1 && !(bBeginExtensible == true && iExtensible <= 0))
                        {
                            //Since this is an object switch, save to object switch table. 

                                DataRow row = IDD.Tables["object_switches"].Rows.Add();
                                row["object_id"] = current_obj_id;
                                if (switch_match.Groups[1].ToString().Trim().Contains(@"\extensible"))
                                {
                                    string temp = switch_match.Groups[1].ToString().Trim();
                                    Match extensible_match = extensible_regex.Match(temp);
                                    //string valuestring = temp.Substring(position + 1, 2);

                                    row["object_switch"] = extensible_match.Groups[1].ToString().Trim();
                                    row["object_switch_value"] = extensible_match.Groups[2].ToString().Trim();
                                    iExtensible = Convert.ToInt32(row["object_switch_value"].ToString());
                                }
                                else
                                {
                                    row["object_switch"] = switch_match.Groups[1].ToString().Trim();
                                    row["object_switch_value"] = switch_match.Groups[2].ToString().Trim();
                                }
                        }
                        if (current_field_id != -1 && !(bBeginExtensible == true && iExtensible <= 0))
                        {
                            //Since this is an field switch, save to object switch table. 
                            DataRow row = IDD.Tables["field_switches"].Rows.Add();
                            row["field_id"] = current_field_id;
                            row["field_switch"] = switch_match.Groups[1].ToString().Trim();
                            row["field_switch_value"] = switch_match.Groups[2].ToString().Trim();
                            if ( row["field_switch"].ToString() == @"\begin-extensible")
                            {
                                bBeginExtensible = true;
                                iExtensible--;
                            }


                        }
                    }
                }
            }
        }
        public void CreateReferenceTables() 
        { 
        


        }
        //Object Query Functions
        public List<int> GetObjectIDList()
        {
            var query =
                from object1 in IDD.Tables["objects"].AsEnumerable()
                select object1.Field<Int32>("object_id");
            return EnumerableRowToListofInts(query);
        }

        public List<int> EnumerableRowToListofInts(EnumerableRowCollection<int> query)
        {
            List<int> returnval = new List<int>();
            foreach (int value in query)
            {
                returnval.Add(value);
            }
            return returnval;
        }
        public int          GetObjectIDFromObjectName(string name)
        {
            var query =
              from object1 in IDD.Tables["objects"].AsEnumerable()
              where object1.Field<String>("object_name") == name
              select object1.Field<Int32>("object_id");
            return query.First();

        }
        public int          GetObjectIDFromFieldId(int field_id)
        {
            var query =
            from field in IDD.Tables["fields"].AsEnumerable()
            where field.Field<Int32>("field_id") == field_id
            select field.Field<Int32>("object_id");
            return (int)query.First();
        }
        public string       GetObjectNameFromObjectID(int object_id)
        {
            var query =
      from object1 in IDD.Tables["objects"].AsEnumerable()
      where object1.Field<Int32>("object_id") == object_id
      select object1.Field<String>("object_name");
            return query.First();

        }
        public List<int>    GetFieldsIDsFromObjectID(int object_id)
        {
            var query =
            from object1 in IDD.Tables["objects"].AsEnumerable()
            join object_field in IDD.Tables["fields"].AsEnumerable()
            on object1.Field<Int32>("object_id") equals
            object_field.Field<Int32>("object_id")
            where object_field.Field<Int32>("object_id") == object_id
            select object_field.Field<Int32>("field_id");
            List<int> returnval = new List<int>();
            foreach (int value in query)
            {
                returnval.Add(value);
            }
            return returnval;
        }
        public int          GetNumberOfFieldsFromObjectID(int object_id)
        {

            //using get childrean may be faster than this. 
            var query =
            from object1 in IDD.Tables["objects"].AsEnumerable()
            join object_field in IDD.Tables["fields"].AsEnumerable()
            on object1.Field<Int32>("object_id") equals
            object_field.Field<Int32>("object_id")
            where object_field.Field<Int32>("object_id") == object_id
            select object_field.Field<Int32>("field_id");
            return query.Count();
            

        }
        public DataTable    GetObjectSwitchesFromObjectID(int object_id)
        {
            string sTableName = "object";
            return GetSwitchesFromID(sTableName,object_id);
        }
        public List<int>    GetObjectSwitcheIDsFromObjectID(int object_id)
        {
            string sTableName = "object";
            return GetSwitchIDsFromID(sTableName,object_id);
        }
        public int          GetObjectExtensibleNumber(int object_id)
        {
            int iExtensibleNumber = 0;
            DataRow row = objectsTable.Rows.Find(object_id);

            if (row == null) return iExtensibleNumber;

            DataRow[] rows = row.GetChildRows("object_switches");
            foreach(DataRow switchrow in rows) 
            {
                if (switchrow["object_switch"] == @"\extensible") 
                {
                    iExtensibleNumber = Convert.ToInt32(switchrow["object_switch_value"].ToString());
                }

            }
            
            return iExtensibleNumber;
                
        }


        //Field query Functions. 
        public DataTable    GetFieldSwitchesFromFieldID(int field_id)
        {
            string sTableName = "field";
            return GetSwitchesFromID(sTableName,field_id);
        }

        public List<int> GetFieldSwitchIDsFromFieldID(int field_id)
        {
            string sTableName = "field";
            return GetSwitchIDsFromID(sTableName, field_id);
        }

        public int          GetFieldPositionFromFieldID(int field_id)
        {
            var query =
            from field in IDD.Tables["fields"].AsEnumerable()
            where field.Field<Int32>("field_id") == field_id
            select field.Field<Int32>("field_position");
            return query.First(); ;
        }
        public string       GetFieldName(int field_id)
        {
            return GetFieldSwitchValues(field_id, @"\field").First();
        }
        public string       GetFieldType(int field_id)
        {
            if (IsFieldSwitchPresent(field_id, @"\type") )
            {
            return GetFieldSwitchValues(field_id, @"\type").First();
            }
        else 
        return null;
        }
        public List<string> GetFieldChoices(int field_id)
        {

            return GetFieldSwitchValues(field_id, @"\key");
        }
        public List<string> GetFieldNotes(int field_id)
        {
            return GetFieldSwitchValues(field_id, @"\note");
        }

        //Field Generic methods. 
        public bool         IsFieldSwitchPresent(int field_id, string switch_name)
        {
        
         List<string> values = GetFieldSwitchValues(field_id, switch_name);
         if (values.Count() == 0) { return false; } else { return true; }
        
        }
        public List<string> GetFieldSwitchValues(int field_id, string switch_name)
        {

            DataTable SwitchTable = GetFieldSwitchesFromFieldID(field_id);
            var query =
            from object1 in SwitchTable.AsEnumerable()
            where

             object1.Field<String>("field_switch") == switch_name
            select
                object1.Field<String>("field_switch_value");

            List<string> slChoices = new List<string>();
            foreach (var choice in query)
            {
                slChoices.Add(choice.ToString());
            }
            return slChoices;
        }
        private DataTable   GetSwitchesFromID(string sTableBaseName, int id)
        {
            var query =
            from object1 in IDD.Tables[sTableBaseName + "s"].AsEnumerable()
            join object_sw in IDD.Tables[sTableBaseName + "_switches"].AsEnumerable()
            on object1.Field<Int32>(sTableBaseName + "_id") equals
                object_sw.Field<Int32>(sTableBaseName + "_id")
            where object1.Field<Int32>(sTableBaseName + "_id") == id
            select new
            {
                field_switch = object_sw.Field<String>(sTableBaseName + "_switch"),
                field_switch_value = object_sw.Field<String>(sTableBaseName + "_switch_value")
            };

            return ConvertToDataTable(query);
        }
        private List<int>   GetSwitchIDsFromID(string sTableBaseName, int id)
        {

            string test = sTableBaseName + "_switch".ToString()+"_id".ToString();
            var query =
            from object1 in IDD.Tables[sTableBaseName + "s"].AsEnumerable()
            join object_sw in IDD.Tables[sTableBaseName + "_switches"].AsEnumerable()
            on object1.Field<Int32>(sTableBaseName + "_id") equals
                object_sw.Field<Int32>(sTableBaseName + "_id")
            where object1.Field<Int32>(sTableBaseName + "_id") == id
            select 
            object_sw.Field<Int32>(test);
            List<int> returnval = new List<int>();
            foreach (int value in query)
            {
                returnval.Add(value);
            }
            return returnval;
        }
        public void         writeIDDXML()
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
