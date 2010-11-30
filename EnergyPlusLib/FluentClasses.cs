using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using Iesi.Collections;
using System.Reflection;
using System.Data.SQLite;
using System.ComponentModel;
/*To-Do
 * 1. Fix Construction.
 * 3. read in gbXMl geomtry. 
 */


namespace EnergyPlusLib
{
    //IDD Support Classes.
    public class ObjectSwitch
    {
        #region Properties
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual Object Object { get; set; }
        #endregion
        #region Constructors
        public ObjectSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
        public ObjectSwitch()
        {
        }
        #endregion
    }
    public class FieldSwitch
    {
        #region Properties
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual Field Field { get; set; }
        #endregion
        #region Constructors
        public FieldSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
        public FieldSwitch() { }
        #endregion
    }
    public class Field
    {
        #region Properties
        public virtual String DataName { get; private set; }
        public virtual int Order { get; set; }
        public virtual Object Object { get; set; }
        public virtual IList<FieldSwitch> Switches { get; private set; }
        public List<Object> ObjectListTypeChoices = null;
        #endregion
        #region Constructors
        public Field(string DataName, int Order, Object Object)
            : this()
        {

            this.DataName = DataName;
            this.Order = Order;
            this.Object = Object;

        }

        public Field()
        {
            Switches = new List<FieldSwitch>();
            ObjectListTypeChoices = new List<Object>();
        }

        #endregion
        #region General Methods
        public virtual bool UpdateRelationships()
        {
            //Add all the Field types that could be used to populate this field if needed. 
            string ObjectList = this.ObjectList();
            if (ObjectList != null)
            {
                //ObjectListTypeChoices = IDD.GetObjectListReferences(ObjectList);
            }


            return true;
        }
        public FieldSwitch FindSwitch(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch;
            return result.FirstOrDefault();
        }
        public string FindSwitchValue(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            return result.FirstOrDefault();
        }
        public virtual void AddSwitch(FieldSwitch Switch)
        {
            this.Switches.Add(Switch);
        }
        #endregion
        #region Energyplus Field Switch Methods.


        public IList<string> FindSwitchValues(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            return result.ToList<String>();
        }

        public bool IsSwitchPresent(string switch_name)
        {
            bool value = false;
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            if (result.Count() > 0) value = true;
            return value;
        }

        public string Name()
        { return FindSwitchValue(@"\field"); }

        public IList<string> Notes()
        { return FindSwitchValues(@"\note"); }

        public bool IsRequiredField()
        { return IsSwitchPresent(@"\required-field"); }

        public string Units()
        { return FindSwitchValue(@"\units"); }

        public string IPUnits()
        { return FindSwitchValue(@"\ip-units"); }

        public string UnitsBasedOnField()
        { return FindSwitchValue(@"\unitsBasedOnField"); }

        public string RangeMinimum()
        { return FindSwitchValue(@"\minimum"); }

        public string RangeMaximum()
        { return FindSwitchValue(@"\maximum"); }

        public string RangeGreaterThan()
        { return FindSwitchValue(@"\minimum>"); }

        public string RangeLessThan()
        { return FindSwitchValue(@"\maximum<"); }

        public string Depreciated()
        { return FindSwitchValue(@"\deprecated"); }

        public string Default()
        { return FindSwitchValue(@"\default"); }

        public bool IsAutoSizable()
        { return IsSwitchPresent(@"\autosizable"); }

        public bool IsAutoCalculable()
        { return IsSwitchPresent(@"\autocalculatable"); }

        public string Type()
        { return FindSwitchValue(@"\type"); }

        public IList<string> Keys()
        { return FindSwitchValues(@"\key"); }

        public string ObjectList()
        { return FindSwitchValue(@"\object-list"); }


        public IList<string> References()
        { return FindSwitchValues(@"\reference"); }
        #endregion
    }
    public class Object
    {
        #region Properties
        public virtual string Name { get; set; }
        public virtual List<ObjectSwitch> Switches { get; set; }
        public virtual List<Field> RegularFields { get; set; }
        public virtual int NumberOfRegularFields { get; set; }
        public virtual List<Field> ExtensibleFields { get; set; }
        public virtual int NumberOfExtensibleFields { get; set; }
        public virtual string Group { get; set; }
        #endregion
        #region Constructors
        public Object()
        {

            this.Switches = new List<ObjectSwitch>();
            this.RegularFields = new List<Field>();
            this.ExtensibleFields = new List<Field>();
        }
        public Object(string Name, string Group)
            : this()
        {
            this.Name = Name;
            this.Group = Group;

        }
        #endregion
        # region General Methods.
        public virtual void AddSwitch(ObjectSwitch switch_pass)
        {
            Switches.Add(switch_pass);
        }
        public virtual void AddField(Field Field_pass)
        {
            RegularFields.Add(Field_pass);
        }
        public virtual void SortFields()
        {
            this.NumberOfRegularFields = this.RegularFields.Count();
            if (this.NumberOfExtensibleFields > 0)
            {

                this.NumberOfRegularFields = this.RegularFields.Count() - this.NumberOfExtensibleFields;
                if (this.NumberOfRegularFields == 0)
                {
                    FieldSwitch switch1 = this.RegularFields[0].FindSwitch(@"\field");
                    switch1.Value = Regex.Replace(switch1.Value, @" (1)", @" ");
                    this.ExtensibleFields.Add(this.RegularFields[0]);
                    this.RegularFields.RemoveAt(0);
                }
                else
                {
                    for (int iField = this.NumberOfRegularFields; iField <= (this.NumberOfRegularFields + this.NumberOfExtensibleFields) - 1; iField++)
                    {

                        FieldSwitch switch1 = this.RegularFields[iField].FindSwitch(@"\field");
                        switch1.Value = Regex.Replace(switch1.Value, @" (1)", @" ");

                        this.ExtensibleFields.Add(this.RegularFields[iField]);
                        //this.RegularFields.Remove(this.RegularFields[iField]);
                    }
                    this.RegularFields.RemoveRange(this.NumberOfRegularFields, this.NumberOfExtensibleFields);
                }

            }

        }
        public string FindSwitchValue(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            return result.FirstOrDefault();
        }
        public IList<string> FindSwitchValues(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            return result.ToList<String>();
        }
        #endregion
        #region IDD Switch Methods
        public bool IsSwitchPresent(string switch_name)
        {
            bool value = false;
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            if (result.Count() > 0) value = true;
            return value;
        }

        public IList<string> Memos()
        { return FindSwitchValues(@"\memo"); }

        public bool IsUniqueObject()
        { return IsSwitchPresent(@"\unique-object"); }

        public bool IsRequiredObject()
        { return IsSwitchPresent(@"\required-object"); }

        public string MinimumFields()
        { return FindSwitchValue(@"\min-fields"); }

        public bool IsObsolete()
        { return IsSwitchPresent(@"\required-object"); }

        public string ExtensibleNumber()
        { return FindSwitchValue(@"\extensible"); }

        public string Format()
        { return FindSwitchValue(@"\format"); }
        #endregion
    }

    //IDD DataModel.
    public class IDDDataModel
    {   
        #region Singleton Contructor
        private static IDDDataModel instance = new IDDDataModel();
        private IDDDataModel() { }
        public static IDDDataModel GetInstance()
        {
            return instance;
        }
        #endregion
        #region Properties
        public IList<Object> IDDObjects;
        Dictionary<string, List<Object>> IDDObjectLists = new Dictionary<string, List<Object>>();
        #endregion
        #region IDD Methods
        public List<Field> GetObjectListReferences(String Reference)
        {

            var q = (from object1 in IDDObjects.AsEnumerable()
                     from field1 in object1.RegularFields
                     from fieldswitch in field1.Switches
                     where fieldswitch.Name == @"\references"
                     select field1).Distinct();
            return q.ToList<Field>();
        }
        public Dictionary<string, List<Object>> GetObjectList()
        {

            var q = from object1 in IDDObjects.AsEnumerable()
                    from field1 in object1.RegularFields
                    from fieldswitch in field1.Switches
                    where fieldswitch.Name == @"\reference"
                    select new
                    {
                        obj = object1,
                        val = fieldswitch.Value,
                    };
            q.Distinct();
            foreach (var x in q)
            {
                if (!IDDObjectLists.ContainsKey(x.val))
                {
                    List<Object> objectlist = new List<Object>();
                    IDDObjectLists.Add(x.val, objectlist);
                }
                IDDObjectLists[x.val].Add(x.obj);
            }
            return IDDObjectLists;
        }
        public Object GetObject(string name)
        {
            var q = from object1 in IDDObjects
                    where object1.Name.ToLower() == name.ToLower()
                    select object1;
            return q.FirstOrDefault();
        }
        public void LoadIDDFile(string path)
        {

            IDDObjects = new List<Object>();

            List<String> FileAsString = File.ReadAllLines(path).ToList<string>();
            #region regex strings.
            // blank regex. 
            Regex blank = new Regex(@"^$");
            // object name regex.
            Regex object_regex = new Regex(@"^\s*(\S*),\s*$", RegexOptions.IgnoreCase);
            // Field start and switch.
            Regex field_regex = new Regex(@"^(\s*(A|N)\d*)\s*(\,|\;)\s*\\field\s(.*)?!?", RegexOptions.IgnoreCase);
            Regex extensible_regex = new Regex(@"^(.*):(\d*)");
            // switch. 
            Regex switch_regex = new Regex(@"^\s*(\\\S*)(\s*(.*)?)", RegexOptions.IgnoreCase);
            // group switch. 
            Regex group_regex = new Regex(@"^\s*(\\group)\s*(.*)$", RegexOptions.IgnoreCase);

            #endregion
            Object current_object = null;
            String current_group = null;
            String current_object_name = null;
            Field current_field = null;
            int iExtensible = 0;
            int field_counter = 0;
            bool bBeginExtensible = false;
            foreach (string line in FileAsString)
            {
                //Todo - Rewrite the field and maybe object so that only when a full list of object switches and all the field and field switches are present to add to the class. 



                //clear comments. 
                string newline = Regex.Replace(line, @"^(.*)(\!.*)$", "$1");
                //check if blank line
                Match match = blank.Match(newline);
                if (!match.Success)
                {
                    //Search for a /group match
                    Match group_match = group_regex.Match(newline);
                    if (group_match.Success)
                    {
                        //store group name. 
                        current_group = group_match.Groups[2].ToString().Trim();
                    }


                    //Search for a Object Match
                    Match object_match = object_regex.Match(newline);
                    if (object_match.Success)
                    {
                        //Found new object.
                        iExtensible = 0;
                        bBeginExtensible = false;
                        current_object_name = object_match.Groups[1].ToString().Trim();
                        //Create new object.
                        current_object = new Object(current_object_name, current_group);
                        IDDObjects.Add(current_object);
                        current_field = null;
                        field_counter = 0;
                    }


                    //Search for a Field Match
                    Match field_match = field_regex.Match(newline);
                    // I want to skip this if bBeginExtensible is true and iExtensible <=0
                    if (field_match.Success && !(bBeginExtensible == true && iExtensible <= 0))
                    {
                        //Found new field.
                        string field_data_name = field_match.Groups[1].ToString().Trim();
                        int field_position = field_counter++;
                        current_field = new Field(field_data_name, field_position, current_object);
                        current_object.AddField(current_field);


                        //Found First Switch. 
                        //Get rid of the "1" if it is an extensible field as to not confuse people. 
                        string field_switch = @"\field";
                        string field_switch_value = field_match.Groups[4].ToString().Trim();
                        current_field.AddSwitch(new FieldSwitch(field_switch, field_switch_value));

                        if (bBeginExtensible == true) iExtensible--;
                    }

                    //Search for a swtich match. 
                    Match switch_match = switch_regex.Match(newline);

                    //check if object switch. 
                    if (current_field == null && !(bBeginExtensible == true && iExtensible == 0) && (switch_match.Success && !group_match.Success))
                    {
                        //Since this is an object switch, save to object switch table. 

                        string object_switch_name;
                        string object_switch_value;
                        if (switch_match.Groups[1].ToString().Trim().Contains(@"\extensible"))
                        {
                            string temp = switch_match.Groups[1].ToString().Trim();
                            Match extensible_match = extensible_regex.Match(temp);

                            object_switch_name = extensible_match.Groups[1].ToString().Trim();
                            object_switch_value = extensible_match.Groups[2].ToString().Trim();
                            iExtensible = Convert.ToInt32(object_switch_value);
                            current_object.NumberOfExtensibleFields = iExtensible;
                        }
                        else
                        {
                            object_switch_name = switch_match.Groups[1].ToString().Trim();
                            object_switch_value = switch_match.Groups[2].ToString().Trim();
                        }
                        current_object.AddSwitch(new ObjectSwitch(object_switch_name, object_switch_value));
                    }

                    //Check if a field switch.
                    if (current_field != null && !(bBeginExtensible == true && iExtensible < 0) && (switch_match.Success && !group_match.Success))
                    {
                        //new field switch.
                        string field_switch = switch_match.Groups[1].ToString().Trim();
                        string field_switch_value = switch_match.Groups[2].ToString().Trim();
                        //Get rid of the "1" if it is an extensible field as to not confuse people. 
                        current_field.AddSwitch(new FieldSwitch(field_switch, field_switch_value));

                        if (field_switch == @"\begin-extensible")
                        {
                            current_object.NumberOfRegularFields = field_counter - 1;
                            bBeginExtensible = true;
                            iExtensible--;
                        }
                    }

                }
            }
            //Sort fields in all objects. 
            foreach (Object obj in IDDObjects) { obj.SortFields(); }
            GetObjectList();


            var q = from object1 in IDDObjects
                    from field in object1.RegularFields
                    select field.UpdateRelationships()
                    ;



        }
        #endregion
    }

    //IDF Support Classes. 
    public class Argument
    {
        #region Properties
        public Field Field;
        public string Value;
        public IDDDataModel idd;
        #endregion
        #region Constructor
        public Argument(Field field, string Value)
        {
            this.Field = field;
            this.Value = Value;
            idd = IDDDataModel.GetInstance();
        }
        #endregion
    }
    public class Command
    {
        #region Properties
        public Object Object;
        public virtual string UserComments { get; set; }
        public IList<Argument> RegularArguments { get; set; }
        public List<List<Argument>> ExtensibleSetArguments { get; set; }
        public bool IsMuted;
        IDDDataModel idd;
        #endregion
        #region Constructors
        public Command(Object Object)
            : this()
        {
            this.Object = Object;
            foreach (Field field in Object.RegularFields)
            {
                Argument arg = new Argument(field, field.Default());
                this.RegularArguments.Add(arg);
            }

            List<Argument> ExtensibleSet = new List<Argument>();
            foreach (Field field in this.Object.ExtensibleFields)
            {
                Argument arg = new Argument(field, field.Default());
                ExtensibleSet.Add(arg);
            }
            ExtensibleSetArguments.Add(ExtensibleSet);
        }
        private Command()
        {
            this.RegularArguments = new List<Argument>();
            this.ExtensibleSetArguments = new List<List<Argument>>();
            this.idd = IDDDataModel.GetInstance();
            this.IsMuted = false;
        }
        public Command(Object Object, params Argument[] Arguments)
            : this(Object)
        {
            //stub
        }
        public Command(string sCommand)
        {
            //stub
        }
        #endregion
        #region General Methods

        public List<Argument> FlattenedArgumentList()
        {
            List<Argument> ExtArgs = (from argumentsets in this.ExtensibleSetArguments
                                      from argument in argumentsets
                                      select argument).ToList<Argument>();

            List<Argument> FullArgs = new List<Argument>();
            FullArgs.AddRange(this.RegularArguments);
            FullArgs.AddRange(ExtArgs);
            return FullArgs;
        }
        public void AddArgument(Argument Argument)
        {
            //stub
        }
        public String ToIDFString()
        {
            string Prefix = "";
            if (this.IsMuted) Prefix = "!";

            string tempstring1 = Prefix + this.Object.Name + ",\r\n";
            List<Argument> FullList;
            FullList = FlattenedArgumentList();

            //Workaround for cosntruction types. 
            if (this.Object.Name.ToUpper() == "Construction".ToUpper()
                || this.Object.Name.ToUpper() == "ZoneControl:Thermostat".ToUpper())
            {
                FullList = new List<Argument>();
                FullList = (from arg in FlattenedArgumentList()
                            where arg.Value != null
                            select arg).ToList<Argument>();
            }
            
            foreach (Argument argument in FullList)
            {

                String units = argument.Field.Units();
                if (units != null)
                {
                    units = " {" + units + "}";
                }
                if (FullList.Last() == argument)
                {
                    tempstring1 += String.Format(Prefix + "    {0,-50} !-{1,-50} \r\n", argument.Value + ";", argument.Field.Name() + units);
                }
                else
                {
                    tempstring1 += String.Format(Prefix + "    {0,-50} !-{1,-50} \r\n", argument.Value + ",", argument.Field.Name() + units);
                }

            }
            return tempstring1;
        }

        public String ToIDFStringTerse()
        {
            string Prefix = "";
            if (this.IsMuted) Prefix = "!";

            string tempstring1 = Prefix + this.Object.Name + ",";
            List<Argument> FullList;
            FullList = FlattenedArgumentList();

            //Workaround for cosntruction types. 
            if (this.Object.Name.ToUpper() == "Construction".ToUpper()
                || this.Object.Name.ToUpper() == "ZoneControl:Thermostat".ToUpper())
            {
                FullList = new List<Argument>();
                FullList = (from arg in FlattenedArgumentList()
                            where arg.Value != null
                            select arg).ToList<Argument>();
            }

            foreach (Argument argument in FullList)
            {

                String units = argument.Field.Units();
                if (units != null)
                {
                    units = " {" + units + "}";
                }
                if (FullList.Last() == argument)
                {
                    tempstring1 += String.Format(Prefix + "{0}\r\n", argument.Value + ";");
                }
                else
                {
                    tempstring1 += String.Format(Prefix + "{0}", argument.Value + ",");
                }

            }
            return tempstring1;
        }



        public void SetArgument(String fieldname, String value)
        {
            List<Argument> Arguments = (from argument in this.FlattenedArgumentList()
                                        where argument.Field.Name() == fieldname
                                        select argument).ToList<Argument>();

            Arguments.ForEach(delegate(Argument s) { s.Value = value; });

        }
        public void SetArgumentbyDataName(String DataName, String value)
        {
            List<Argument> Arguments = (from argument in this.FlattenedArgumentList()
                                        where argument.Field.DataName == DataName
                                        select argument).ToList<Argument>();

            Arguments.ForEach(delegate(Argument s) { s.Value = value; });

        }
        #endregion
    }

    //IDF DataModel. 
    public class IDFDataModel
    {
        #region Properties
        public IDDDataModel idddata = IDDDataModel.GetInstance();
        public IList<Command> IDFCommands;

        public string sEnergyPlusRootFolder;
 

        private string sIDFFileName;


        Dictionary<string, List<Object>> IDDObjectLists = new Dictionary<string, List<Object>>();
        public IDDDataModel idd = IDDDataModel.GetInstance();
        #endregion
        #region Constructor
        public IDFDataModel()
        {
            this.IDFCommands = new List<Command>();
        }
        #endregion
        #region IDF Methods
        public void LoadIDFFile(string sIDFFileName)
        {
            this.sIDFFileName = sIDFFileName;
            IDFCommands.Clear();
            //Read file into string List. 
            List<String> idfListString = File.ReadAllLines(this.sIDFFileName).ToList<string>();

            //find command strings and reformat. 
            List<string> CommandStrings = CleanCommandStrings(idfListString);
            foreach (string commandstring in CommandStrings)
            {
                IDFCommands.Add(GetCommandFromTextString(commandstring));
            }
        }
        private List<string> CleanCommandStrings(List<String> idfListString)
        {
            List<string> CommandStrings = new List<String>();

            string tempstring = "";
            //Iterates through each line in in the array. 
            foreach (string line in idfListString)
            {
                //Remove comments. 
                string sline = line;
                sline = Regex.Replace(line, @"(^\s*.*)(!.*)", @"$1");

                //check if line is a blank or whitespace only.
                if (sline != "" || !Regex.IsMatch(sline, @"^\s*$"))
                {

                    // is this an , line. if true then
                    if (Regex.IsMatch(sline, @"^.*,\s*$"))
                    {
                        //Trim whitespace.
                        sline = sline.Trim();
                        //add to tempstring. 
                        tempstring = tempstring + sline;
                    }

                    //
                    if (Regex.IsMatch(sline, @"^.*;\s*$"))
                    {
                        //Trim whitespace.
                        sline = sline.Trim();
                        //add to tempstring. 
                        tempstring = tempstring + sline;
                        CommandStrings.Add(tempstring);
                        //reset temp string for next round. 
                        tempstring = "";


                    }
                }

            }
            return CommandStrings;
        }
        private Command GetCommandFromTextString(string tempstring)
        {

            Command new_command;
            //remove ; 
            tempstring = Regex.Replace(tempstring, @"(^.*)(;\s*$)", @"$1").Trim();
            //split along ,
            List<string> items = tempstring.Split(',').ToList<string>();

            //find object.
            Object object_type = idd.GetObject(items[0].Trim());
            new_command = new Command(object_type);
            //remove name from List.
            items.RemoveAt(0);

            //some values arguments are trailing optional. So to account for that. set the following to what is greater. 
            int limit = items.Count() < object_type.RegularFields.Count() ? items.Count() : object_type.RegularFields.Count();

            for (int itemcounter = 0; itemcounter < limit; itemcounter++)
            {
                new_command.RegularArguments[itemcounter].Value = items[itemcounter];
            }

            if (object_type.NumberOfExtensibleFields > 0)
            {
                //remove regular argument items from List. 
                items.RemoveRange(0, object_type.RegularFields.Count());
                //Clear existing sets if any. 
                new_command.ExtensibleSetArguments.Clear();
                for (int itemcounter = 0; itemcounter < items.Count; itemcounter = itemcounter + object_type.NumberOfExtensibleFields)
                {

                    List<Argument> ArgumentList = new List<Argument>();
                    for (int fieldcounter = 0; fieldcounter < object_type.NumberOfExtensibleFields; fieldcounter++)
                    {
                        ArgumentList.Add(new Argument(object_type.ExtensibleFields[fieldcounter], items[itemcounter + fieldcounter]));

                    }
                    new_command.ExtensibleSetArguments.Add(ArgumentList);
                }
            }

            return new_command;
        }
        public List<Command> FindCommandsFromObjectName(string ObjectName)
        {

            List<Command> commands = (from command in IDFCommands
                                      where command.Object.Name == ObjectName
                                      select command).ToList<Command>();
            return commands;
        }
        public void SaveIDFFile(string path)
        {
            TextWriter tw = new StreamWriter(path);

            foreach (Command command in IDFCommands)
            {
                tw.WriteLine(command.ToIDFStringTerse());
            }
            tw.Close();
        }
        public void DeleteCommands(string sObjectName)
        {
            FindCommandsFromObjectName(sObjectName).ForEach(delegate(Command cmd) { IDFCommands.Remove(cmd); });
        }
        public void ChangeSimulationPeriod(int startmonth, int startday, int endmonth, int endday)
        {
            //removes any exisiting entries of simulation control or Runperiod. 
            this.DeleteCommands("SimulationControl");
            this.DeleteCommands("RunPeriod");
            this.IDFCommands.Add(GetCommandFromTextString("SimulationControl,Yes,Yes,Yes,Yes,Yes;"));
            this.IDFCommands.Add(
                GetCommandFromTextString(
                String.Format(
                "RunPeriod,FullYear,{0},{1},{2},{3},UseWeatherFile,Yes,Yes,No,Yes,Yes,1;",
                startmonth, startday, endmonth, endday)
                ));
        }
        public bool ProcessEnergyPlusSimulation()
        {

            string idf_folder_path = Path.GetDirectoryName(this.sIDFFileName);
            string folder_name = idf_folder_path + @"\test";
            string lines = "[program]\r\ndir=" + sEnergyPlusRootFolder;
            string file_name = folder_name + "\\in2.idf";
            string ini_file_name = folder_name + "\\energy+.ini";
            if (System.IO.Directory.Exists(folder_name)) { System.IO.Directory.Delete(folder_name, true); }

            System.IO.Directory.CreateDirectory(folder_name);
            this.SaveIDFFile(file_name);

            System.IO.StreamWriter file = new System.IO.StreamWriter(ini_file_name);
            file.WriteLine(lines);
            file.Close();

            string startdirectory = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(folder_name);

            Process EPProcess = new Process();
            ProcessStartInfo EPStartInfo = new ProcessStartInfo();

            EPStartInfo.FileName = "CMD.exe ";

            EPStartInfo.RedirectStandardError = false;

            EPStartInfo.RedirectStandardInput = false;
            EPProcess.StartInfo.UseShellExecute = false;
            EPProcess.StartInfo.RedirectStandardOutput = true;


            EPStartInfo.UseShellExecute = false;
            //Dont show a command window
            EPStartInfo.CreateNoWindow = false;

            EPStartInfo.Arguments = "/D /c " + sEnergyPlusRootFolder + "RunEPlus.bat in2 USA_CA_San.Francisco.Intl.AP.724940_TMY3";

            EPProcess.EnableRaisingEvents = true;
            EPProcess.StartInfo = EPStartInfo;

            //start cmd.exe & the EP process
            EPProcess.Start();

            //set the wait period for exiting the process
            EPProcess.WaitForExit(1500000000); //or the wait time you want

            int ExitCode = EPProcess.ExitCode;
            bool EPSuccessful = true;

            //Now we need to see if the process was successful
            if (ExitCode > 0 & !EPProcess.HasExited)
            {
                EPProcess.Kill();
                EPSuccessful = false;
            }

            //now clean up after ourselves
            EPProcess.Dispose();
            //EPProcess.StartInfo = null;
            return EPSuccessful;
        }
        #endregion
    }
}




    

