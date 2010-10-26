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
    public class EPIDD
    {
        //Members
        public DataSet IDD;
        

        DataTable fieldsTable;
        DataTable fieldSwitchesTable;

        //**********Datatable Groups
        const string GroupTableName = "groups";
        DataTable GroupsTable;

        const string GroupIDColumnHeader = @"group_id";
        DataColumn groups_group_id_column;

        const string GroupNameColumnHeader = @"group_name";
        DataColumn groups_group_name_column;



        //***********DataTable objects
        const string ObjectTableName = @"objects";
        DataTable ObjectsTable;

        //DataTable objects columns.
        const string ObjectIDColumnHeader = @"object_id";
        DataColumn objects_object_id_column;

        const string ObjectNameColumnHeader = @"object_name";
        DataColumn objects_object_name_column;

        //Uses same header as in the group table.
        DataColumn objects_group_id_column;


        //************DataTable for object switches.

        const string ObjectSwitchesTableName = "object_switches";
        DataTable ObjectsSwitchesTable;

        //DataTable objects columns.

        const string ObjectSwitchIDColumnHeader = @"object_switch_id";
        DataColumn object_switches_switch_id_column;

        //same header as in objects table for object_id.
        DataColumn object_switches_object_id_column;

        const string ObjectSwitchNameColumnHeader = @"object_switch";
        DataColumn object_switches_switch_name_column;

        const string ObjectSwitchValueColumnHeader = @"object_switch_value";
        DataColumn object_switches_switch_value_column;


        //DataRelations

        public DataRelation ObjectsToFieldsRelation;


        string[] FileAsString;

        //Methods to Create Databases.
        private static EPIDD instance = new EPIDD();
        public static  EPIDD GetInstance() { return instance; }
        
        private             EPIDD()
        {

            //Read file into a string. 
            FileAsString = File.ReadAllLines(@"C:\EnergyPlusV5-0-0\energy+.idd");

            IDD = new DataSet("DataDictionary");


            //Create Groups Table
            GroupsTable = IDD.Tables.Add(GroupTableName);

            groups_group_id_column = GroupsTable.Columns.Add(GroupIDColumnHeader, typeof(Int32));
            groups_group_name_column = GroupsTable.Columns.Add(GroupNameColumnHeader, typeof(String));
            groups_group_id_column.AutoIncrement = true; ; groups_group_id_column.Unique = true;
            GroupsTable.PrimaryKey = new DataColumn[] { groups_group_id_column };


            //Create Objects Table.
            ObjectsTable = IDD.Tables.Add(ObjectTableName);

            //Add Columns to the Objects Table
            objects_object_id_column = ObjectsTable.Columns.Add(ObjectIDColumnHeader, typeof(Int32));
            objects_object_name_column = ObjectsTable.Columns.Add(ObjectNameColumnHeader, typeof(String));

            objects_group_id_column = ObjectsTable.Columns.Add(GroupIDColumnHeader, typeof(Int32));
            //Set the primary key. 
            objects_object_id_column.AutoIncrement = true; ; objects_object_id_column.Unique = true;
            ObjectsTable.PrimaryKey = new DataColumn[] { objects_object_id_column };


            //Create Groups <-> Object Relationship. 
            IDD.Relations.Add(new DataRelation("GroupsToObjectsRelation",
                groups_group_id_column,objects_group_id_column
                ));


            //Create object Switches Table.
            ObjectsSwitchesTable = IDD.Tables.Add(ObjectSwitchesTableName);
            DataColumn column = ObjectsSwitchesTable.Columns.Add(ObjectSwitchIDColumnHeader, typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            ObjectsSwitchesTable.Columns.Add(ObjectIDColumnHeader, typeof(Int32));
            ObjectsSwitchesTable.Columns.Add(ObjectSwitchNameColumnHeader, typeof(string));
            ObjectsSwitchesTable.Columns.Add(ObjectSwitchValueColumnHeader, typeof(string));

            //Create Object <-> Switches Relationship.
            IDD.Relations.Add(new DataRelation("ObjectSwitches",
                ObjectsTable.Columns[ObjectIDColumnHeader],
                ObjectsSwitchesTable.Columns[ObjectIDColumnHeader]));

            //Create Fields Table.
            fieldsTable = IDD.Tables.Add("fields");
            column = fieldsTable.Columns.Add("field_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            fieldsTable.Columns.Add(ObjectIDColumnHeader, System.Type.GetType("System.Int32"));
            fieldsTable.Columns.Add("field_name", typeof(string));
            fieldsTable.Columns.Add("field_position", System.Type.GetType("System.Int32"));
            fieldsTable.Columns.Add("field_data_name", typeof(string));
            //Create Object <-> Fields Relationship. 
            ObjectsToFieldsRelation = new DataRelation("ObjectsToFieldsRelation",
                IDD.Tables["objects"].Columns[ObjectIDColumnHeader],
                IDD.Tables["fields"].Columns[ObjectIDColumnHeader]);

            IDD.Relations.Add(ObjectsToFieldsRelation);

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

                if (m.Success)
                {
                    conversionTable.Rows.Add(m.Groups[1], m.Groups[2], Convert.ToDouble(m.Groups[3].ToString()));

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
            int current_id = 0;
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
                        DataRow row = GroupsTable.Rows.Add();
                        row[groups_group_name_column] = current_group;
                        current_id =  Convert.ToInt32(row[groups_group_id_column]);
                    }

                    Match object_match = object_regex.Match(line);
                    if (object_match.Success )
                    {
                        //Found new object.

                        DataRow row = objectTable.Rows.Add();
                        iExtensible = 0; 
                        bBeginExtensible = false;
                        row[ObjectNameColumnHeader] = object_match.Groups[1].ToString().Trim();
                        row[GroupIDColumnHeader] = current_id;
                        current_obj_id = (int)row[ObjectIDColumnHeader];
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
                        row[ObjectIDColumnHeader] = current_obj_id;
                        row["field_data_name"] = field_match.Groups[1].ToString().Trim();
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

                                DataRow row = IDD.Tables[ObjectSwitchesTableName].Rows.Add();
                                row[ObjectIDColumnHeader] = current_obj_id;
                                if (switch_match.Groups[1].ToString().Trim().Contains(@"\extensible"))
                                {
                                    string temp = switch_match.Groups[1].ToString().Trim();
                                    Match extensible_match = extensible_regex.Match(temp);
                                    //string valuestring = temp.Substring(position + 1, 2);

                                    row[ObjectSwitchNameColumnHeader] = extensible_match.Groups[1].ToString().Trim();
                                    row[ObjectSwitchValueColumnHeader] = extensible_match.Groups[2].ToString().Trim();
                                    iExtensible = Convert.ToInt32(row[ObjectSwitchValueColumnHeader].ToString());
                                }
                                else
                                {
                                    row[ObjectSwitchNameColumnHeader] = switch_match.Groups[1].ToString().Trim();
                                    row[ObjectSwitchValueColumnHeader] = switch_match.Groups[2].ToString().Trim();
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
                select object1.Field<Int32>(ObjectIDColumnHeader);
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
              where object1.Field<String>(ObjectNameColumnHeader) == name
              select object1.Field<Int32>(ObjectIDColumnHeader);
            return query.First();

        }
        public int          GetObjectIDFromFieldId(int field_id)
        {
            var query =
            from field in IDD.Tables["fields"].AsEnumerable()
            where field.Field<Int32>("field_id") == field_id
            select field.Field<Int32>(ObjectIDColumnHeader);
            return (int)query.First();
        }
        public string       GetObjectNameFromObjectID(int object_id)
        {
            var query =
      from object1 in IDD.Tables["objects"].AsEnumerable()
      where object1.Field<Int32>(ObjectIDColumnHeader) == object_id
      select object1.Field<String>(ObjectNameColumnHeader);
            return query.First();

        }
        public List<int>    GetFieldsIDsFromObjectID(int object_id)
        {
            var query =
            from object1 in IDD.Tables["objects"].AsEnumerable()
            join object_field in IDD.Tables["fields"].AsEnumerable()
            on object1.Field<Int32>(ObjectIDColumnHeader) equals
            object_field.Field<Int32>(ObjectIDColumnHeader)
            where object_field.Field<Int32>(ObjectIDColumnHeader) == object_id
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
            on object1.Field<Int32>(ObjectIDColumnHeader) equals
            object_field.Field<Int32>(ObjectIDColumnHeader)
            where object_field.Field<Int32>(ObjectIDColumnHeader) == object_id
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
            DataRow row = ObjectsTable.Rows.Find(object_id);

            if (row == null) return iExtensibleNumber;

            DataRow[] rows = row.GetChildRows("ObjectSwitches");
            foreach(DataRow switchrow in rows) 
            {
                if (switchrow[ObjectSwitchNameColumnHeader].ToString() == @"\extensible") 
                {
                    iExtensibleNumber = Convert.ToInt32(switchrow[ObjectSwitchValueColumnHeader].ToString());
                }

            }
            
            return iExtensibleNumber;
                
        }

        public int GetObjectsMinNumOfFields(int object_id)
        {
            int iMinNumbOfFields = 0;
            DataRow row = ObjectsTable.Rows.Find(object_id);

            if (row == null) return iMinNumbOfFields;

            DataRow[] rows = row.GetChildRows("ObjectSwitches");
            foreach (DataRow switchrow in rows)
            {
                if (switchrow[ObjectSwitchNameColumnHeader].ToString() == @"\min-fields")
                {
                    iMinNumbOfFields = Convert.ToInt32(switchrow[ObjectSwitchValueColumnHeader].ToString());
                }

            }

            return iMinNumbOfFields;

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




        public void CreateReferenceListTable()

        {
            var query =
            from object1 in IDD.Tables["fields"].AsEnumerable()
            join object_sw in IDD.Tables["field_switches"].AsEnumerable()
            on object1.Field<Int32>("field_id") equals
                object_sw.Field<Int32>("field_id")
            where object_sw.Field<String>("field_switch") == @"\reference"
            select
            new
            {
                object_id = object1.Field<Int32>(ObjectIDColumnHeader),
                field_id = object_sw.Field<Int32>("field_id"),
                field_switch = object_sw.Field<String>("field_switch"),
                field_switch_value = object_sw.Field<String>("field_switch_value")
            };

            
            DataTable Table = ConvertToDataTable(query);


            var distinctValues = Table.AsEnumerable()
                        .Select(row => new
                        {
                            attribute1_name = row.Field<string>("field_switch_value")
                        })
                        .Distinct();
            DataTable Table2 = ConvertToDataTable(distinctValues);
            DataTable objectList = IDD.Tables.Add("object_lists");
            DataColumn column1 = objectList.Columns.Add("object_list_id",typeof(Int32));
            DataColumn[] keys = new DataColumn[1];
            keys[0] = column1;
            objectList.PrimaryKey = keys;
            column1.AutoIncrement = true; column1.Unique = true;
            objectList.Columns.Add("object_list_name", typeof(String));

            foreach( DataRow row in Table2.Rows)
            {
                string rtr = row[0].ToString();
                DataRow newrow = objectList.Rows.Add();
                newrow["object_list_name"] = row[0].ToString();
            }

            //Create Table.
            DataTable RefTable = IDD.Tables.Add("references");
            DataColumn column = RefTable.Columns.Add("reference_list_id", typeof(Int32));
            column.AutoIncrement = true; column.Unique = true;
            DataColumn[] key2 = new DataColumn[1];
            key2[0] = column;
            RefTable.PrimaryKey = key2;
            RefTable.Columns.Add("object_list_id", System.Type.GetType("System.Int32"));
            RefTable.Columns.Add("field_id", System.Type.GetType("System.Int32"));
            RefTable.Columns.Add(ObjectIDColumnHeader, System.Type.GetType("System.Int32"));

            IDD.Relations.Add(new DataRelation("ObjectListRelation",
                IDD.Tables["object_lists"].Columns["object_list_id"],
                IDD.Tables["references"].Columns["object_list_id"]));




            var query3 =
            from object1 in Table.AsEnumerable()
            join object_sw in IDD.Tables["object_lists"].AsEnumerable()
            on object1.Field<String>("field_switch_value") equals
                object_sw.Field<String>("object_list_name")
            select
            new
            {   
                object_id = object1.Field<Int32>(ObjectIDColumnHeader),
                field_id = object1.Field<Int32>("field_id"),
                object_list_id = object_sw.Field<Int32>("object_list_id"),
               
            };

            DataTable Table4 = ConvertToDataTable(query3);

            foreach (DataRow row11 in Table4.Rows)

            { 
                DataRow newrow = RefTable.Rows.Add();

                newrow[ObjectIDColumnHeader] = row11[ObjectIDColumnHeader];
                newrow["field_id"] = row11["field_id"];
                newrow["object_list_id"] = row11["object_list_id"];
            }

//object list



            var query7 =
            from field in IDD.Tables["fields"].AsEnumerable()
            join fieldswitches in IDD.Tables["field_switches"].AsEnumerable() 
            on field.Field<Int32>("field_id") equals fieldswitches.Field<Int32>("field_id")

            join objectlist in IDD.Tables["object_lists"].AsEnumerable()
            on fieldswitches.Field<string>("field_switch_value") equals objectlist.Field<string>("object_list_name")


            where fieldswitches.Field<String>("field_switch") == @"\object-list"
            select new
            {  
            object_id = field.Field<Int32>(ObjectIDColumnHeader),
            field_id = field.Field<Int32>("field_id"),
            field_switch_value = fieldswitches.Field<String>("field_switch_value"),
            object_list_id = objectlist.Field<Int32>("object_list_id")
            };

            DataTable Table5 = ConvertToDataTable(query7);


            //Create Table.
            DataTable DepTable = IDD.Tables.Add("dependancies");
            DataColumn column2 = DepTable.Columns.Add("dependance_id", typeof(Int32));
            column2.AutoIncrement = true; column.Unique = true;
            DataColumn[] key3 = new DataColumn[1];
            key3[0] = column2;
            DepTable.PrimaryKey = key3;
            DepTable.Columns.Add("object_list_id", System.Type.GetType("System.Int32"));
            DepTable.Columns.Add("field_id", System.Type.GetType("System.Int32"));
            DepTable.Columns.Add(ObjectIDColumnHeader, System.Type.GetType("System.Int32"));


            foreach (DataRow row11 in Table5.Rows)
            {
                DataRow newrow = DepTable.Rows.Add();
                newrow[ObjectIDColumnHeader]      = row11[ObjectIDColumnHeader];
                newrow["field_id"]       = row11["field_id"];
                newrow["object_list_id"] = row11["object_list_id"];
            }
        }




        public List<int> GetChildObjectIDs(int object_id){
        
        DataTable fields = IDD.Tables["fields"];
        DataTable objects = IDD.Tables["objects"];
        DataTable references = IDD.Tables["references"];
        DataTable object_lists = IDD.Tables["object_lists"];
        DataTable dependancies = IDD.Tables["dependancies"];

        //find all 
        var listofobjectlistsrt =
        (from reference in references.AsEnumerable()
        join dependacy in dependancies.AsEnumerable()
        on reference.Field<Int32>("object_list_id") equals dependacy.Field<Int32>("object_list_id")
        where reference.Field<Int32>(ObjectIDColumnHeader) == object_id
        select dependacy.Field<Int32>(ObjectIDColumnHeader)).Distinct();
        //Convert query to list of ints. 
        List<int> children = listofobjectlistsrt.ToList<int>();
        children.Sort();

        return children;
        
        }


        public List<string> GetChildObjectIDStrings(int object_id)
        {

            DataTable fields = IDD.Tables["fields"];
            DataTable objects = IDD.Tables["objects"];
            DataTable references = IDD.Tables["references"];
            DataTable object_lists = IDD.Tables["object_lists"];
            DataTable dependancies = IDD.Tables["dependancies"];

            //find all 
            var listofobjectlistsrt =
            (from reference in references.AsEnumerable()
             join dependacy in dependancies.AsEnumerable()
             on reference.Field<Int32>("object_list_id") equals dependacy.Field<Int32>("object_list_id")
             join object1 in objects.AsEnumerable()
             on dependacy.Field<Int32>(ObjectIDColumnHeader) equals object1.Field<Int32>(ObjectIDColumnHeader)
             where reference.Field<Int32>(ObjectIDColumnHeader) == object_id
             select object1.Field<String>(ObjectNameColumnHeader)).Distinct();
            //Convert query to list of ints. 
            List<string> children = listofobjectlistsrt.ToList<string>();
            children.Sort();

            return children;

        }


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


    }
}


