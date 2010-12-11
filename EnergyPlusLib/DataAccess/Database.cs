using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnergyPlusLib.DataModel.IDD;
using EnergyPlusLib.DataModel.IDF;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace EnergyPlusLib.DataAccess
{
    public class IDFDatabase
    {
        #region Properties
        private IDDDataBase idd = IDDDataBase.GetInstance();
        private IList<Command> IDFCommands;
        public string sEnergyPlusRootFolder;
        public string sIDFFileName;
        public string sWeatherFile;
        //Objectlist methods. 
        public Dictionary<string, List<Argument>> IDFObjectLists = new Dictionary<string, List<Argument>>();


        public void LoadIDDFile(string filename)

        {
           this.idd.LoadIDDFile(filename);
        }

        public void UpdateAllObjectLists()
        {
            IDFObjectLists = new Dictionary<string, List<Argument>>();
            foreach (Command command in this.IDFCommands)
            {
                foreach (Argument argument in command.FlattenedArgumentList())
                {
                    foreach (string reference in argument.Field.References())
                    {
                        //using TrygetValue because it is faster. 
                        List<Argument> value1 = new List<Argument>();
                        if (false == IDFObjectLists.TryGetValue(reference, out value1) )
                        {
                            value1 = IDFObjectLists[reference] = new List<Argument>();
                        }
                        
                        if (false == value1.Contains(argument))
                        {
                            value1.Add(argument);
                        }
                    }
                }
            }
        }





        public void UpdateObjectList(string objectlistname)
        {

            List<Argument> CommandList = new List<Argument>();
            //find all commands that are of this object type. 
            foreach (Field field in idd.IDDObjectLists[objectlistname])
            {
                CommandList.AddRange(this.FindAllArgumentsOfFieldType(field));
            }
            IDFObjectLists[objectlistname] = CommandList;
        }



        public List<string> GetFieldListArgumentNames(string objectlistname)
        {
            List<string> ObjectListNames = new List<string>();
            //todo some output varible object lists are only present after a run. these start with a "autoRDD" prefix. 
            if (IDFObjectLists.ContainsKey(objectlistname))
            {
                List<Argument> Arguments = this.IDFObjectLists[objectlistname];
                
                foreach (Argument argument in Arguments)
                {
                    ObjectListNames.Add( argument.Value );
                }
            }
            return ObjectListNames;
        }



        #endregion
        #region Constructor
        public IDFDatabase()
        {
            this.IDFCommands = new ObservableCollection<Command>();
        }
        #endregion
        #region IDF Methods
        public void LoadIDFFile(string sIDFFileName)
        {
            this.sIDFFileName = sIDFFileName;
            IDFCommands.Clear();
            //Read file into string List. 
            IList<String> idfListString = File.ReadAllLines(this.sIDFFileName).ToList<string>();

            //find command strings and reformat. 
            IList<string> CommandStrings = CleanCommandStrings(idfListString);
            foreach (string commandstring in CommandStrings)
            {
                IDFCommands.Add(GetCommandFromTextString(commandstring));
            }
        }
        private IList<string> CleanCommandStrings(IList<String> idfListString)
        {
            List<string> CommandStrings = new List<String>();

            string tempstring = "";
            //Iterates through each line in in the array. 
            foreach (string line in idfListString)
            {
                //Remove comments. 
                string sline = line;
                sline = Regex.Replace(line, @"(^\s*.*)(!.*)", @"$1");
                sline = Regex.Replace(sline, @"(^!.*)", @"");
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
            IDDObject object_type = idd.GetObject(items[0].Trim());
            new_command = new Command(this,object_type);
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
                        ArgumentList.Add(new Argument(this, object_type.ExtensibleFields[fieldcounter], items[itemcounter + fieldcounter]));

                    }
                    new_command.ExtensibleSetArguments.Add(ArgumentList);
                }
            }

            return new_command;
        }
        public IList<Command> FindCommandsFromObjectName(string ObjectName)
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
                tw.WriteLine(command.ToIDFString());
            }
            tw.Close();
        }
        public void DeleteCommands(string sObjectName)
        {
            foreach (Command cmd in FindCommandsFromObjectName(sObjectName))
            { this.IDFCommands.Remove(cmd);}
            this.UpdateAllObjectLists();
        }

        public void DeleteCommands(IList<Command> Commands)
        {

            foreach (Command cmd in Commands)
            { this.IDFCommands.Remove(cmd);}
            this.UpdateAllObjectLists();
        }


        //This should be the only way a command gets deleted.
        public void DeleteCommand(Command command)
        {
            if (this.IDFCommands.Contains(command)) 
            { this.IDFCommands.Remove(command); };
            this.UpdateAllObjectLists();
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
        public void ChangeAspectRatioXY(double X, double Y)
        {
            //Remove Geometry Transform if present. 
            this.DeleteCommands("GeometryTransform");
            this.IDFCommands.Add(
                GetCommandFromTextString(String.Format("GeometryTransform,XY,{0},{1}", X, Y))
                );
        }
        public IList<Command> FindAllCommandsOfObjectType(IDDObject object1)
        {
            List<Command> objects = (
                    from command in this.IDFCommands
                    where command.Object == object1
                    select command).ToList<Command>();
            return objects;
        }


        public IList<Argument> FindAllArgumentsOfFieldType(Field field1)
        {
            List<Argument> args = (
                    from command in this.IDFCommands
                    from argument in command.FlattenedArgumentList()
                    where argument.Field == field1
                    select argument).Distinct().ToList<Argument>();
            return args.ToList<Argument>();
        }


        public IList<Command> FindCommands(string ObjectName, string FieldName, string FieldValue)
        {

            List<Command> objects = (from surface in FindCommandsFromObjectName(ObjectName)
                                     where surface.DoesArgumentExist(FieldName) && surface.GetArgument(FieldName).Value == FieldValue
                                     select surface).ToList<Command>();
            return objects;
        }

        public IList<Command> FindCommandsWithRangeErrors()
        {
            this.UpdateAllObjectLists();
            IList<Command> Commands = new List<Command>();
            foreach (Command command in this.IDFCommands)
            {
                if (command.CheckValues() == true)
                {
                    Commands.Add(command);
                }
                
            }
            return Commands;
        }

        public bool ProcessEnergyPlusSimulation()
        {
            //Get path of current idf file. 
            string idf_folder_path = Path.GetDirectoryName(this.sIDFFileName);
            //Create new folder to run simulation in. 
            string folder_name = idf_folder_path + @"\simrun\";
            if (System.IO.Directory.Exists(folder_name)) { System.IO.Directory.Delete(folder_name, true); }
            System.IO.Directory.CreateDirectory(folder_name);

            //Save IDF file in memory to new folder. 
            string file_name = folder_name + Path.GetFileName(this.sIDFFileName);
            this.SaveIDFFile(file_name);

            //Save location of current folder and change dir to new folder. 
            string startdirectory = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(folder_name);

            //Create new process 
            Process EPProcess = new Process();
            ProcessStartInfo EPStartInfo = new ProcessStartInfo();
            EPStartInfo.FileName = "CMD.exe ";
            EPStartInfo.RedirectStandardError = false;
            EPStartInfo.RedirectStandardInput = false;
            EPProcess.StartInfo.UseShellExecute = false;
            EPProcess.StartInfo.RedirectStandardOutput = true;
            EPStartInfo.UseShellExecute = false;
            //Show the command window
            EPStartInfo.CreateNoWindow = false;

            //Set up E+ arguments. 
            string filen = folder_name + Path.GetFileNameWithoutExtension(this.sIDFFileName);
            string sWeatherfileNoExtention = Path.GetFileNameWithoutExtension(this.sWeatherFile);
            EPStartInfo.Arguments = "/D /c " + sEnergyPlusRootFolder + "RunEPlus.bat " + filen + " " + sWeatherfileNoExtention;
            EPProcess.EnableRaisingEvents = true;
            EPProcess.StartInfo = EPStartInfo;

            //start cmd.exe & the EP process
            EPProcess.Start();

            //set the wait period for exiting the process
            EPProcess.WaitForExit(150000000); //Roughly 1.73 days. 

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

            //Return to the start directory. 
            System.IO.Directory.SetCurrentDirectory(startdirectory);
            return EPSuccessful;
        }
        #endregion


    }
    //IDD DataModel.
    public class IDDDataBase
    {
        #region Singleton Contructor
        private static IDDDataBase instance = new IDDDataBase();
        private IDDDataBase() { }
        public static IDDDataBase GetInstance()
        {
            return instance;
        }
        #endregion
        #region Properties
        public IList<IDDObject> IDDObjects;
        public Dictionary<string, List<Field>> IDDObjectLists = new Dictionary<string, List<Field>>();
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
        public Dictionary<string, List<Field>> GetObjectList()
        {

            var q = from object1 in IDDObjects.AsEnumerable()
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
                if (!IDDObjectLists.ContainsKey(x.val))
                {
                    List<Field> objectlist = new List<Field>();
                    IDDObjectLists.Add(x.val, objectlist);
                }
                IDDObjectLists[x.val].Add(x.fld);
            }
            return IDDObjectLists;
        }
        public IDDObject GetObject(string name)
        {
            var q = from object1 in IDDObjects
                    where object1.Name.ToLower() == name.ToLower()
                    select object1;
            return q.FirstOrDefault();
        }
        public void LoadIDDFile(string path)
        {

            IDDObjects = new List<IDDObject>();

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
            IDDObject current_object = null;
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
                        current_object = new IDDObject(current_object_name, current_group);
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
                    else if (field_match.Success && (bBeginExtensible == true && iExtensible <= 0))
                    { iExtensible--; }

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
            foreach (IDDObject obj in IDDObjects) { obj.SortFields(); }
            GetObjectList();


            var q = from object1 in IDDObjects
                    from field in object1.RegularFields
                    select field.UpdateRelationships()
                    ;



        }
        #endregion
    }
}
