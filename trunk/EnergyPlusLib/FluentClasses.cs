using System;
using System.Collections.Generic;
using FluentNHibernate;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Reflection;

namespace EnergyPlusLib
{

    //IDD classes.
    public class ObjectSwitch
    {
        //table data.
        public virtual int Id { get; private set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }

        public ObjectSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }
    public class FieldSwitch
    {
        //table data.
        public virtual int Id { get; private set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }

        public FieldSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

    }
    public class Field
    {
        //table data.
        public virtual int Id { get; private set; }
        public virtual String DataName { get; private set; }
        public virtual int Order { get; set; }
        public virtual IList<FieldSwitch> Switches { get; private set; }

        //Functions.
        public string Name { get; set; }
        public string Notes { get; set; }
        public float RangeMaxValue { get; set; }
        public float RangeMinValue { get; set; }
        public float RangeGreaterThan { get; set; }
        public float RangeLessThan { get; set; }
        public List<string> Choices { get; set; }
        public string Type { get; set; }
        public string ObjectList { get; set; }
        public string Reference { get; set; }


        public Field(string DataName, int Order)
        {
            this.DataName = DataName;
            this.Order = Order;
            Switches = new List<FieldSwitch>();
        }

        public void AddSwitch(FieldSwitch Switch)
    {
        this.Switches.Add(Switch);
    }

    }
    public class Object
    {
        //table data. 
        public virtual int Id { get; private set; }
        public virtual string Name { get; set; } 
        public virtual IList<ObjectSwitch> Switches { get; set; }
        public virtual IList<Field> Fields { get; set; }
        public virtual string Group{ get; set; } 
        public Object(string Name, string Group)
        {
            this.Name = Name;
            this.Group = Group;
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

        //Methods
        public int ExtensibleNumber { get; set; }
        public bool IsRequiredObject { get; set; }
        public bool IsUnique { get; set; }
        public IList<Object> References { get; set; }
        public IList<Object> Dependancies { get; set; }

    }

    //IDF Classes. 
    public class Argument
    {
        public Field Field;
        public string Value { get; set; }

    }
    public class Command
    {
        public Object Object;
        public virtual int Id { get; private set; }
        public virtual string UserComments { get; set; }
        //IDF Arguments. 
        public virtual IList<Argument> Arguments { get; set; }
        //Extra Arguments that we may need for compliance or something else. 
        public virtual IList<Argument> EnhancedArguments { get; set; }
        
        //Methods
        public bool IsExtensible(){return true;}
        public int ExtensibleNumber() { return 1; }
        public int MinNumberOfFields() { return 1; }
        //public IList<Command> FindChildren();
        //public IList<Command> FindParents();

        public Command(Object Object) { }

        public void AddArgument(Argument Argument)
        {

        }

        public void AddEnhancedArgument(Argument Argument)
        {

        }

    }


    public class EPlus
    {

        public void ReadIDDFile()
        {
            List<Object> Objects = new List<Object>();

            string[] FileAsString = File.ReadAllLines(@"C:\EnergyPlusV5-0-0\energy+.idd");
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
            int current_field_id = -1;
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
                        Objects.Add(current_object);
                        current_field_id = -1;
                        field_counter = 0;
                    }
                    Match field_match = field_regex.Match(newline);
                    // I want to skip this if bBeginExtensible is true and iExtensible <=0
                    if (field_match.Success && !(bBeginExtensible == true && iExtensible <= 0))
                    {
                        //Found new field.
                        string field_data_name = field_match.Groups[1].ToString().Trim();
                        int field_position = field_counter++;
                        current_field = new Field(field_data_name, field_position);
                        current_object.AddField(current_field);


                        //Found First Switch. 
                        string field_switch = @"\field";
                        string field_switch_value = field_match.Groups[4].ToString().Trim();
                        current_field.AddSwitch(new FieldSwitch(field_switch,field_switch_value));

                        if (bBeginExtensible == true) iExtensible--;
                    }
                    Match switch_match = switch_regex.Match(newline);
                    if (switch_match.Success && !group_match.Success)
                    {
                        //found switch. 
                        //check if object switch. 
                        if (current_field_id == -1 && !(bBeginExtensible == true && iExtensible <= 0))
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
                            current_object.AddSwitch(new ObjectSwitch(object_switch_name,object_switch_value));
                        }
                        if (current_field_id != -1 && !(bBeginExtensible == true && iExtensible <= 0))
                        {
                            //new field switch.
                            string field_switch = switch_match.Groups[1].ToString().Trim();
                            string field_switch_value = switch_match.Groups[2].ToString().Trim();
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
        }
    }
}

