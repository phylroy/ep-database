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
    /// <summary>
    /// 
    /// </summary>
    public class IDFDatabase
    {
        #region Properties
        /// <summary>
        /// idd provides access to the idd class singleton.
        /// </summary>
        private IDDDataBase idd = IDDDataBase.GetInstance();
        /// <summary>
        /// List container containing all the Commands in the Building. 
        /// </summary>
        private IList<IDFCommand> IDFCommands;
        /// <summary>
        /// Path to the energy plus root folder. "c:\energyplus6.0\" for example. 
        /// </summary>
        public string EnergyPlusRootFolder;
        /// <summary>
        ///  The current IDF file name that was last loaded from or saved to. 
        /// </summary>
        public string CurrentIDFFilePath;
        /// <summary>
        /// The full path of the weather file used for the simulation. 
        /// </summary>
        public string WeatherFilePath;
        /// <summary>
        /// This Dictionary contains all the arguments referenced by the object lists names.  
        /// </summary>
        public Dictionary<string, List<IDFArgument>> IDFObjectLists = new Dictionary<string, List<IDFArgument>>();
        /// <summary>
        /// This method calls the private method from the IDDDatabase class and creates, or overwrites that data in the singleton. 
        /// </summary>
        /// <param name="filename"></param>
        public void LoadIDDFile(string filename)

        {
           this.idd.LoadIDDFile(filename);
        }
        /// <summary>
        /// This method parses all the commands and arguments in the building model and creates the 
        /// object-list -> arguments relationship within the IDFObjectLists dictionary. This is used 
        /// for range chacking and possibly for combox box control in a gui application. 
        /// </summary>
        public void UpdateAllObjectLists()
        {
            IDFObjectLists = new Dictionary<string, List<IDFArgument>>();
            foreach (IDFCommand command in this.IDFCommands)
            {
                foreach (IDFArgument argument in command.FlattenedArgumentList())
                {
                    foreach (string reference in argument.Field.References())
                    {
                        //using TrygetValue because it is faster. 
                        List<IDFArgument> ListOfArguments = new List<IDFArgument>();
                        if (false == IDFObjectLists.TryGetValue(reference, out ListOfArguments) )
                        {
                            //Made the result equal to ListOfArguments so I don't have to do an array lookup again below. 
                            ListOfArguments = IDFObjectLists[reference] = new List<IDFArgument>();
                        }
                        
                        if (false == ListOfArguments.Contains(argument))
                        {
                            ListOfArguments.Add(argument);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// This method will return the list of current options available for the passed object-list 
        /// string.  If there are no choices available, it will return an empty List container. 
        /// </summary>
        /// <param name="objectlistname"> The name of the object-list</param>
        /// <returns>The list of names of the arguments that are contained in the object-list.</returns>
        public List<string> GetFieldListArgumentNames(string objectlistname)
        {
            List<string> ObjectListNames = new List<string>();
            //todo some output varible object lists are only present after a run. these start with a "autoRDD" prefix. 
            if (IDFObjectLists.ContainsKey(objectlistname))
            {
                List<IDFArgument> Arguments = this.IDFObjectLists[objectlistname];
                
                foreach (IDFArgument argument in Arguments)
                {
                    ObjectListNames.Add( argument.Value );
                }
            }
            return ObjectListNames;
        }

        #endregion
        #region Constructor
        /// <summary>
        /// Constructor of the IDFDatabase class. 
        /// </summary>
        public IDFDatabase()
        {
            this.IDFCommands = new ObservableCollection<IDFCommand>();
        }
        #endregion
        #region IDF Methods

        /// <summary>
        /// This method Loads Existing IDF file into memory and set the current sIDFFile varible.  
        /// </summary>
        /// <param name="sIDFFileName">Path of exisiting IDF file.</param>
        public void LoadIDFFile(string sIDFFileName)
        {
            this.CurrentIDFFilePath = sIDFFileName;
            IDFCommands.Clear();
            //Read file into string List. 
            IList<String> idfListString = File.ReadAllLines(this.CurrentIDFFilePath).ToList<string>();

            //find command strings and reformat. 
            IList<string> CommandStrings = CleanCommandStrings(idfListString);
            foreach (string commandstring in CommandStrings)
            {
                IDFCommands.Add(GetCommandFromTextString(commandstring));
            }
        }

        /// <summary>
        /// This method takes a list of strings (from a file usually) and breaks it into easily digestible command strings if present. 
        /// It will remove all comments and non-relevant charecters from the string
        /// </summary>
        /// <param name="idfListString">A list of string containing raw file data.</param>
        /// <returns>A list of string with one command per string.</returns>
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
                sline = Regex.Replace(sline, @"(^\s*!.*)", @"");
                sline = Regex.Replace(sline, @"(^\s*Lead Input\s*;)", @"");
                sline = Regex.Replace(sline, @"(^\s*End Lead Input\s*;)", @"");
                sline = Regex.Replace(sline, @"(^\s*End Simulation Data\s*;)", @"");
                sline = Regex.Replace(sline, @"(^\s*Simulation Data\s*;)", @"");

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

        /// <summary>
        /// Converts a clean string into a command object. 
        /// </summary>
        /// <param name="tempstring">string data of a command, no comments or spaces.</param>
        /// <returns>A command object.</returns>
        private IDFCommand GetCommandFromTextString(string tempstring)
        {

            IDFCommand new_command;
            //remove ; 
            tempstring = Regex.Replace(tempstring, @"(^.*)(;\s*$)", @"$1").Trim();
            //split along ,
            List<string> items = tempstring.Split(',').ToList<string>();

            //find object.
            IDDObject object_type = idd.GetObject(items[0].Trim());
            new_command = new IDFCommand(this,object_type);
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

                    List<IDFArgument> ArgumentList = new List<IDFArgument>();
                    for (int fieldcounter = 0; fieldcounter < object_type.NumberOfExtensibleFields; fieldcounter++)
                    {
                        string value;
                        try
                        {// Some of the example file do not have full extensible sets. Energyplus seemd to ignore this. I guess the interface should 
                            // as well. 
                            //To-Do implement a warning that we are adding a empty "" argument. 
                            value = items[itemcounter + fieldcounter];
                        }
                        catch (System.ArgumentOutOfRangeException)
                        {
                            value = "";
                        }

                        ArgumentList.Add(new IDFArgument(this, object_type.ExtensibleFields[fieldcounter], value));

                    }
                    new_command.ExtensibleSetArguments.Add(ArgumentList);
                }
            }

            return new_command;
        }

        public IList<IDFCommand> FindCommandsFromObjectName(string ObjectName)
        {

            List<IDFCommand> commands = (from command in IDFCommands
                                      where command.Object.Name == ObjectName
                                      select command).ToList<IDFCommand>();
            return commands;
        }
        public void SaveIDFFile(string path)
        {
            TextWriter tw = new StreamWriter(path);

            foreach (IDFCommand command in IDFCommands)
            {
                tw.WriteLine(command.ToIDFString());
            }
            tw.Close();
        }
        public void DeleteCommands(string sObjectName)
        {
            foreach (IDFCommand cmd in FindCommandsFromObjectName(sObjectName))
            { this.IDFCommands.Remove(cmd);}
            this.UpdateAllObjectLists();
        }
        public void DeleteCommands(IList<IDFCommand> Commands)
        {

            foreach (IDFCommand cmd in Commands)
            { this.IDFCommands.Remove(cmd);}
            this.UpdateAllObjectLists();
        }


        //This should be the only way a command gets deleted.
        public void DeleteCommand(IDFCommand command)
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
        public IList<IDFCommand> FindAllCommandsOfObjectType(IDDObject object1)
        {
            List<IDFCommand> objects = (
                    from command in this.IDFCommands
                    where command.Object == object1
                    select command).ToList<IDFCommand>();
            return objects;
        }


        public IList<IDFArgument> FindAllArgumentsOfFieldType(IDDField field1)
        {
            List<IDFArgument> args = (
                    from command in this.IDFCommands
                    from argument in command.FlattenedArgumentList()
                    where argument.Field == field1
                    select argument).Distinct().ToList<IDFArgument>();
            return args.ToList<IDFArgument>();
        }


        public IList<IDFCommand> FindCommands(string ObjectName, string FieldName, string FieldValue)
        {

            List<IDFCommand> objects = (from surface in FindCommandsFromObjectName(ObjectName)
                                     where surface.DoesArgumentExist(FieldName) && surface.GetArgument(FieldName).Value == FieldValue
                                     select surface).ToList<IDFCommand>();
            return objects;
        }

        public IList<IDFCommand> FindCommandsWithRangeErrors()
        {
            this.UpdateAllObjectLists();
            IList<IDFCommand> Commands = new List<IDFCommand>();
            foreach (IDFCommand command in this.IDFCommands)
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
            string idf_folder_path = Path.GetDirectoryName(this.CurrentIDFFilePath);
            //Create new folder to run simulation in. 
            string folder_name = idf_folder_path + @"\simrun\";
            if (System.IO.Directory.Exists(folder_name)) { System.IO.Directory.Delete(folder_name, true); }
            System.IO.Directory.CreateDirectory(folder_name);

            //Save IDF file in memory to new folder. 
            string file_name = folder_name + Path.GetFileName(this.CurrentIDFFilePath);
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
            string filen = folder_name + Path.GetFileNameWithoutExtension(this.CurrentIDFFilePath);
            string sWeatherfileNoExtention = Path.GetFileNameWithoutExtension(this.WeatherFilePath);
            EPStartInfo.Arguments = "/D /c " + EnergyPlusRootFolder + "RunEPlus.bat " + filen + " " + sWeatherfileNoExtention;
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
        public Dictionary<string, List<IDDField>> IDDObjectLists = new Dictionary<string, List<IDDField>>();
        #endregion
        #region IDD Methods

        public Dictionary<string, List<IDDField>> GetObjectList()
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
                    List<IDDField> objectlist = new List<IDDField>();
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
                        current_field = new IDDField(field_data_name, field_position, current_object);
                        current_object.AddField(current_field);


                        //Found First Switch. 
                        //Get rid of the "1" if it is an extensible field as to not confuse people. 
                        string field_switch = @"\field";
                        string field_switch_value = field_match.Groups[4].ToString().Trim();
                        current_field.AddSwitch(new IDDFieldSwitch(field_switch, field_switch_value));

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
                        current_object.AddSwitch(new IDDObjectSwitch(object_switch_name, object_switch_value));
                    }

                    //Check if a field switch.
                    if (current_field != null && !(bBeginExtensible == true && iExtensible < 0) && (switch_match.Success && !group_match.Success))
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
