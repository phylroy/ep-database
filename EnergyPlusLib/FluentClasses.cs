using System;
using System.Collections.Generic;
using FluentNHibernate;
using FluentNHibernate.Mapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NHibernate.ByteCode.Castle;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using Iesi.Collections;
using System.Reflection;
using System.Data.SQLite;

namespace EnergyPlusLib
{

    //IDD classes.
    public class ObjectSwitch
    {
        //table data.
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual Object Object { get; set; }
        public ObjectSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
        public ObjectSwitch()
        {
        }
    }
    public class FieldSwitch
    {
        //table data.
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual Field Field { get; set; }
        public FieldSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
        public FieldSwitch() { }
    }
    public class Field
    {
        //table data.
        public virtual String DataName { get; private set; }
        public virtual int Order { get; set; }
        public virtual Object Object { get; set; }
        public virtual IList<FieldSwitch> Switches { get; private set; }
        EPlusDataModel IDD = null;
        public IList<Field> ObjectListTypeChoices = null;

        public virtual bool UpdateRelationships()
        {
            //Add all the Field types that could be used to populate this field if needed. 
            string ObjectList = this.ObjectList();
            if (ObjectList != null)
            {

                ObjectListTypeChoices = IDD.GetObjectListReferences(ObjectList);
            }


            return true;
        }


        #region Energyplus Field switch types.

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

        public Field(string DataName, int Order, Object Object)
            : this()
        {
            IDD = EPlusDataModel.GetInstance();
            this.DataName = DataName;
            this.Order = Order;
            this.Object = Object;

        }

        public Field()
        {
            Switches = new List<FieldSwitch>();
            ObjectListTypeChoices = new List<Field>();
        }


        public virtual void AddSwitch(FieldSwitch Switch)
        {
            this.Switches.Add(Switch);
        }

    }
    public class Object
    {
        //table data. 
        public virtual string Name { get; set; }
        public virtual IList<ObjectSwitch> Switches { get; set; }
        public virtual List<Field> Fields { get; set; }
        public virtual string Group { get; set; }
        public Object(string Name, string Group)
        {
            this.Name = Name;
            this.Group = Group;
            this.Switches = new List<ObjectSwitch>();
            this.Fields = new List<Field>();
        }


        public Object()
        {
            EPlusDataModel IDD = EPlusDataModel.GetInstance();
            this.Switches = new List<ObjectSwitch>();
            this.Fields = new List<Field>();
        }


        public virtual void AddSwitch(ObjectSwitch switch_pass)
        {
            Switches.Add(switch_pass);
        }

        public virtual void AddField(Field Field_pass)
        {
            Fields.Add(Field_pass);
        }

        //General Methods.
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

    }

    //IDF Classes. 
    public class Argument
    {
        public Field Field;
        public string Value;

        public Argument(Field field, string Value)
        {

            this.Field = field;
            this.Value = Value;

        }

    }
    public class Command
    {
        public Object Object;
        public virtual string UserComments { get; set; }
        //IDF Arguments. 
        public IList<Argument> Arguments { get; set; }

        public List<List<Argument>> Extensibles { get; set; }

        public Command(Object Object) {

            this.Arguments = new List<Argument>();
            this.Object = Object;
            this.Extensibles = new List<List<Argument>>();
        
        }

        public void AddArgument(Argument Argument)
        {

        }

        public void AddExtensible(params Argument[] Arguments){} 


    }

    //EPlus Methods. 
    public class EPlusDataModel
    {
        private static EPlusDataModel instance = new EPlusDataModel();
        public IList<Object> IDDObjects;
        public IList<Command> IDFCommands;
        
        Dictionary<string, List<Object>> IDDObjectLists = new Dictionary<string, List<Object>>();



        public static EPlusDataModel GetInstance()
        {
            return instance;
        }
        private EPlusDataModel() {}


        public List<Field> GetObjectListReferences(String Reference)
        {

            var q = (from object1 in IDDObjects.AsEnumerable()
                     from field1 in object1.Fields
                     from fieldswitch in field1.Switches
                     where fieldswitch.Name == @"\references"
                     select field1).Distinct();
            return q.ToList<Field>();
        }
        public Dictionary<string,List<Object>> GetObjectList()
        {

            var q = from object1 in IDDObjects.AsEnumerable()
                    from field1 in object1.Fields
                    from fieldswitch in field1.Switches
                    where fieldswitch.Name == @"\reference"
                    select new
                    {
                        obj = object1,
                        val = fieldswitch.Value,
                    };
                     q.Distinct();
            foreach ( var x in q )
            {
               if ( !IDDObjectLists.ContainsKey(x.val) )
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
                //clear comments. 
                string newline = Regex.Replace(line, @"^(.*)(\!.*)$", "$1");
                //check if blank line
                Match match = blank.Match(newline);
                if (!match.Success)
                {
                    Match group_match = group_regex.Match(newline);
                    if (group_match.Success)
                    {
                        //store group name. 
                        current_group = group_match.Groups[2].ToString().Trim();
                    }

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
                        string field_switch = @"\field";
                        string field_switch_value = field_match.Groups[4].ToString().Trim();

                        //Get rid of the "1" if it is an extensible field as to not confuse people. 

                        field_switch_value = Regex.Replace(field_switch_value, @" (1)", @" ");

                        current_field.AddSwitch(new FieldSwitch(field_switch, field_switch_value));

                        if (bBeginExtensible == true) iExtensible--;
                    }
                    Match switch_match = switch_regex.Match(newline);
                    if (switch_match.Success && !group_match.Success)
                    {
                        //found switch. 
                        //check if object switch. 
                        if (current_field == null && !(bBeginExtensible == true && iExtensible == 0))
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
                            }
                            else
                            {
                                object_switch_name = switch_match.Groups[1].ToString().Trim();
                                object_switch_value = switch_match.Groups[2].ToString().Trim();
                            }
                            current_object.AddSwitch(new ObjectSwitch(object_switch_name, object_switch_value));
                        }
                        if (current_field != null && !(bBeginExtensible == true && iExtensible < 0))
                        {
                            //new field switch.
                            string field_switch = switch_match.Groups[1].ToString().Trim();
                            string field_switch_value = switch_match.Groups[2].ToString().Trim();
                            //Get rid of the "1" if it is an extensible field as to not confuse people. 
                            field_switch_value = Regex.Replace(field_switch_value, @" (1)", @" ");

                            current_field.AddSwitch(new FieldSwitch(field_switch, field_switch_value));
                            if (field_switch == @"\begin-extensible")
                            {
                                bBeginExtensible = true;
                                iExtensible--;
                            }
                        }
                    }
                }
            }

            var q = from object1 in IDDObjects
                    from field in object1.Fields
                    select field.UpdateRelationships()
                    ;



        }
        public void LoadIDFFile(string path)
        {
            IDFCommands = new List<Command>();

            //Read file into string List. 
            List<String> idfListString = File.ReadAllLines(path).ToList<string>();

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
                        Object new_object = GetObject(object_name);
                        //get number of fields 
                        int NumberOfFields = new_object.Fields.Count();
                        //get number of extensible fields.
                        int NumberOfExtensible = Convert.ToInt16(new_object.ExtensibleNumber());
                        //get the min num of fields. 
                        int MinNumOfFields = Convert.ToInt16(new_object.MinimumFields());
                        //get Field_ids from object_id. 
                        List<Field> Fields = new_object.Fields;

                        //Add command to Array.
                        Command new_command = new Command(new_object);
                        IDFCommands.Add(new_command);


                        //add row to its datatable
                        //TODO have non-extensible row items to the table and add the extensible item added to the extensible table with command_id. 

                        int iFieldCount = 0;
                        if (NumberOfExtensible == 0)
                        {
                            iFieldCount = items.Length;
                        }
                        else
                        {
                            iFieldCount = NumberOfFields - NumberOfExtensible + 1;
                        }

                        for (int i = 1; i < iFieldCount; i++)
                        {
                            Argument argument = new Argument(Fields[i - 1], items[i]);
                            new_command.Arguments.Add(argument);


                        }
                        //now add extensible. 
                        if (NumberOfExtensible != 0)
                        {
                            //divide number of repeated fields by the amount of items remaining. 
                            int itemsRemaining = items.Length - iFieldCount;
                            int NumberOfGroups = itemsRemaining / NumberOfExtensible;

                            for (int i = 0; i < NumberOfGroups; i++)
                            {
                                List<Argument> list = new List<Argument>(); 

                                new_command.Extensibles.Add(list);
                                for (int j = 1; j <= NumberOfExtensible; j++)
                                {

                                    int fieldIndex = NumberOfFields - NumberOfExtensible + j - 1;
                                    int itemIndex = NumberOfFields - NumberOfExtensible  + (i * NumberOfExtensible) + j;

                                    //Create new argument and add to Argument list. 
                                    Argument argument = new Argument(Fields[fieldIndex], items[itemIndex]);
                                    list.Add(argument);
                                    new_command.Arguments.Add(argument);

                                }
                            }
                        }
                        tempstring = "";
                    }
                }
            }


        }
        public void SaveIDFFile(string path)
        {
            TextWriter tw = new StreamWriter(path);

            foreach (Command command in IDFCommands)
            {
                string tempstring1 = command.Object.Name + ",\r\n";

                List<Argument> FullList = new List<Argument>();

                FullList.AddRange(command.Arguments);
                foreach (List<Argument> Arguments in command.Extensibles)
                {
                    FullList.AddRange(Arguments);
                }

                foreach (Argument argument in FullList)
                    {

                        String units = argument.Field.Units();
                        if (units != null)
                        {
                            units = " {" + units  + "}";
                        }


                        if (FullList.Last() == argument )
                        {
                            tempstring1 += String.Format("    {0,-50} !-{1,-50} \r\n", argument.Value + ";", argument.Field.Name() + units);
                        }
                        else
                        {
                            tempstring1 += String.Format("    {0,-50} !-{1,-50} \r\n", argument.Value + ",", argument.Field.Name() + units);
                        }

                    }
                       // tempstring1 += ";";
                        tw.WriteLine(tempstring1);
            }
                
            
            tw.Close();

        }

        //Commands
        public void CopyCommand(Command command){

        }
        public void AddNewCommand(Object Object){}
        public void DeleteCommand(Command command){}


    }
}




    

