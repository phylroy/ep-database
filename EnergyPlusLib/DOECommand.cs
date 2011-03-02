using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Data;


namespace EnergyPlusLib
{
    public class BuildingDatabase
    {
             /// <summary>
        /// Place to store error involving this command. 
        /// </summary>
        public List<string> ErrorList = new List<string>();

        /// <summary>
        ///  The current input file name that was last loaded from or saved to. 
        /// </summary>
        public string CurrentInputFilePath { get; set; }

        /// <summary>
        /// Path to the energy plus root folder. "c:\energyplus6.0\" for example. 
        /// </summary>
        public string SimulationEngineRootFolder;

        /// <summary>
        /// The full path of the weather file used for the simulation. 
        /// </summary>
        public string WeatherFilePath;

        protected string InputFileExtention;

        protected string ResultFileExtention; 

        protected string SimulationExecutable;

        public virtual void LoadResults(string File){}
        public virtual void LoadInputFile(string File){}
        public virtual void SaveInputFile(string File){}
        public bool RunSimulation()
        {
            bool DeleteFolder = false;

            //Get path of current idf file. 
            string ProjectFolderPath = Path.GetDirectoryName(this.CurrentInputFilePath);
            //Create new folder to run simulation in. 
            string folder_name = ProjectFolderPath + @"\simrun\";
            if (Directory.Exists(folder_name))
            {
                if (DeleteFolder == true)
                {
                    Directory.Delete(folder_name, true);
                }
            }

            Directory.CreateDirectory(folder_name);

            string filename_no_extention = Path.GetFileNameWithoutExtension(this.CurrentInputFilePath);
            //Save IDF file in memory to new folder. 
            string file_name = folder_name + filename_no_extention + this.InputFileExtention;
            this.SaveInputFile(file_name);

            //Set sql filename

            string sql_file_name = folder_name + filename_no_extention + this.ResultFileExtention;

            //Save location of current folder and change dir to new folder. 
            string startdirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(folder_name);

            //Create new process 
            var SimProcess = new Process();
            var SimStartInfo = new ProcessStartInfo();
            SimStartInfo.FileName = "CMD.exe ";
            SimStartInfo.RedirectStandardError = false;
            SimStartInfo.RedirectStandardInput = false;
            SimProcess.StartInfo.UseShellExecute = false;
            SimProcess.StartInfo.RedirectStandardOutput = true;
            SimStartInfo.UseShellExecute = false;
            //Show the command window
            SimStartInfo.CreateNoWindow = false;

            //Set up simulation arguments. 
            string filen = folder_name + Path.GetFileNameWithoutExtension(this.CurrentInputFilePath);
            string sWeatherfileNoExtention = Path.GetFileNameWithoutExtension(this.WeatherFilePath);
            SimStartInfo.Arguments = "/D /c " + this.SimulationEngineRootFolder + this.SimulationExecutable + filen + " " +
                                    sWeatherfileNoExtention;
            SimProcess.EnableRaisingEvents = true;
            SimProcess.StartInfo = SimStartInfo;

            //start cmd.exe & the EP process
            SimProcess.Start();

            //set the wait period for exiting the process
            SimProcess.WaitForExit(150000000); //Roughly 1.73 days. 

            int ExitCode = SimProcess.ExitCode;
            bool EPSuccessful = true;

            //Now we need to see if the process was successful
            if (ExitCode > 0 & !SimProcess.HasExited)
            {
                SimProcess.Kill();
                EPSuccessful = false;
            }

            //now clean up after ourselves
            SimProcess.Dispose();
            //EPProcess.StartInfo = null;

            this.LoadResults(sql_file_name);


            //Return to the start directory. 
            Directory.SetCurrentDirectory(startdirectory);
            return EPSuccessful;
        }

    }



    public class DOECommand
    {
        //Contains the user specified name
        public String utype;
        //Contains the u-value
        String uvalue;
        // Contains the DOE-2 command name.
        String commandName;
        // Contains the Keyword Pairs.
        Dictionary<string, string> keywordPairs = new Dictionary<string, string>();
        // Lists all ancestors in increasing order.
        List<DOECommand> parents;
        // An Array of all the children of this command.
        List<DOECommand> children;
        // The command type. 
        String commandType;
        // Flag to see if this component is exempt.
        bool exempt = false;
        // Comments. To be added to the command. 
        String comments;
        // A list of all the non_utype_commands.
        List<String> non_utype_commands = new List<string>()
        {
        "TITLE", 
        "SITE-PARAMETERS", 
        "BUILD-PARAMETER", 
        "LOADS_REPORT", 
        "SYSTEMS-REPORT", 
        "MASTERS-METERS", 
        "ECONOMICS-REPORT",
        "PLANT-REPORT", 
        "LOADS-REPORT",
        "COMPLIANCE",
        "PARAMETER"
        };
        // A list of all the one line commands (no keyword pairs)
        List<String> one_line_commands = new List<String>() { "INPUT", "RUN-PERIOD", "DIAGNOSTIC", "ABORT", "END", "COMPUTE", "STOP" };

        List<List<String>> envelope_level = new List<List<String>>()
        {
            new List<String>(){"FLOOR"},
            new List<String>(){"SPACE"},
            new List<String>(){"EXTERIOR-WALL", "INTERIOR-WALL","UNDERGROUND-WALL", "ROOF"},
            new List<String>(){"WINDOW", "DOOR"},
        };
        List<List<String>> hvacLevel = new List<List<String>>() { 
            new List<String>(){"SYSTEM"}, 
            new List<String>(){"ZONE"} 
        };


        #region Keyword Methods
        String GetValue(string Keyword)
        {
            string result = null;
            this.keywordPairs.TryGetValue(Keyword, out result);
            return result;
        }
        void SetValue(string Keyword, String Value)
        {
            this.keywordPairs[Keyword] = Value;
        }
        void RemoveKeyword(string Keyword)
        {
            this.keywordPairs.Remove(Keyword);
        }
        #endregion
        public String determine_command_type(String type)
        {
            type = type.Trim();
            //Default to regular input format.
            string s_command_type = "regular_command";
            //Check for one-line commands.
            if (this.one_line_commands.Contains(type)) s_command_type = "oneline";
            if (this.non_utype_commands.Contains(type)) s_command_type = "no_u-type";
            return s_command_type;
        }
        public String doe_scope()
        {
            string scope = null;
            foreach (List<string> list in this.envelope_level)
            {
                if (list.Contains(this.commandName)) scope = "envelope";
            }
            foreach (List<string> list in this.hvacLevel)
            {
                if (list.Contains(this.commandName)) scope = "hvac";
            }
            return scope;
        }

        //Determines the DOE scope depth (Window, Wall, Space Floor) or (System->Plant) Hierarchy)
        public int depth()
        {
            int level = 0;
            List<List<String>> scopelist;
            if (doe_scope() == "hvac")
                scopelist = this.hvacLevel;
            else
                scopelist = this.envelope_level;

            foreach (List<string> list in scopelist)
            {
                if (list.Contains(this.commandName)) return level;
                level++;
            }
            return level;
        }

        //Gets DOE2 command from string. 
        public void get_command_from_string(string command_string)
        {

            //Split the command based on the equal '=' sign.
            string remove = "";
            string keyword = "";
            string value = "";

            var command_and_uvalue_regex = new Regex(@"(^\s*("".*?"")\s*\=\s*(\S+)\s*)");
            var command_only_regex = new Regex(@"(^\s*(\S*)\s*)");
            var parameter_type_command_regex = new Regex(@"(^\s*("".*?"")\s*(\=?)\s*(\S*)\s*)");
            var material_type_command_regex = new Regex(@"(^\s*(MATERIAL)\s*(\=?)\s*(.*)\s*)(THICKNESS.*|INSIDE-FILM-RES.*)");
            var material2_type_command_regex = new Regex(@"(^\s*(MATERIAL|DAY-SCHEDULES|WEEK-SCHEDULES)\s*(\=?)\s*(.*)\s*)");
            var star_type_command_regex = new Regex(@"(^\s*(\S*)\s*(\=?)\s*(\*.*?\*)\s*)");
            var bracket_type_command_regex = new Regex(@"(^\s*(\S*)\s*(\=?)\s*(\(.*?\))\s*)");
            var curly_type_command_regex = new Regex(@"(^\s*(\S*)\s*(\=?)\s*(\{.*?\})\s*)");
            var quotes_type_command_regex = new Regex(@"(^\s*(\S*)\s*(\=?)\s*("".*?"")\s*)");
            var single_command_regex = new Regex(@"(^\s*(\S*)\s*(\=?)\s*(\S+)\s*)");

            command_and_uvalue_regex.IsMatch(command_string);
            //ensure that command string is not empty.
            if (command_string != "")
            {

                //Get command and u-value

                if (command_and_uvalue_regex.IsMatch(command_string))
                {
                    Match matchgroup = command_and_uvalue_regex.Match(command_string);
                    this.commandName = matchgroup.Groups[3].ToString().Trim();
                    this.utype = matchgroup.Groups[2].ToString().Trim();
                    command_string = command_string.Replace(matchgroup.Groups[1].ToString(), "");
                }
                else
                //if no u-value, get just the command.
                {
                    Match matchgroup = command_only_regex.Match(command_string);
                    remove = matchgroup.Groups[1].ToString().Trim();
                    this.commandName = matchgroup.Groups[2].ToString().Trim();
                    command_string = command_string.Replace(matchgroup.Groups[1].ToString(), "");
                }

                //Loop throught the keyword values. 
                while (command_string.Length > 0)
                {
                    Match matchgroup = null;
                    //Parameter type command.
                    if (parameter_type_command_regex.IsMatch(command_string) && this.commandName == "PARAMETER")
                    { matchgroup = command_only_regex.Match(command_string); }

                    else if (material_type_command_regex.IsMatch(command_string))
                    { matchgroup = material_type_command_regex.Match(command_string); }

                    else if (material2_type_command_regex.IsMatch(command_string))
                    { matchgroup = material2_type_command_regex.Match(command_string); }

                    else if (star_type_command_regex.IsMatch(command_string))
                    { matchgroup = star_type_command_regex.Match(command_string); }

                    else if (bracket_type_command_regex.IsMatch(command_string))
                    { matchgroup = bracket_type_command_regex.Match(command_string); }

                    else if (curly_type_command_regex.IsMatch(command_string))
                    { matchgroup = curly_type_command_regex.Match(command_string); }

                    else if (quotes_type_command_regex.IsMatch(command_string))
                    { matchgroup = quotes_type_command_regex.Match(command_string); }

                    else if (single_command_regex.IsMatch(command_string))
                    { matchgroup = single_command_regex.Match(command_string); }


                    keyword = matchgroup.Groups[2].ToString().Trim();
                    value = matchgroup.Groups[4].ToString().Trim();
                    this.SetValue(keyword, value);
                    command_string = command_string.Replace(matchgroup.Groups[1].ToString(), "");
                }
            }
        }

        //converts DOECommand data to INP command sting. 
        public String output()
        {
            String temp_string = "";
            if (this.utype != "" && this.utype != null)
            {
                temp_string += String.Format("{0} = ", this.utype);
            }
            temp_string += this.commandName + "\n";
            foreach (var pair in this.keywordPairs)
            {
                temp_string = temp_string + this.shortenValue(pair.Key, pair.Value);
            }
            temp_string = temp_string + "..\n\n";
            return temp_string;
        }

        private String shortenValue(string keyword, string value)
        {

            int limit = 80;

            string tempstring = String.Format("   {0,-16}= {1} \n", keyword, value);
            string returnstring = "";
            if (tempstring.Length < limit)
            {
                returnstring = tempstring;

            }
            else
            {
                //add start of command.
                tempstring = String.Format("   {0,-16}= ", keyword);
                List<string> values = null;
                Match MatchGroup = null;
                string startbracket = null;
                string endbracket = null;

                Regex ValueInBrackets = new Regex(@"^\((.*)\)$");
                if (ValueInBrackets.IsMatch(value))
                {
                    MatchGroup = ValueInBrackets.Match(value);
                    values = Regex.Split(MatchGroup.Groups[1].ToString().Trim(), @"(,|\r\n|\n|\r)(?=(?:[^""]*""[^""]*"")*(?![^""]*""))").ToList<string>();
                    startbracket = " ( ";
                    endbracket = " ) ";
                }
                Regex ValueInCurlyBrackets = new Regex(@"\{(.*)\}");
                if (ValueInCurlyBrackets.IsMatch(value))
                {
                    MatchGroup = ValueInCurlyBrackets.Match(value);
                    values = Regex.Split(MatchGroup.Groups[1].ToString().Trim(), @"(,|\r\n|\n|\r)(?=(?:[^""]*""[^""]*"")*(?![^""]*""))").ToList<string>();
                    startbracket = " { ";
                    endbracket = " } ";
                }

                if (ValueInCurlyBrackets.IsMatch(value) || ValueInBrackets.IsMatch(value))
                {
                    tempstring = tempstring + startbracket;
                    string comma = ", ";
                    int counter = 0;
                    foreach (string substring in values)
                    {

                        if (substring != ",")
                        {
                            if (values[counter] == values.Last())
                            {
                                comma = endbracket + "\n";
                            }
                            else
                            {
                                comma = ", ";
                            }
                            string substring1 = substring.Trim();
                            if ((tempstring.Length + substring1.Length + comma.Length) >= limit)
                            {
                                returnstring = returnstring + tempstring + "\n";
                                tempstring = "         " + substring + comma;
                            }
                            else
                            {
                                returnstring = returnstring + tempstring + "\n";
                                tempstring = String.Format("                     {0}", substring.Trim() + comma);
                            }
                            if (values[counter] == values.Last())
                            {
                                returnstring = returnstring + tempstring;
                            }



                        }
                        counter++;
                    }

                }
            }


            return returnstring;
        }
    }

    public class INPDatabase : BuildingDatabase
    {

        List<DOECommand> Commands, parents;
        private DOECommand last_command; 
        String engine;
        string weather_file;
        string CurrentINPFilePath;
        //DOESIM doe_sim; 

        public INPDatabase()
        {
            this.SimulationEngineRootFolder = @"C:\DOE22\";
            this.SimulationExecutable = @"DOE22.BAT exent ";
            this.Commands = new List<DOECommand>();
            this.parents = new List<DOECommand>();

        }
        public void LoadInputFile(string filename, string weather_file)
        {
            this.CurrentINPFilePath = filename;
            this.weather_file = weather_file;
            this.Commands.Clear();
            this.parents.Clear();
            string command_string = "";

            List<String> FileAsString = File.ReadAllLines(this.CurrentINPFilePath).ToList();
            foreach (string line in FileAsString)
            {
                Regex IsComment = new Regex(@"\$.*");
                if (!IsComment.IsMatch(line))
                {
                    //Check if this is the last line of the command. 
                    Regex IsLastLine = new Regex(@"(.*?)\.\.");
                    if (IsLastLine.IsMatch(line))
                    {
                        Match match = IsLastLine.Match(line);
                        command_string = command_string + match.Groups[1];
                        DOECommand command = new DOECommand();
                        command.get_command_from_string(command_string);
                        command_string = "";
                        Commands.Add(command);
                    }
                    else
                    {
                        command_string += line;
                    }

                }


            }


        }
        public void write_clean_output_file(string INPFilename)
        {

            Console.Write("Writing DOE INP file");
            TextWriter idffile = new StreamWriter(INPFilename);
            idffile.WriteLine(this.INPTextBody);
            idffile.Close();


        }
        public string INPTextBody
        {
            get
            {
                String Body = "";
                foreach (DOECommand command in this.Commands)
                {
                    Body += command.output();
                }
                return Body;
            }
        }

        public List<DOECommand> determine_current_parents(DOECommand new_command)
        {
            if (this.last_command == null)
            {
                this.last_command = new_command;
            }
            //Check to see if scope (HVAC versus Envelope) has changed or the parent depth is undefined "0"
            if (this.parents.Count != 0 && (new_command.doe_scope() != @parents.Last().doe_scope() || new_command.depth() == 0 ))
            {
                this.parents.Clear();
            }

            //no change in parent
            if ( (new_command.depth() == this.last_command.depth() ) )
            {
                last_command = new_command; 
            }
            //Parent Depth Added.
            if ( (new_command.depth() > this.last_command.depth() ) )
            {
                parents.Add(last_command);
                last_command = new_command; 
            }

            //Parent Depth Added.
            if ( (new_command.depth() < this.last_command.depth() ) )
            {
                parents.RemoveAt(parents.Count() -1 );
                last_command = new_command; 
            }
            return parents;

        }

        public void SaveInputFile(string sINPFilename)
        {
            TextWriter inpfile = new StreamWriter(sINPFilename);
            inpfile.WriteLine(this.INPTextBody);
            inpfile.Close();
            this.CurrentINPFilePath = sINPFilename;
        }






    }

    public class DOE22SimFile
    {
        //Members.
        //location of file path. 
        public string filepath;
        public DataTable SystemAnnualTable;
        public DataTable ZoneAnnualTable;

        //BEPS table and header list vars.
        public DataTable bepsTable;
        public List<string> bepsHeaderList;

        //ES-D table and header list vars.
        public DataTable esdTable;
        public List<string> esdHeaderList;

        //Zone table for Annual or static data. 
        //public DataTable ZoneAnnualTable;

        //SV-A Header lists. 
        public List<string> zoneSVAHeaderList;
        public List<string> systemSVAHeaderList1;
        public List<string> systemSVAHeaderList2;
        //SS-R Header lists. 
        public List<string> zoneSSRHeaderList;



        public void scanSimFile()
        {

            //clear all tables. 
            Initialize();
            string[] lineArray = File.ReadAllLines(filepath);
            for (int linecounter = 0; linecounter < lineArray.Count(); linecounter++)
            {

                getBEPS(lineArray, linecounter);
                getESD(lineArray, linecounter);
                getSSR(lineArray, linecounter);
            }

            //Because the SV-A report truncates to the 25 char, the zone names must be initilized before the SV-A report.
            for (int linecounter = 0; linecounter < lineArray.Count(); linecounter++)
            {
                string line = lineArray[linecounter];
                getSVA(lineArray, linecounter);
            }
        }

        private void scanSimFile(string filepath)
        {
            this.filepath = filepath;
            scanSimFile();
        }

        public DOE22SimFile()
        {
            Initialize();

        }

        public DOE22SimFile(string filepath)
        {
            Initialize();
            scanSimFile(filepath);

        }

        private void Initialize()
        {

            bepsTable = new DataTable("bepsTable");
            bepsTable.Columns.Add("METER", typeof(string));
            bepsTable.Columns["METER"].Unique = true;
            bepsTable.PrimaryKey = new DataColumn[] { bepsTable.Columns["METER"] };
            bepsHeaderList = ParserTools.addColumns(ref bepsTable, new string[]{
                "System.String","METER",
                "System.String","FUEL",
                "System.Double","LIGHTS",
                "System.Double","TASK-LIGHTS",
                "System.Double","MISC-EQUIP",
                "System.Double","SPACE-HEATING",
                "System.Double","SPACE-COOLING",
                "System.Double","HEAT-REJECTION",
                "System.Double","PUMPS-AUX",
                "System.Double","VENT-FANS",
                "System.Double","REFRIG-DISPLAY",
                "System.Double","HT-PUMP-SUPPLEM",
                "System.Double","DOMEST-HOT-WTR",
                "System.Double","EXT-USAGE",
                "System.Double","TOTAL"
            });

            esdTable = new DataTable("esdTable");
            esdTable.Columns.Add("UTILITY-RATE", typeof(string));
            esdTable.Columns["UTILITY-RATE"].Unique = true;
            esdTable.PrimaryKey = new DataColumn[] { esdTable.Columns["UTILITY-RATE"] };
            esdHeaderList = ParserTools.addColumns(ref esdTable, new string[]{
                "System.String","UTILITY-RATE",
                "System.String","RESOURCE",
                "System.String","METERS",
                "System.Double","METERED-ENERGY UNITS/YR",
                "System.String","UNIT",
                "System.Double","TOTAL CHARGE ($)",
                "System.Double","VIRTUAL RATE ($/UNIT)",
                "System.String","RATE USED ALL YEAR"
            });



            SystemAnnualTable = new DataTable("systemTable");
            SystemAnnualTable.Columns.Add("SYSTEM-NAME", typeof(string));
            SystemAnnualTable.Columns["SYSTEM-NAME"].Unique = true;
            SystemAnnualTable.PrimaryKey = new DataColumn[] { SystemAnnualTable.Columns["SYSTEM-NAME"] };
            //First line.
            systemSVAHeaderList1 = ParserTools.addColumns(ref SystemAnnualTable, new string[] {    
                "System.String", "SYSTEM-NAME",
                "System.String", "SYSTEM-TYPE",
                "System.Double", "ALTITUDE-FACTOR",
                "System.Double", "FLOOR-AREA",
                "System.Double", "MAX-PEOPLE",
                "System.Double", "OUTSIDE-AIR-RATIO",
                "System.Double", "COOLING-CAPACITY",
                "System.Double", "SENSIBLE",
                "System.Double", "HEATING-CAPACITY",
                "System.Double", "COOLING-EIR",
                "System.Double", "HEATING-EIR",
                "System.Double", "HEAT PUMP-SUPP-HEAT"
            });

            //Second line
            systemSVAHeaderList2 = ParserTools.addColumns(ref SystemAnnualTable, new string[]{
                "System.String", "FAN-TYPE",
                "System.Double", "FAN-CAPACITY",
                "System.Double", "DIVERSITY-FACTOR",
                "System.Double", "POWER-DEMAND",
                "System.Double", "FAN-DELTA-T",
                "System.Double", "STATIC-PRESSURE",
                "System.Double", "TOTAL-EFF",
                "System.Double", "MECH-EFF",
                "System.String", "FAN-PLACEMENT",
                "System.String", "FAN_CONTROL",
                "System.Double", "MAX-FAN-RATIO",
                "System.Double", "MIN-FAN-RATIO"
            });

            ZoneAnnualTable = new DataTable("zoneTable");
            ZoneAnnualTable.Columns.Add("ZONE-NAME", typeof(string));
            ZoneAnnualTable.Columns["ZONE-NAME"].Unique = true;
            ZoneAnnualTable.PrimaryKey = new DataColumn[] { ZoneAnnualTable.Columns["ZONE-NAME"] };
            zoneSVAHeaderList = ParserTools.addColumns(ref ZoneAnnualTable, new string[]{
                "System.String", "ZONE-NAME",
                "System.Double", "SUPPLY-FLOW",
                "System.Double", "EXHAUST-FLOW",
                "System.Double", "FAN",
                "System.Double", "MINIMUM-FLOW",
                "System.Double", "OUTSIDE-AIR-FLOW",
                "System.Double", "COOLING-CAPACITY",
                "System.Double", "SENSIBLE",
                "System.Double", "EXTRACTION-RATE",
                "System.Double", "HEATING-CAPACITY",
                "System.Double", "ADDITION-RATE",
                "System.Double", "ZONE-MULT",
                "System.Double", "BASEBOARD-HEATING-CAPACITY"
            });


            //SS-R report.


            //            VECC 2008 Proposed Model DDMode                                                  DOE-2.2-44e4   2/23/2010    16:29:18  BDL RUN  1

            //REPORT- SS-R Zone Performance Summary for   RTU-1 (PSZ)                                     WEATHER FILE- VANCOUVER TMY       
            //---------------------------------------------------------------------------------------------------------------------------------


            //                   ZONE OF  ZONE OF   ZONE     ZONE        --------  Number of hours within each PART LOAD range  --------- TOTAL
            //                   MAXIMUM  MAXIMUM   UNDER    UNDER         00    10    20    30    40    50    60    70    80    90   100   RUN
            //                  HTG DMND CLG DMND  HEATED   COOLED         10    20    30    40    50    60    70    80    90   100    +  HOURS
            // ZONE              (HOURS)  (HOURS)  (HOURS)  (HOURS)
            //----------------  -------- -------- -------- --------      ----  ----  ----  ----  ----  ----  ----  ----  ----  ----  ----  ----
            //EL4 South Perim Zn (G.S11)      
            //                         0        0        0        0         0     0     0  1226     0     0     0     0     0     0  3840  5066
            //EL1 South Perim Zn (G.S11)      
            //                         0        0        0      623         0     0     0  1152   374   436   425   639   353  1687     0  5066

            //                  -------- -------- -------- --------

            //           TOTAL         0        0        0      623

            zoneSSRHeaderList = ParserTools.addColumns(ref ZoneAnnualTable, new string[]{
                "System.String", "ZONE-NAME",
                "System.Double", "ZONE-OF-MAX-HTG-DMND (HOURS)",
                "System.Double", "ZONE-OF-MAX-CLG-DMND (HOURS)",
                "System.Double", "ZONE-UNDER-HEATED (HOURS)",
                "System.Double", "ZONE-UNDER-COOLED (HOURS)",
                "System.Double", "NUM-HOURS PLR 00-10",
                "System.Double", "NUM-HOURS PLR 10-20",
                "System.Double", "NUM-HOURS PLR 20-30",
                "System.Double", "NUM-HOURS PLR 30-40",
                "System.Double", "NUM-HOURS PLR 40-50",
                "System.Double", "NUM-HOURS PLR 50-60",
                "System.Double", "NUM-HOURS PLR 60-70",
                "System.Double", "NUM-HOURS PLR 70-80",
                "System.Double", "NUM-HOURS PLR 80-90",
                "System.Double", "NUM-HOURS PLR 90-100",
                "System.Double", "NUM-HOURS PLR 100-+",
                "System.Double", "NUM-HOURS PLR Total"
        });
        }

        //method stores the beps energy for the run. 
        private void getBEPS(string[] lineArray, int linecounter)
        {

            //Search for BEPS report.


            //not using regex here..it is too slow. 
            if (lineArray[linecounter].IndexOf("REPORT- BEPS Building Energy Performance") >= 0)
            {
                //find last line of BEPS Energy table. 
                int lastline = linecounter;
                while (lineArray[lastline] != "              =======  =======  =======  =======  =======  =======  =======  =======  =======  =======  =======  =======  ========")
                {
                    lastline += 1;
                }

                //Start reading in BEPS energy, eight lines from start of beps.
                int sublinecounter = linecounter + 7;
                while (sublinecounter < lastline)
                {

                    //get fuel meter name
                    string[] regexList = 
                        {  
                            "0",@"^(\w*)\s+(\S*)\s*$",
                            "1",@".{12}(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})(.{9})?(.*)?"
                        };
                    string linetest1 = lineArray[sublinecounter];
                    string linetest2 = lineArray[sublinecounter + 1];
                    string linetest3 = lineArray[sublinecounter + 2];
                    string linetest4 = lineArray[sublinecounter + 3];
                    List<string> values = ParserTools.GetRowsDataToList(lineArray, sublinecounter, regexList);
                    sublinecounter += 3;
                    DataRow row;
                    DataTable tabletest = bepsTable;
                    string valuetest = values[0];
                    if (tabletest.Rows.Find(values[0]) == null)
                    {
                        row = tabletest.NewRow();
                        row["METER"] = values[0];
                        values.RemoveAt(0);
                    }
                    else
                    {
                        row = tabletest.Rows.Find(values[0]);
                        values.RemoveAt(0);
                    }


                    for (int iCounter = 0; iCounter < values.Count(); iCounter++)
                    {
                        row[bepsHeaderList[iCounter]] = values[iCounter];
                    }
                    bepsTable.Rows.Add(row);
                }
                bepsTable.AcceptChanges();

            }
        }

        //method stores the esd energy for the run. 
        private void getESD(string[] lineArray, int linecounter)
        {
            string line = lineArray[linecounter];
            //Search for esd report.
            if (lineArray[linecounter].IndexOf("REPORT- ES-D Energy Cost Summary") >= 0)
            {
                //find last line of esd Energy table. 
                int lastline = linecounter;
                while (lineArray[lastline] != "                                                                                          ==========")
                {
                    lastline += 1;
                }
                //Start reading in esd energy, six lines from start of esd.
                int sublinecounter = linecounter + 8;
                while (sublinecounter < lastline)
                {
                    //get fuel meter name
                    string[] regexList = 
                        {  
                            "0",@"(.{35})(.{19})(.{11})(.{13})(.{12})(.{10})(.{13})(.{9})"
                        };
                    List<string> values = ParserTools.GetRowsDataToList(lineArray, sublinecounter, regexList);
                    sublinecounter += 2;
                    DataRow row;
                    DataTable tabletest = esdTable;
                    string valuetest = values[0];
                    //Check to see if a row does not exist..if not create a new row.
                    if (tabletest.Rows.Find(values[0]) == null)
                    {
                        row = tabletest.NewRow();
                        row["UTILITY-RATE"] = values[0];
                        values.RemoveAt(0);
                    }
                    else
                    {
                        //otherwise find the existing row and use it. 
                        row = tabletest.Rows.Find(values[0]);
                        values.RemoveAt(0);
                    }
                    for (int iCounter = 0; iCounter < values.Count(); iCounter++)
                    {
                        row[esdHeaderList[iCounter]] = values[iCounter];
                    }
                    esdTable.Rows.Add(row);
                }
                esdTable.AcceptChanges();
            }
        }

        //method stores the beps energy for the run. 
        private void getSVA(string[] lineArray, int linecounter)
        {


            //Search for SV-A
            if (lineArray[linecounter].IndexOf("REPORT- SV-A System Design Parameters for") >= 0)
            {

                Regex sva_flag = new Regex(@"^\s*REPORT- SV-A System Design Parameters for(.{51})WEATHER FILE.*$", RegexOptions.IgnoreCase);
                if (sva_flag.IsMatch(lineArray[linecounter]))
                {
                    //Zone 
                    DataRow row;
                    MatchCollection matches = sva_flag.Matches(lineArray[linecounter]);
                    string system_name = matches[0].Groups[1].Value.ToString().Trim();

                    //Start reading in System data, six lines from start of beps.
                    int sublinecounter = linecounter + 6;
                    string line1 = lineArray[sublinecounter];

                    List<string> values = ParserTools.getStringMatches(@"\s*(.{8})(.{11})(.{11})(.{11})(.{11})(.{11})(.{11})(.{11})(.{11})(.{11})(.{11})\s*$", lineArray[sublinecounter]);
                    values.Insert(0, system_name);
                    if (SystemAnnualTable.Rows.Find(values[0]) == null)
                    {
                        row = SystemAnnualTable.NewRow();
                        row["SYSTEM-NAME"] = values[0];
                        values.RemoveAt(0);
                        SystemAnnualTable.Rows.Add(row);
                    }
                    else
                    {
                        row = SystemAnnualTable.Rows.Find(values[0]);
                        values.RemoveAt(0);
                    }

                    int counter = 0;
                    foreach (string value in values)
                    {
                        row[systemSVAHeaderList1[counter]] = value;
                        counter += 1;
                    }
                    int iNotSumLine = 0;
                    if (values[0] != "SUM")
                    {

                        //Start reading in System data line 2.
                        sublinecounter = linecounter + 13;
                        line1 = lineArray[sublinecounter];
                        List<string> values2 = ParserTools.getStringMatches(@"\s*(.{8})(.{11})(.{11})(.{9})(.{10})(.{11})(.{8})(.{8})(.{12})(.{10})(.{10})(.{10})\s*$", lineArray[sublinecounter]);
                        int counter2 = 0;
                        foreach (string value in values2)
                        {
                            row[systemSVAHeaderList2[counter2]] = value;
                            counter2 += 1;
                        }
                        iNotSumLine = 4;
                    }

                    SystemAnnualTable.AcceptChanges();


                    //Start reading in Zone data.
                    sublinecounter = linecounter + 16 + iNotSumLine;

                    Regex end_flag = new Regex(@".*DOE-.*\d+/\d+/\d+.*BDL RUN.*$", RegexOptions.IgnoreCase);
                    Regex baseboard = new Regex(@"\s*(.*)\(BASEBOARDS\)$", RegexOptions.IgnoreCase);
                    while (!end_flag.IsMatch(lineArray[sublinecounter]))
                    {
                        int skip = 0;
                        if (lineArray[sublinecounter] != "")
                        {


                            line1 = lineArray[sublinecounter];
                            List<string> values3 = ParserTools.getStringMatches(@"(.{26})(.{10})(.{10})(.{10})(.{10})(.{10})(.{10})(.{10})(.{10})(.{10})(.{10})(.{5})$", lineArray[sublinecounter]);
                            if (baseboard.IsMatch(lineArray[sublinecounter + 1]))
                            {
                                List<string> baseboardVal = ParserTools.getStringMatches(@"\s*(.*)\(BASEBOARDS\)$", lineArray[sublinecounter + 1]);
                                values3.Add(baseboardVal[0]);
                                skip = 1;
                            }
                            else
                            {
                                values3.Add("0.0");
                                skip = 0;
                            }

                            row = null;
                            for (int i = 0; i < ZoneAnnualTable.Rows.Count; i++)
                            {
                                DataRow testrow = ZoneAnnualTable.Rows[i];
                                string Zonename = testrow["ZONE-NAME"].ToString();

                                if (Zonename == values3[0]
                                    || (Zonename.Length >= 25 && Zonename.Substring(0, 25) == values3[0]))
                                {
                                    row = testrow;
                                }
                            }

                            if (row == null)
                            {
                                row = ZoneAnnualTable.NewRow();
                                row["ZONE-NAME"] = values3[0];
                                ZoneAnnualTable.Rows.Add(row);
                            }
                            values3.RemoveAt(0);
                            if (values3.Count == 12)
                            {
                                int counter3 = 0;
                                foreach (string value in values3)
                                {
                                    row[zoneSVAHeaderList[counter3]] = value;
                                    counter3 += 1;
                                }
                                ZoneAnnualTable.AcceptChanges();
                            }
                        }
                        sublinecounter += (1 + skip);
                    }
                }
            }
        }
        //method stores the beps energy for the run. 
        private void getSSR(string[] lineArray, int linecounter)
        {

            //Search for SV-A
            if (lineArray[linecounter].IndexOf("REPORT- SS-R Zone Performance") >= 0)
            {

                string line = lineArray[linecounter];

                //Search for BESS-R report.
                string pattern = @"(\s*REPORT- SS-R Zone Performance.*$)";
                Regex ssr_flag = new Regex(pattern, RegexOptions.IgnoreCase);
                if (ssr_flag.IsMatch(line))
                {
                    //find last line of BEPS Energy table. 
                    int lastline = linecounter;
                    while (lineArray[lastline] != "                  -------- -------- -------- --------")
                    {
                        lastline += 1;
                    }

                    //Start reading in BEPS energy, nine lines from start of beps.
                    int sublinecounter = linecounter + 9;
                    while (sublinecounter + 2 < lastline)
                    {

                        //get fuel meter name
                        string[] regexList = 
                        {  
                            "0",@"^(.*)$",
                            "1",@"^(.{26})(.{9})(.{9})(.{9})(.{10})(.{6})(.{6})(.{6})(.{6})(.{6})(.{6})(.{6})(.{6})(.{6})(.{6})(.{6})$"
                        };

                        List<string> values = ParserTools.GetRowsDataToList(lineArray, sublinecounter, regexList);
                        sublinecounter += 2;
                        DataRow row;
                        DataRow foundrow = null;
                        DataTable tabletest = ZoneAnnualTable;
                        string valuetest = values[0];

                        for (int i = 0; i < ZoneAnnualTable.Rows.Count; i++)
                        {
                            DataRow testrow = ZoneAnnualTable.Rows[i];
                            string Zonename = testrow["ZONE-NAME"].ToString();

                            if (Zonename == values[0] || (Zonename.Length == 25 && Zonename.Substring(0, 25) == values[0]))
                            {
                                foundrow = testrow;
                            }
                        }
                        if (foundrow == null)
                        {
                            row = ZoneAnnualTable.NewRow();
                            row["ZONE-NAME"] = values[0];
                            values.RemoveAt(0);
                            ZoneAnnualTable.Rows.Add(row);

                        }
                        else
                        {
                            row = foundrow;
                            row = ZoneAnnualTable.Rows.Find(values[0]);
                            values.RemoveAt(0);
                        }


                        for (int iCounter = 0; iCounter < values.Count(); iCounter++)
                        {
                            row[zoneSSRHeaderList[iCounter]] = values[iCounter];
                        }
                        //zoneTable.Rows.Add(row);
                    }
                    ZoneAnnualTable.AcceptChanges();
                }
            }
        }
    }

    public class DOE22SimManager
    {
        public List<DOE22SimFile> DOESimFiles;

        //Excel Objects. 
        Excel.Application oXL;
        Excel.Workbook oWB;
        Excel.Worksheet oSheet;
        Excel.Sheets oXLSheets;
        Excel.Range oRange;


        //Flags to 
        bool bWriteESD;
        bool bWriteBEPS;
        bool bWriteZoneAnnualData;
        bool bWriteSystemAnnualData;
        string sFoldername;


        //This is the constructor for the Simulation manager. 
        public DOE22SimManager(string foldername, List<string> simFileList, bool writeBEPS, bool writeESD, bool writeZoneAnnualData, bool writeSystemAnnualData)
        {

            bWriteZoneAnnualData = writeZoneAnnualData;
            bWriteSystemAnnualData = writeSystemAnnualData;
            bWriteBEPS = writeBEPS;
            bWriteESD = writeESD;

            sFoldername = foldername;

            //Initialize the DOE22SIMfile. 
            DOESimFiles = new List<DOE22SimFile>();


            //Load all files from list. 
            LoadFilesFromList(simFileList);



            //Write to excel.
            WriteToExcel();



        }

        // This method loads all the simfile into the DOESimFiles Container. 
        public void LoadFilesFromFolder(string foldername)
        {

            // find all sim files in current folder. 
            DirectoryInfo di = new DirectoryInfo(foldername);
            FileInfo[] rgFiles = di.GetFiles("*.sim");
            foreach (FileInfo fi in rgFiles)
            {
                DOE22SimFile test = new DOE22SimFile(fi.FullName);
                DOESimFiles.Add(test);
                Console.Write(fi.FullName + "\n");
            }
        }

        public void LoadFilesFromList(List<string> simFileList)
        {
            foreach (string fi in simFileList)
            {
                DOE22SimFile test = new DOE22SimFile(fi);
                DOESimFiles.Add(test);
                Console.Write(fi + "\n");
            }


        }

        //This method wirtes the sim data to the excel file. 
        public void WriteToExcel()
        {
            int iSimRunNumber = 0;
            int linenumber = 0;

            // Start Excel and get Application object. 
            oXL = new Excel.Application();

            // Set some properties 
            oXL.Visible = false;
            oXL.DisplayAlerts = false;

            // Get a new workbook. 
            oWB = oXL.Workbooks.Add(Missing.Value);

            //Add a new sheets object.
            oXLSheets = oXL.Sheets as Excel.Sheets;


            foreach (DOE22SimFile simfile in DOESimFiles)
            {
                iSimRunNumber++;
                oSheet = (Excel.Worksheet)oXLSheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                oSheet.Name = "RUN-" + iSimRunNumber.ToString();

                //oSheet.Name = Path.GetFileName(simfile.filepath);
                linenumber = 0;
                // Output BEPS to excel Sheet.
                oSheet.Cells[linenumber = 1, 1] = Path.GetFileName(simfile.filepath);
                linenumber++;
                oSheet.Cells[linenumber, 1] = "BEPS";
                linenumber++;
                //print bpes report. 
                PrintTableToExcel(linenumber, simfile.bepsTable, oSheet);
                linenumber = linenumber + simfile.bepsTable.Rows.Count + 1;
                linenumber++;
                oSheet.Cells[linenumber, 1] = "ES-D";
                linenumber++;
                //Print es-d report. 
                PrintTableToExcel(linenumber, simfile.esdTable, oSheet);

                // Resize the columns 
                oRange = oSheet.get_Range(oSheet.Cells[1, 1],
                                          oSheet.Cells[simfile.bepsTable.Rows.Count,
                                          simfile.bepsTable.Columns.Count]);
                oRange.EntireColumn.AutoFit();
            }

            //reset linenumber for All sheet.
            linenumber = 0;
            oSheet = (Excel.Worksheet)oXLSheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            oSheet.Name = "ALL";

            foreach (DOE22SimFile simfile in DOESimFiles)
            {
                linenumber++;
                // Output Filename to excel Sheet.
                oSheet.Cells[linenumber, 1] = Path.GetFileName(simfile.filepath);
                linenumber++;

                if (bWriteBEPS == true)
                {
                    // Output Filename to excel Sheet.
                    oSheet.Cells[linenumber, 1] = "BEPS";
                    linenumber++;
                    //print beps report. 
                    PrintTableToExcel(linenumber, simfile.bepsTable, oSheet);
                    linenumber = linenumber + simfile.bepsTable.Rows.Count + 1;
                }

                //Print ES-D
                if (bWriteESD == true)
                {
                    linenumber++;
                    oSheet.Cells[linenumber, 1] = "ES-D";
                    linenumber++;
                    //Print es-d report. 
                    PrintTableToExcel(linenumber, simfile.esdTable, oSheet);
                    linenumber = linenumber + simfile.esdTable.Rows.Count + 1;
                }

                //Print Zone Annual Data
                if (bWriteZoneAnnualData == true)
                {
                    linenumber++;
                    oSheet.Cells[linenumber, 1] = "Zone Annual Data";
                    linenumber++;
                    //Print Zone Annual Data report. 
                    PrintTableToExcel(linenumber, simfile.ZoneAnnualTable, oSheet);
                    linenumber = linenumber + simfile.ZoneAnnualTable.Rows.Count + 1;
                }

                //Print System Annual Data
                if (bWriteSystemAnnualData == true)
                {
                    linenumber++;
                    oSheet.Cells[linenumber, 1] = "System Annual Data";
                    linenumber++;
                    //Print Zone Annual Data report. 
                    PrintTableToExcel(linenumber, simfile.SystemAnnualTable, oSheet);
                    linenumber = linenumber + simfile.SystemAnnualTable.Rows.Count + 1;
                }




                // Resize the columns 
                oRange = oSheet.get_Range(oSheet.Cells[1, 1],
                                          oSheet.Cells[simfile.bepsTable.Rows.Count,
                                          simfile.bepsTable.Columns.Count]);
                oRange.EntireColumn.AutoFit();
            }
            // Save the sheet and close 
            oSheet = null;
            oRange = null;
            oWB.SaveAs(sFoldername + @"\test.xls", Excel.XlFileFormat.xlWorkbookNormal,
                Missing.Value, Missing.Value, Missing.Value, Missing.Value,
                Excel.XlSaveAsAccessMode.xlExclusive,
                Missing.Value, Missing.Value, Missing.Value,
                Missing.Value, Missing.Value);
            oWB.Close(Missing.Value, Missing.Value, Missing.Value);
            oWB = null;

            // Clean up 
            // NOTE: When in release mode, this does the trick 
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        private static void PrintTableToExcel(int RowNumber, DataTable dt, Excel.Worksheet oSheet)
        {
            int rowCount = 0;
            foreach (DataRow dr in dt.Rows)
            {
                rowCount += 1;
                for (int i = 1; i < dt.Columns.Count + 1; i++)
                {
                    // Add the header the first time through 
                    if (rowCount == 2)
                    {
                        oSheet.Cells[RowNumber, i] = dt.Columns[i - 1].ColumnName;
                    }
                    oSheet.Cells[rowCount + RowNumber, i] = dr[i - 1].ToString();
                }
            }
        }

    }


 * */
}



