using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EnergyPlusLib
{
    public class DOECommand
    {
        //Contains the user specified name
        public String utype;
        //Contains the u-value
        String uvalue;
        // Contains the DOE-2 command name.
        String commandName;
        // Contains the Keyword Pairs.
        Dictionary<string, string> keywordPairs;
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

        // Pointer to the building obj.
        //attr_accessor building;

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

        String determine_command_type(String type)
        {
            type = type.Trim();
            //Default to regular input format.
            string s_command_type = "regular_command";
            //Check for one-line commands.
            if (this.one_line_commands.Contains(type)) s_command_type = "oneline";
            if (this.non_utype_commands.Contains(type)) s_command_type = "no_u-type";
            return s_command_type;
        }

        String doe_scope()
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
        int depth()
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
       void get_command_from_string(string command_string)
        {

            //Split the command based on the equal '=' sign.
            string remove = "";
            string keyword = "";
            string value = "";

            var command_and_uvalue_regex = new Regex(@"(^\s*("".*?"")\s*\=\s*(\S+)\s*)");
            var command_only_regex = new Regex(@"(^\s*(\S*)\s*)");
            var parameter_type_command_regex = new Regex(@"(^\s*("".*?"")\s*(\=?)\s*(\S*)\s*)");
            var material_type_command_regex = new Regex(@"(^\s*(MATERIAL)\s*(\=?)\s*(.*)\s*)(THICKNESS.*|INSIDE-FILM-RES.*)");
            var material2_type_command_regex = new Regex(@"(^\s*(MATERIAL|DAY-SCHEDULES|WEEK-SCHEDULES)\s*(\=?)\s*(.*)\s*)/)");
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
                    command_string.Replace(matchgroup.Groups[1].ToString(), "");
                }
                else
                //if no u-value, get just the command.
                {
                    Match matchgroup = command_only_regex.Match(command_string);
                    remove = matchgroup.Groups[1].ToString().Trim();
                    this.commandName = matchgroup.Groups[2].ToString().Trim();
                    command_string.Replace(matchgroup.Groups[1].ToString(), "");
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
                    command_string.Replace(matchgroup.Groups[1].ToString(), "");
                }
            }
        }

       String output()
        {
            String temp_string = "";
            if (this.utype != "")
            {
                temp_string = temp_string + "#{utype} = ";
            }
            temp_string = temp_string + this.commandName + "\n";
            foreach (var pair in this.keywordPairs)
            {
                temp_string = temp_string + String.Format("  {0} {1}\n", pair.Key, pair.Value);
            }
            temp_string = temp_string + "..\n";
            return temp_string;
        }
       String shortenValue(string keyword, string value)
      {
      int limit = 80;
      string comma =", ";
      string tempstring = String.Format("{0}                 = {1} \n", keyword, value);
      string returnstring = "";
      if (tempstring.Length < limit)
      {
        returnstring = tempstring;
      
      }
      
      /*
      else
        tempstring =  String.Format("{0}                 = \n", keyword);
        if value.match(/^\((.*)\)$/)
          newstring = value.match(/\((.*)\)/)
          array = Array.new()
          array = GetCSVData(newstring[1])
          tempstring = tempstring + " ( "

          array.each_with_index do |substring, i|
            if substring != ","
              #substring = ", "
              if (i+1) == array.length
                comma = " )\n"
              else
                comma =", "
              end
              substring.strip!()
              if ( ( tempstring.length() + substring.length() + comma.length() ) >= limit )
                returnstring = returnstring + tempstring  +"\n"
                tempstring = "         "+substring+ comma
              else
                tempstring = tempstring + substring + comma
              end
              if (i+1) == array.length

                returnstring = returnstring + tempstring
              end
            end
          end
          returnstring = returnstring
        end

        if value.match(/\{(.*)\}/)
          newstring = value.match(/\{(.*)\}/)
          array = Array.new()
          array = newstring[1].split(" ")
          tempstring = tempstring + " { "

          array.each_with_index do |substring, i|
            if substring != ","
              #substring = ", "
              if (i+1) == array.length
                comma = " )\n"
              else
                comma ="  "
              end
              substring.strip!()
              if ( ( tempstring.length() + substring.length() + comma.length() ) >= limit )
                returnstring = returnstring + tempstring  +"\n"
                tempstring = "         "+substring+ comma
              else
                tempstring = tempstring + substring + comma
              end
              if (i+1) == array.length

                returnstring = returnstring + tempstring
              end
            end
          end
          returnstring = returnstring
        end
      end
       *  */
      return returnstring;
      
      }


    }
}
