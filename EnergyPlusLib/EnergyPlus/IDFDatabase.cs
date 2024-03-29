﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnergyPlusLib;
using System.Data;

namespace EnergyPlusLib.EnergyPlus
{
    /// <summary>
    /// This class manages the E+ commands 
    /// </summary>
    public class IDFDatabase : BuildingDatabase
    {
        #region Properties


        /// <summary>
        /// List container containing all the Commands in the Building. 
        /// </summary>
        public IDFCommands IDFCommandList { get; set; }

        /// <summary>
        /// idd provides access to the idd class singleton.
        /// </summary>
        public readonly IDDDataBase idd = IDDDataBase.GetInstance();



        /// <summary>
        ///  The current IDF file name that was last loaded from or saved to. 
        /// </summary>
        public List<string> OrigIDFFileStringsNoComments;


        /// <summary>
        /// This Dictionary contains all the arguments referenced by the object lists names.  
        /// </summary>
        public Dictionary<string, List<IDFArgument>> IDFObjectLists = new Dictionary<string, List<IDFArgument>>();



        /// <summary>
        /// This method parses all the commands and arguments in the building model and creates the 
        /// object-list -> arguments relationship within the IDFObjectLists dictionary. This is used 
        /// for range chacking and possibly for combox box control in a gui application. 
        /// </summary>
        public void UpdateAllObjectLists()
        {
            this.IDFObjectLists = new Dictionary<string, List<IDFArgument>>();
            foreach (IDFCommand command in this.IDFCommandList)
            {
                foreach (IDFArgument argument in command.FlattenedArgumentList)
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

        #region Constructors

        /// <summary>
        /// Constructor of the IDFDatabase class. 
        /// </summary>
        public IDFDatabase()
        {
            this.InputFileExtention = ".idf";
            this.ResultFileExtention = ".sql";
            this.SimulationExecutable = "RunEPlus.bat ";
            this.IDFCommandList = new IDFCommands();
        }

        #endregion

        #region Methods
        #region File I/O
        /// <summary>
        /// This method Loads Existing IDF file into memory and set the current sIDFFile varible.  
        /// </summary>
        /// <param name="sIDFFileName">Path of exisiting IDF file.</param>
        public void LoadInputFile(string sIDFFileName)
        {
            this.CurrentInputFilePath = sIDFFileName;
            this.IDFCommandList.Clear();
            //Read file into string List. 
            IList<string> idfListString = File.ReadAllLines(this.CurrentInputFilePath).ToList();

            //find command strings and reformat. 
            IList<string> CommandStrings = this.CleanCommandStrings(idfListString);
            foreach (string commandstring in CommandStrings)
            {
                IDFCommand command = this.CreateCommandFromTextString(commandstring);

                this.IDFCommandList.Add(command);
            }
            if (ErrorList.Count() > 0)
            {
                StreamWriter readin_warnings = new StreamWriter(sIDFFileName + ".readwarnings");
                foreach (string error in this.ErrorList)
                {
                    readin_warnings.WriteLine(error);
                }
                readin_warnings.Close();
            }
        }
        /// <summary>
        /// This method calls the private method from the IDDDatabase class and creates, or overwrites that data in the singleton. 
        /// </summary>
        /// <param name="sIDDFilename"></param>
        public void LoadIDDFile(string sIDDFilename)
        {
            this.idd.LoadIDDFile(sIDDFilename);
        }
        /// <summary>
        /// Save IDF format file of building to the passed full path, and sets the current path variable "CurrentIDFFilePath"
        /// to the passed path. 
        /// </summary>
        /// <param name="sIDFFilename"> full path of idf file to be saved.</param>
        public override void SaveInputFile(string sIDFFilename)
        {
            TextWriter idffile = new StreamWriter(sIDFFilename);
            idffile.WriteLine(this.IDFTextBody);
            idffile.Close();
            this.CurrentInputFilePath = sIDFFilename;

            TextWriter originalfile = new StreamWriter(sIDFFilename+".original");
            foreach (string line in this.OrigIDFFileStringsNoComments)
            {
                originalfile.WriteLine(line);
            }
            originalfile.Close();
            this.CurrentInputFilePath = sIDFFilename;



        }
        public string IDFTextBody
        {
            get
            {
                String Body = "";
                foreach (IDFCommand command in this.IDFCommandList)
                {
                    Body += command.ToIDFString();
                }
                return Body;
            }
        }

        #endregion
        #region Methods to Delete /Create Commands.
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
        public IDFCommand CreateCommandFromTextString(string inputstring)
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


            if (limit < Convert.ToInt32(object_type.MinimumFields()) && object_type.NumberOfExtensibleFields == 0)
            {
                for (int emptyfieldcounter = limit; emptyfieldcounter < Convert.ToInt32(object_type.MinimumFields()); emptyfieldcounter++)
                {
                    if (object_type.RegularFields[emptyfieldcounter].Default() != null)
                    {
                        new_command.RegularArguments[emptyfieldcounter].Value = object_type.RegularFields[emptyfieldcounter].Default();
                    }
                    else
                    {
                        new_command.RegularArguments[emptyfieldcounter].Value = null;
                    }

                }
            }



            //Test that the if the required number of field is defined and that it is >= the number of fields present. Otherwise add error. 

            if (object_type.MinimumFields() != null && items.Count() < Convert.ToInt32(object_type.MinimumFields()))
            {

                string message = "Warning:In Command " + object_type.Name + " = " + new_command.GetName() + "\r\n";
                message += "\tCommand has " + items.Count() + " when IDD requires " + object_type.MinimumFields() + "\r\n";
                this.ErrorList.Add(message);


            }





            if (object_type.NumberOfExtensibleFields > 0)
            {
                //remove regular argument items from List. 
                items.RemoveRange(0, object_type.RegularFields.Count());

                if (items.Count % object_type.NumberOfExtensibleFields != 0 && object_type.IsUniqueObject() == false)
                {
                    string message = "Warning:In Command " + object_type.Name + " = " + new_command.GetName() + "\r\n";
                    message += "\tCommand has extensible items of" + items.Count() +
                               " when IDD requires that extensibles be multiples of " +
                               object_type.NumberOfExtensibleFields + "\r\n";
                    this.ErrorList.Add(message);

                }

                if ((items.Count % object_type.NumberOfExtensibleFields != 0) && (object_type.IsUniqueObject()))
                {
                    string message = "Warning:In Command " + object_type.Name + "\r\n";
                    message += "\tCommand has extensible items of" + items.Count() +
                               " when IDD requires that extensibles be multiples of " +
                               object_type.NumberOfExtensibleFields + "\r\n";
                    this.ErrorList.Add(message);

                }


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
                            if (object_type.ExtensibleFields[fieldcounter].Default() != null)
                            {
                                value = object_type.ExtensibleFields[fieldcounter].Default();
                            }
                            else
                            {
                                value = null;
                            }


                        }

                        ArgumentList.Add(new IDFArgument(this, object_type.ExtensibleFields[fieldcounter], value));
                    }



                    new_command.ExtensibleSetArguments.Add(ArgumentList);
                }
            }

            if (inputstring.ToUpper() + ";" != new_command.ToIDFStringTerse().ToUpper().Trim())
            {
                string error = ("Read In Mismatch\r\n" + "\t" + inputstring.ToUpper() + ";" + "\r\n" + "\t" + new_command.ToIDFStringTerse().ToUpper().Trim() + "\r\n");
                this.ErrorList.Add(error);
            }


            return new_command;
        }
        /// <summary>
        /// Deletes all commands that are of type passed in ObjectName string.
        /// </summary>
        /// <param name="sObjectName">Name of IDD Object.</param>
        public void DeleteCommands(string sObjectName)
        {
            foreach (IDFCommand cmd in this.FindCommandsFromObjectName(this.IDFCommandList,sObjectName))
            {
                this.IDFCommandList.Remove(cmd);
            }
            this.UpdateAllObjectLists();
        }

        /// <summary>
        /// Deletes IDFCommands from the IDFCommand Container.
        /// </summary>
        /// <param name="Commands">List of IDFCommands to be removed.</param>
        public void DeleteCommands(IList<IDFCommand> Commands)
        {
            foreach (IDFCommand cmd in Commands)
            {
                this.IDFCommandList.Remove(cmd);
            }
            this.UpdateAllObjectLists();
        }

        /// <summary>
        /// Deletes a single command from the IDFCommands container.
        /// </summary>
        /// <param name="command">Command object to be removed.</param>
        public void DeleteCommand(IDFCommand command)
        {
            if (this.IDFCommandList.Contains(command))
            {
                this.IDFCommandList.Remove(command);
            }
            ;
            this.UpdateAllObjectLists();
        }
        #endregion
        #region Methods to Change / Add Commands.
        /// <summary>
        /// Deletes all simulation periods and add a new simulation period command to the 
        /// IDFCommands.
        /// </summary>
        /// <param name="startmonth">Start Month of simulation.</param>
        /// <param name="startday">Start Day of simulation.</param>
        /// <param name="endmonth">End Month of simulation</param>
        /// <param name="endday">End Day of simulation.</param>
        public void ChangeSimulationPeriod(int startmonth, int startday, int endmonth, int endday)
        {
            //removes any exisiting entries of simulation control or Runperiod. 
            
            this.DeleteCommands("RunPeriod");
            
            this.IDFCommandList.Add(
                this.CreateCommandFromTextString(
                    String.Format(
                        "RunPeriod,FullYear,{0},{1},{2},{3},UseWeatherFile,No,No,No,No,Yes,1;",
                        startmonth, startday, endmonth, endday)
                    ));
        }

        /// <summary>
        /// Turns on all simulation control options. 
        /// </summary>
        public void ChangeSimulationControl()
        {
            this.DeleteCommands("SimulationControl");
            this.IDFCommandList.Add(this.CreateCommandFromTextString("SimulationControl,Yes,Yes,Yes,Yes,Yes;"));
        }

        /// <summary>
        /// Insert IDFCommand to enable SQLite output.
        /// </summary>
        public void AddSQLiteOutput()
        {
            this.DeleteCommands("Output:SQLite");
            this.IDFCommandList.Add(this.CreateCommandFromTextString("Output:SQLite, SimpleAndTabular;"));   

        }

        /// <summary>
        /// Adds command to change aspect ratio of building. This is the Geometry Transform Object. 
        /// </summary>
        /// <param name="X">X axis scale</param>
        /// <param name="Y">Y axis scale</param>
        public void ChangeAspectRatioXY(double X, double Y)
        {
            //Remove Geometry Transform if present. 
            this.DeleteCommands("GeometryTransform");
            this.IDFCommandList.Add(
                this.CreateCommandFromTextString(String.Format("GeometryTransform,XY,{0},{1}", X, Y))
                );
        }
        #endregion
        #region Methods to Search / Find Commands and Arguments.
        public IList<IDFCommand>    FindCommandsOfObjectType(IList<IDFCommand> Commands, IDDObject objecttype)
        {
            var objects = (
                                           from command in Commands
                                           where command.Object == objecttype
                                           select command).ToList();
            return objects;
        }
        /// <summary>
        /// Returns all Commands which match the IDDObject name type in the IDFCommands list. 
        /// </summary>
        /// <param name="ObjectName">The string iddobject name.</param>
        /// <returns>List of IDFCommands whos obmatching the Object name. </returns>
        public IList<IDFCommand>    FindCommandsFromObjectName(IList<IDFCommand> Commands, string ObjectName)
        {
            IDDObject iddobject = this.idd.GetObject(ObjectName);
            return FindCommandsOfObjectType(Commands, iddobject);
        }
        public IList<IDFArgument>   FindArgumentsOfFieldType(IList<IDFCommand> Commands, IDDField FieldType)
        {
            List<IDFArgument> args = (
                                         from command in Commands
                                         from argument in command.FlattenedArgumentList
                                         where argument.Field == FieldType
                                         select argument).Distinct().ToList();
            return args.ToList();
        }
        public IList<IDFCommand>    FindCommands(IList<IDFCommand> Commands, string ObjectName, string FieldName, string FieldValue)
        {
            List<IDFCommand> objects = (from surface in this.FindCommandsFromObjectName(Commands, ObjectName)
                                        where
                                            surface.DoesArgumentExist(FieldName) &&
                                            surface.GetArgument(FieldName).Value == FieldValue
                                        select surface).ToList();
            return objects;
        }
        public IList<IDFCommand>    FindCommandsWithFieldValuePair(IList<IDFCommand> Commands, string FieldName, string FieldValue)
        {
            List<IDFCommand> objects = (from command in Commands
                                        where
                                            command.DoesArgumentExist(FieldName) &&
                                            command.GetArgument(FieldName).Value == FieldValue
                                        select command).ToList();
            return objects;
        }
        public IList<IDFCommand>    FindCommandsWithRangeErrors(IList<IDFCommand> Commands)
        {
            this.UpdateAllObjectLists();
            IList<IDFCommand> CommandsWithErrors = new List<IDFCommand>();
            foreach (IDFCommand command in this.IDFCommandList)
            {
                if (command.CheckValues())
                {
                    CommandsWithErrors.Add(command);
                }
            }
            return CommandsWithErrors;
        }
        public IList<IDFCommand>    FindCommandsOfGroup(IList<IDFCommand> Commands, string groupin)
        {
            List<IDFCommand> commands = (from command in Commands
                                         where command.Object.Group == groupin
                                         select command).ToList<IDFCommand>();
            return commands;

        }
        public bool TestCommand(string testin, string expectedresult)
        {
            List<string> test = this.CleanCommandStrings(Regex.Split(testin, "\r\n")).ToList();
            IDFCommand command = this.CreateCommandFromTextString(test.FirstOrDefault());
            string test3 = command.ToIDFString().Trim();
            bool test4 = (expectedresult.Trim() == command.ToIDFString().Trim());
            return test4;
        }
        #endregion




        protected void LoadResults(string resultsFile)
        {
            //SQLite test
            SqliteDB db = new SqliteDB(resultsFile);
            db.LoadDataSet();
            DataTable List = db.ListDataTables();
            DataTable Errors = db.LoadDataTable("Errors");
        }
        #endregion

    }
}