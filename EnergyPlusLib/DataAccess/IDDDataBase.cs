using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnergyPlusLib.DataModel.IDD;

namespace EnergyPlusLib.DataAccess
{
    //IDD DataModel.
    public class IDDDataBase
    {
        #region Singleton Contructor

        private static readonly IDDDataBase instance = new IDDDataBase();

        private IDDDataBase()
        {
        }

        public static IDDDataBase GetInstance()
        {
            return instance;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Contains the Dictionary with the key being the object-list name and value being the list of fields associated.
        /// </summary>
        public Dictionary<string, List<IDDField>> IDDObjectLists = new Dictionary<string, List<IDDField>>();
        /// <summary>
        /// List containing all Dictionary Objects. 
        /// </summary>
        public IList<IDDObject> IDDObjects;
        /// <summary>
        /// Contains the Dictionary with the key being the group name and the value being the Objects belonging to the group. 
        /// </summary>
        public Dictionary<string, List<IDDObject>> IDDGroupObjectLists;

        #endregion

        #region IDD Methods

        /// <summary>
        /// Creates the Objectlist and field references. 
        /// </summary>
        /// <returns></returns>
        private void GetObjectList()
        {
            var q = from object1 in this.IDDObjects.AsEnumerable()
                    from field1 in object1.FlattenedFieldList()
                    from fieldswitch in field1.Switches
                    where fieldswitch.Name == @"\reference"
                    select new
                               {
                                   fld = field1,
                                   val = fieldswitch.Value,
                               };
            q.Distinct();
            foreach (var x in q)
            {
                if (!this.IDDObjectLists.ContainsKey(x.val))
                {
                    var objectlist = new List<IDDField>();
                    this.IDDObjectLists.Add(x.val, objectlist);
                }
                this.IDDObjectLists[x.val].Add(x.fld);
            }

        }

        /// <summary>
        /// Return the IDDObject of by the string name passed as an argument.n For example passing "zone" will 
        /// return the zonal IDDObject.  
        /// </summary>
        /// <param name="name"> String name of IDDObject</param>
        /// <returns>The IDDObject from the given string name.</returns>
        public IDDObject GetObject(string name)
        {
            IEnumerable<IDDObject> q = from object1 in this.IDDObjects
                                       where object1.Name.ToLower() == name.ToLower()
                                       select object1;
            return q.FirstOrDefault();
        }


        /// <summary>
        /// Loads the IDD file and creates all IDDObjects defined in the idd file. 
        /// </summary>
        /// <param name="path">The full path of the idd file.</param>
        public void LoadIDDFile(string path)
        {
            this.IDDObjects = new List<IDDObject>();

            List<String> FileAsString = File.ReadAllLines(path).ToList();

            #region regex strings.

            // blank regex. 
            var blank = new Regex(@"^$");
            // object name regex.
            var object_regex = new Regex(@"^\s*(\S*),\s*$", RegexOptions.IgnoreCase);
            // Field start and switch.
            var field_regex = new Regex(@"^(\s*(A|N)\d*)\s*(\,|\;)\s*\\field\s(.*)?!?", RegexOptions.IgnoreCase);
            var extensible_regex = new Regex(@"^(.*):(\d*)");
            // switch. 
            var switch_regex = new Regex(@"^\s*(\\\S*)(\s*(.*)?)", RegexOptions.IgnoreCase);
            // group switch. 
            var group_regex = new Regex(@"^\s*(\\group)\s*(.*)$", RegexOptions.IgnoreCase);

            #endregion

            IDDObject current_object = null;
            String current_group = null;
            String current_object_name = null;
            IDDField current_field = null;
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
                        current_object = new IDDObject(current_object_name, current_group);
                        this.IDDObjects.Add(current_object);
                        current_field = null;
                        field_counter = 0;
                    }


                    //Search for a Field Match
                    Match field_match = field_regex.Match(newline);
                    // I want to skip this if bBeginExtensible is true and iExtensible <=0
                    if (field_match.Success && !(bBeginExtensible && iExtensible <= 0))
                    {
                        //Found new field.
                        string field_data_name = field_match.Groups[1].ToString().Trim();
                        int field_position = field_counter++;
                        current_field = new IDDField(field_data_name, field_position, current_object);
                        current_object.AddField(current_field);


                        //Found First Switch. 
                        //Get rid of the "1" if it is an extensible field as to not confuse people. 
                        string field_switch = @"\field";
                        string field_switch_value = field_match.Groups[4].ToString().Trim();
                        current_field.AddSwitch(new IDDFieldSwitch(field_switch, field_switch_value));

                        if (bBeginExtensible) iExtensible--;
                    }
                    else if (field_match.Success && (bBeginExtensible && iExtensible <= 0))
                    {
                        iExtensible--;
                    }

                    //Search for a swtich match. 
                    Match switch_match = switch_regex.Match(newline);

                    //check if object switch. 
                    if (current_field == null && !(bBeginExtensible && iExtensible == 0) &&
                        (switch_match.Success && !group_match.Success))
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
                        current_object.AddSwitch(new IDDObjectSwitch(object_switch_name, object_switch_value));
                    }

                    //Check if a field switch.
                    if (current_field != null && !(bBeginExtensible && iExtensible < 0) &&
                        (switch_match.Success && !group_match.Success))
                    {
                        //new field switch.
                        string field_switch = switch_match.Groups[1].ToString().Trim();
                        string field_switch_value = switch_match.Groups[2].ToString().Trim();
                        //Get rid of the "1" if it is an extensible field as to not confuse people. 
                        current_field.AddSwitch(new IDDFieldSwitch(field_switch, field_switch_value));

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
            foreach (IDDObject obj in this.IDDObjects)
            {
                obj.SortFields();
            }
            this.GetObjectList();


            IEnumerable<bool> q = from object1 in this.IDDObjects
                                  from field in object1.RegularFields
                                  select field.UpdateRelationships()
                ;

            this.CreateGroups();

        }

        /// <summary>
        /// Returns a list of all the groups defined in the idd file. 
        /// </summary>
        /// <returns></returns>
        public IList<string> GetGroups()
        {
            return this.IDDGroupObjectLists.Keys.ToList<string>(); ;
        }

        /// <summary>
        /// Returns all the IDDObjects belonging to a group.
        /// </summary>
        /// <param name="groupname">The string name of a group.</param>
        /// <returns>The IDDObjects which are a part of the group. </returns>
        public IList<IDDObject> GetObjectsInGroup(string groupname)
        {
            groupname = groupname.Trim().ToUpper();

            List<IDDObject> objects = (
                from object1 in IDDObjects
                where object1.Group.ToUpper() == groupname
                select object1).Distinct<IDDObject>().ToList<IDDObject>();
            return objects; 
        }

        /// <summary>
        /// Private method used to populate the dictionary of the groups and children IDDObjects. 
        /// </summary>
        private void CreateGroups()
        {

            this.IDDGroupObjectLists = new Dictionary<string, List<IDDObject>>();
            List<string> groups = (
                from object1 in IDDObjects
                select object1.Group).Distinct<string>().ToList<string>();

            foreach (string group1 in groups)
            {

                List<IDDObject> objects= (
                    from object1 in this.IDDObjects
                    where object1.Group == group1
                    select object1).Distinct<IDDObject>().ToList<IDDObject>();
                IDDGroupObjectLists.Add(group1,objects);

            }

        }

        #endregion
    }
}