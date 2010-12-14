﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnergyPlusLib.DataModel.IDD;
using EnergyPlusLib.DataModel.IDF;

namespace EnergyPlusLib.DataAccess
{
    /// <summary>
    /// 
    /// </summary>
    public class IDFDatabase
    {
        #region Properties
        /// <summary>
        /// Place to store error involving this command. 
        /// </summary>
        public List<List<string>> ErrorList = new List<List<string>>();


        /// <summary>
        /// List container containing all the Commands in the Building. 
        /// </summary>
        private readonly IList<IDFCommand> IDFCommands;

        /// <summary>
        /// idd provides access to the idd class singleton.
        /// </summary>
        private readonly IDDDataBase idd = IDDDataBase.GetInstance();

        /// <summary>
        ///  The current IDF file name that was last loaded from or saved to. 
        /// </summary>
        public string CurrentIDFFilePath;

        /// <summary>
        ///  The current IDF file name that was last loaded from or saved to. 
        /// </summary>
        public List<string> OrigIDFFileStringsNoComments;


        /// <summary>
        /// Path to the energy plus root folder. "c:\energyplus6.0\" for example. 
        /// </summary>
        public string EnergyPlusRootFolder;

        /// <summary>
        /// This Dictionary contains all the arguments referenced by the object lists names.  
        /// </summary>
        public Dictionary<string, List<IDFArgument>> IDFObjectLists = new Dictionary<string, List<IDFArgument>>();

        /// <summary>
        /// The full path of the weather file used for the simulation. 
        /// </summary>
        public string WeatherFilePath;

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
            this.IDFObjectLists = new Dictionary<string, List<IDFArgument>>();
            foreach (IDFCommand command in this.IDFCommands)
            {
                foreach (IDFArgument argument in command.FlattenedArgumentList())
                {
                    foreach (string reference in argument.Field.References())
                    {
                        //using TrygetValue because it is faster. 
                        var ListOfArguments = new List<IDFArgument>();
                        if (false == this.IDFObjectLists.TryGetValue(reference, out ListOfArguments))
                        {
                            //Made the result equal to ListOfArguments so I don't have to do an array lookup again below. 
                            ListOfArguments = this.IDFObjectLists[reference] = new List<IDFArgument>();
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
            var ObjectListNames = new List<string>();
            //todo some output varible object lists are only present after a run. these start with a "autoRDD" prefix. 
            if (this.IDFObjectLists.ContainsKey(objectlistname))
            {
                List<IDFArgument> Arguments = this.IDFObjectLists[objectlistname];

                foreach (IDFArgument argument in Arguments)
                {
                    ObjectListNames.Add(argument.Value);
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
            this.IDFCommands.Clear();
            //Read file into string List. 
            IList<string> idfListString = File.ReadAllLines(this.CurrentIDFFilePath).ToList();

            //find command strings and reformat. 
            IList<string> CommandStrings = this.CleanCommandStrings(idfListString);
            foreach (string commandstring in CommandStrings)
            {
                IDFCommand command = this.GetCommandFromTextString(commandstring);

                this.IDFCommands.Add(command);
            }
            if (ErrorList.Count()>0)
            {
                StreamWriter readin_warnings = new StreamWriter(sIDFFileName+".readwarnings");
                foreach (List<string> errors in this.ErrorList)
                {
                    foreach(string error in errors)
                    {
                        readin_warnings.WriteLine(error);
                    }
                }
                readin_warnings.Close();
            }
        }

        /// <summary>
        /// This method takes a list of strings (from a file usually) and breaks it into easily digestible command strings if present. 
        /// It will remove all comments and non-relevant charecters from the string
        /// </summary>
        /// <param name="idfListString">A list of string containing raw file data.</param>
        /// <returns>A list of string with one command per string.</returns>
        public IList<string> CleanCommandStrings(IList<string> idfListString)
        {
           
            var CommandStrings = new List<string>();

            this.OrigIDFFileStringsNoComments = new List<string>();

            string tempstring = "";
            //Iterates through each line in the array. 
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
                    //Orginal file strings used for debug. 
                    OrigIDFFileStringsNoComments.Add(sline);
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
        /// <param name="inputstring">string data of a command, no comments or spaces.</param>
        /// <returns>A command object.</returns>
        public IDFCommand GetCommandFromTextString(string inputstring)
        {
            IDFCommand new_command;
            //remove ; 
            inputstring = Regex.Replace(inputstring, @"(^.*)(;\s*$)", @"$1").Trim();
            //split along ,
            List<string> items = inputstring.Split(',').ToList();

            //find object.
            IDDObject object_type = this.idd.GetObject(items[0].Trim());
            new_command = new IDFCommand(this, object_type);
            //remove name from List.
            items.RemoveAt(0);

            //some values arguments are trailing optional. So to account for that. set the following to what is greater. 
            int limit = items.Count() < object_type.RegularFields.Count()
                            ? items.Count()
                            : object_type.RegularFields.Count();

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
                for (int itemcounter = 0;
                     itemcounter < items.Count;
                     itemcounter = itemcounter + object_type.NumberOfExtensibleFields)
                {
                    var ArgumentList = new List<IDFArgument>();
                    for (int fieldcounter = 0; fieldcounter < object_type.NumberOfExtensibleFields; fieldcounter++)
                    {
                        string value;
                        try
                        {
                            // Some of the example file do not have full extensible sets. Energyplus seemd to ignore this. I guess the interface should 
                            // as well. 
                            //To-Do implement a warning that we are adding a empty "" argument. 
                            value = items[itemcounter + fieldcounter];
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            value = "";
                        }

                        ArgumentList.Add(new IDFArgument(this, object_type.ExtensibleFields[fieldcounter], value));
                    }
                    new_command.ExtensibleSetArguments.Add(ArgumentList);
                }
            }
            //test to see if they are the same. 
            string new_command_string = new_command.ToIDFStringTerse().ToUpper().Trim();
            string input_string_to_upper = inputstring.ToUpper() +";";
            if (input_string_to_upper != new_command_string)
            {
                List<string> error = new List<string>();
                error.AddRange(new string[] { "Read In Mismatch", input_string_to_upper, new_command_string });
                this.ErrorList.Add(error);
            }
                
           
            return new_command;
        }

        public IList<IDFCommand> FindCommandsFromObjectName(string ObjectName)
        {
            List<IDFCommand> commands = (from command in this.IDFCommands
                                         where command.Object.Name == ObjectName
                                         select command).ToList();
            return commands;
        }

        public void SaveIDFFile(string path)
        {
            TextWriter idffile = new StreamWriter(path);

            foreach (IDFCommand command in this.IDFCommands)
            {
                idffile.WriteLine(command.ToIDFString());
            }
            idffile.Close();
            this.CurrentIDFFilePath = path;

            TextWriter originalfile = new StreamWriter(path+".original");
            foreach (string line in this.OrigIDFFileStringsNoComments)
            {
                originalfile.WriteLine(line);
            }
            originalfile.Close();
            this.CurrentIDFFilePath = path;



        }

        public void DeleteCommands(string sObjectName)
        {
            foreach (IDFCommand cmd in this.FindCommandsFromObjectName(sObjectName))
            {
                this.IDFCommands.Remove(cmd);
            }
            this.UpdateAllObjectLists();
        }

        public void DeleteCommands(IList<IDFCommand> Commands)
        {
            foreach (IDFCommand cmd in Commands)
            {
                this.IDFCommands.Remove(cmd);
            }
            this.UpdateAllObjectLists();
        }


        //This should be the only way a command gets deleted.
        public void DeleteCommand(IDFCommand command)
        {
            if (this.IDFCommands.Contains(command))
            {
                this.IDFCommands.Remove(command);
            }
            ;
            this.UpdateAllObjectLists();
        }

        public void ChangeSimulationPeriod(int startmonth, int startday, int endmonth, int endday)
        {
            //removes any exisiting entries of simulation control or Runperiod. 
            
            this.DeleteCommands("RunPeriod");
            
            this.IDFCommands.Add(
                this.GetCommandFromTextString(
                    String.Format(
                        "RunPeriod,FullYear,{0},{1},{2},{3},UseWeatherFile,No,No,No,No,Yes,1;",
                        startmonth, startday, endmonth, endday)
                    ));
        }

        public void ChangeSimulationControl()
        {
            this.DeleteCommands("SimulationControl");
            this.IDFCommands.Add(this.GetCommandFromTextString("SimulationControl,Yes,Yes,Yes,Yes,Yes;"));
        }

        public void AddSQLiteOutput()
        {
            this.DeleteCommands("Output:SQLite");
            this.IDFCommands.Add(this.GetCommandFromTextString("Output:SQLite, SimpleAndTabular;"));   

        }

        public void ChangeAspectRatioXY(double X, double Y)
        {
            //Remove Geometry Transform if present. 
            this.DeleteCommands("GeometryTransform");
            this.IDFCommands.Add(
                this.GetCommandFromTextString(String.Format("GeometryTransform,XY,{0},{1}", X, Y))
                );
        }

        public IList<IDFCommand> FindAllCommandsOfObjectType(IDDObject object1)
        {
            List<IDFCommand> objects = (
                                           from command in this.IDFCommands
                                           where command.Object == object1
                                           select command).ToList();
            return objects;
        }


        public IList<IDFArgument> FindAllArgumentsOfFieldType(IDDField field1)
        {
            List<IDFArgument> args = (
                                         from command in this.IDFCommands
                                         from argument in command.FlattenedArgumentList()
                                         where argument.Field == field1
                                         select argument).Distinct().ToList();
            return args.ToList();
        }


        public IList<IDFCommand> FindCommands(string ObjectName, string FieldName, string FieldValue)
        {
            List<IDFCommand> objects = (from surface in this.FindCommandsFromObjectName(ObjectName)
                                        where
                                            surface.DoesArgumentExist(FieldName) &&
                                            surface.GetArgument(FieldName).Value == FieldValue
                                        select surface).ToList();
            return objects;
        }

        public IList<IDFCommand> FindCommandsWithRangeErrors()
        {
            this.UpdateAllObjectLists();
            IList<IDFCommand> Commands = new List<IDFCommand>();
            foreach (IDFCommand command in this.IDFCommands)
            {
                if (command.CheckValues())
                {
                    Commands.Add(command);
                }
            }
            return Commands;
        }

        public bool ProcessEnergyPlusSimulation()
        {
            bool DeleteFolder = false;

            //Get path of current idf file. 
            string idfFolderPath = Path.GetDirectoryName(this.CurrentIDFFilePath);
            //Create new folder to run simulation in. 
            string folder_name = idfFolderPath + @"\simrun\";
            if (Directory.Exists(folder_name))
            {
                if (DeleteFolder == true)
                {
                    Directory.Delete(folder_name, true);
                }
            }
            
            Directory.CreateDirectory(folder_name);

            //Save IDF file in memory to new folder. 
            string file_name = folder_name + Path.GetFileName(this.CurrentIDFFilePath);
            this.SaveIDFFile(file_name);

            //Save location of current folder and change dir to new folder. 
            string startdirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(folder_name);

            //Create new process 
            var EPProcess = new Process();
            var EPStartInfo = new ProcessStartInfo();
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
            EPStartInfo.Arguments = "/D /c " + this.EnergyPlusRootFolder + "RunEPlus.bat " + filen + " " +
                                    sWeatherfileNoExtention;
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
            Directory.SetCurrentDirectory(startdirectory);
            return EPSuccessful;
        }

        #endregion
    }
}