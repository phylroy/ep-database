
# Author::    Phylroy Lopez  (mailto:plopez@nrcan.gc.ca)
# Copyright:: Copyright (c) NRCan
# License::   GNU Public Licence

require("doe_commands")

=begin 
This class contains encapulates the generic interface for the DOE2.x command 
set. It store the u type, commands, and keyword pairs for each command. It also 
stores the parent and child command relationships w.r.t. the building envelope 
and the hvac systems. I have attempted to make the underlying storage of data 
private so, if required, we could move to a database solution in the future 
if required for web development..
=end

module DOE2
  class DOECommand
  
    # Contains the user specified name
    attr_accessor :utype
    #Contains the u-value
    attr_accessor :uvalue
    # Contains the DOE-2 command name.
    attr_accessor :commandName
    # Contains the Keyword Pairs.
    attr_accessor :keywordPairs
    # Lists all ancestors in increasing order.
    attr_accessor :parents
    # An Array of all the children of this command.
    attr_accessor :children
    # The command type. 
    attr_accessor :commandType
    # Flag to see if this component is exempt.
    attr_accessor :exempt
    # Comments. To be added to the command. 
    attr_accessor :comments
    # A list of all the non_utype_commands.
    attr_accessor :non_utype_commands
    # A list of all the one line commands (no keyword pairs)
    attr_accessor :one_line_commands    
    # Pointer to the building obj.
    attr_accessor :building
  
    #This method will return the value of the keyword pair if available. 
    #Example:
    #If you object has this data in it...
    #
    #"EL1 West Perim Spc (G.W4)" = SPACE           
    #SHAPE            = POLYGON
    #ZONE-TYPE        = CONDITIONED
    #PEOPLE-SCHEDULE  = "EL1 Bldg Occup Sch"
    #LIGHTING-SCHEDUL = ( "EL1 Bldg InsLt Sch" )
    #EQUIP-SCHEDULE   = ( "EL1 Bldg Misc Sch" )
    #
    #
    #then calling 
    #
    #get_keyword_value("ZONE-TYPE") 
    #
    #will return the string 
    #
    #"CONDITIONED".
    #
    #if the keyword does not exist, it will return a nil object.
  
    # Returns the value associated with the keyword.
    def get_keyword_value(string)
      return_string = String.new()
      found = false
      @keywordPairs.each do |pair|
        if pair[0] == string
          found = true
          return_string = pair[1]
        end
      end
      if found == false
        raise "Error: In the command #{@utype}:#{@command_name} Attempted to get a Keyword pair #{string} present in the command\n Is this keyword missing? \n#{output}"
      end
      return return_string
    end
    # Sets the keyword value.
    def set_keyword_value(keyword, value)
      found = false
      if (not @keywordPairs.empty? )
        @keywordPairs.each do |pair|
          if pair[0] == keyword
            pair[1] = value
            found = true
          end
        end
        if (found == false)
          @keywordPairs.push([keyword,value])
        end
      else
        #First in the array...
        add_keyword_pair(keyword,value)
      end
    end
  

  
    # Removes the keyword pair.
    def remove_keyword_pair(string)
      return_string = String.new()
      @keywordPairs.each do |pair|
        if pair[0] == string
          @keywordPairs.delete(pair)
        end
      end
      return return_string
    end
  
    def initialize
      @utype = String.new()
      @commandName= String.new()
      @keywordPairs=Array.new()
      @parents = DOECommands.new()
      @children = DOECommands.new()
      @commandType = String.new()
      @exempt = false
      #HVAC Hierarchry
      @comments =Array.new()
      @hvacLevel = Array.new()
      @hvacLevel[0] =["SYSTEM"]
      @hvacLevel[1] =["ZONE"]
      #Envelope Hierachy
      @envelopeLevel = Array.new()
      @envelopeLevel[0] = ["FLOOR"]
      @envelopeLevel[1] = ["SPACE"]
      @envelopeLevel[2] = ["EXTERIOR-WALL", "INTERIOR-WALL","UNDERGROUND-WALL", "ROOF"]
      @envelopeLevel[3] = ["WINDOW", "DOOR"]
     
      @non_utype_commands = Array.new()
      @non_utype_commands.push( "TITLE", 
        "SITE-PARAMETERS", 
        "BUILD-PARAMETER", 
        "LOADS_REPORT", 
        "SYSTEMS-REPORT", 
        "MASTERS-METERS", 
        "ECONOMICS-REPORT",
        "PLANT-REPORT", 
        "LOADS-REPORT",
        "COMPLIANCE",
        "PARAMETER")
      @one_line_commands = Array.new()
      @one_line_commands = ["INPUT","RUN-PERIOD","DIAGNOSTIC","ABORT", "END", "COMPUTE", "STOP"]
    
    end 
  
    def determine_command_type(string)
      #Default to regular input format.
      s_command_type = String.new("regular_command")
      #Check for one-line commands.
      @one_line_commands = Array.new()
      @one_line_commands = ["INPUT","RUN-PERIOD","DIAGNOSTIC","ABORT", "END", "COMPUTE", "STOP"]
      @one_line_commands.each do |type|
        match_string = /^\s*#{type}\s+.*$/ 
        if (string.match(match_string) )
          s_command_type = "oneline"
        end
      end 
      #Check for non u-type commands.
    
      @non_utype_commands.each do |type|
        match_string = /^\s*#{type}\s+.*$/ 
        if (string.match(match_string) )
          s_command_type = "no_u-type"
        end
      end
      return s_command_type 
    end  
  
  
    # Determines the DOE scope (Window, Wall, Space Floor) or (System->Plant) Hierarchy)
    def doe_scope
      scope = "none"
      @envelopeLevel.each_index do |index|
        @envelopeLevel[index].each do |name|
          if (@commandName == name )
            scope = "envelope"
          end
        end
      end
    
      @hvacLevel.each_index do |index|
        @hvacLevel[index].each do |name|
          if (@commandName == name )
            scope = "hvac"
          end
        end
      end
      return scope
    end  
    # Determines the DOE scope depth (Window, Wall, Space Floor) or (System->Plant) Hierarchy)
    def depth
      level = 0
      scopelist=[]
      if (doe_scope == "hvac")
        scopelist = @hvacLevel
      else
        scopelist = @envelopeLevel
      end
      scopelist.each_index do |index|
        scopelist[index].each do |name|
          if (@commandName == name )
            level = index
          end
        end
      end
      return level  
    end
  
    #Outputs the command in DOE 2.2 format.
    def output
      return basic_output()
    end
    #Outputs the command in DOE 2.2 format.
    def basic_output()
      temp_string = String.new()
    
      if (@utype != "")
        temp_string = temp_string + "#{@utype} = "
      end
      temp_string = temp_string + @commandName
      temp_string = temp_string + "\n"

      #sprintf("%17s %04x", 123, 123)
      #@keywordPairs.each {|array| temp_string = temp_string +  "\s\s\s#{array[0]} = #{shortenValue(array[1])}\n" }
      @keywordPairs.each {|array| temp_string = temp_string +  shortenValue( array[0],array[1])   }
      temp_string = temp_string + "..\n"
    
                  #temp_string = temp_string + "$Parents\n"
                  #@parents.each do |array|
                  #  temp_string = temp_string +  "$\t#{array.utype} = #{array.commandName}\n"
                  #end
                  #temp_string = temp_string + "..\n"
      
                  #temp_string = temp_string + "$Children\n"
                  #@children.each {|array| temp_string = temp_string +  "$\t#{array.utype} = #{array.commandName}\n" }
                  #temp_string = temp_string + "..\n"
                  return temp_string
    end


    def GetCSVData(csv_data)

      resultsArray = Array.new()
      csv_data.split(/(,|\r\n|\n|\r)(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))/m).each do |csv|
        #csv_data.split(/[,\n\r]+(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))/m).each do |csv|

        next if csv.empty?

        csv = csv.strip

        if csv =~ /\A(".*[^"]|[^"].*")\z/m then     # examples: csv => "ab\nc"def  or  abc"de\nf"
          puts
          puts "Error:"
          puts csv_data
          p csv
          puts csv[/\A./mu], csv[/.\z/mu]
          #puts csv[0..0], csv[-1..-1]
          puts
          next
        end

        #if csv =~ /\A".*"\z/m then csv.gsub!(/\A"(.*)"\z/m, '\1') end  # remove double-quotes at string beginning & end
        #if csv =~ /""/m then csv.gsub!(/""/m, '"') end                 # remove a double-quote from double double-quotes

        resultsArray.push(csv)

      end
      return resultsArray
    end

    def shortenValue(keyword, value)
      limit = 100
      if keyword == "MATERIAL"
        limit = 80
      else
        limit = 80
      end
      comma =", "
      tempstring = "   %-17s= %s\n" % [ keyword, value ]
      returnstring = ""
      if tempstring.length() < limit
        returnstring = tempstring
      else
        tempstring =  "   %-17s=" % [ keyword]
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
      return returnstring
    end


    # Creates the command informantion based on DOE 2.2 syntax.
    def get_command_from_string(command_string)
      #Split the command based on the equal '=' sign.
      remove = ""
      keyword=""
      value=""
    
      if (command_string != "")
        #Get command and u-value
        if ( command_string.match(/(^\s*(\".*?\")\s*\=\s*(\S+)\s*)/) )
          @commandName=$3.strip
          @utype = $2.strip
          remove = Regexp.escape($1)
        
        else
          # if no u-value, get just the command.
          command_string.match(/(^\s*(\S*)\s*)/ )
          remove = Regexp.escape($1)
          @commandName=$2.strip
        end
        #Remove command from string.
      
        command_string.sub!(/#{remove}/,"")
      
      
        #Loop throught the keyword values. 
        while ( command_string.length > 0 )

          #Parameter type command.
          if ( command_string.match(/(^\s*(".*?")\s*(\=?)\s*(\S*)\s*)/) and @commandName == "PARAMETER" )
            #puts "Quotes"
            keyword = $2
            value = $4.strip
            remove = Regexp.escape($1)
          

          #DOEMaterial, or SCHEDULES
          elsif ( command_string.match(/(^\s*(MATERIAL)\s*(\=?)\s*(.*)\s*)(THICKNESS.*|INSIDE-FILM-RES.*)/))
            #puts "Bracket"
            keyword = $2.strip
            value = $4.strip
            remove = Regexp.escape($1)

          elsif ( command_string.match(/(^\s*(MATERIAL|DAY-SCHEDULES|WEEK-SCHEDULES)\s*(\=?)\s*(.*)\s*)/))
            #puts "Bracket"
            keyword = $2.strip
            value = $4.strip
            remove = Regexp.escape($1)
            #Stars
          elsif ( command_string.match(/(^\s*(\S*)\s*(\=?)\s*(\*.*?\*)\s*)/))
            #puts "Bracket"
            keyword = $2.strip
            value = $4.strip
            remove = Regexp.escape($1)
          
            #Brackets
          elsif ( command_string.match(/(^\s*(\S*)\s*(\=?)\s*(\(.*?\))\s*)/))
            #puts "Bracket"
            keyword = $2.strip
            value = $4.strip
            remove = Regexp.escape($1)
            # Curly Brackets
          elsif ( command_string.match(/(^\s*(\S*)\s*(\=?)\s*(\{.*?\})\s*)/))
            #puts "Bracket"
            keyword = $2.strip
            value = $4.strip
            remove = Regexp.escape($1)

            #Quotes
          elsif ( command_string.match(/(^\s*(\S*)\s*(\=?)\s*(".*?")\s*)/) )
            #puts "Quotes"
            keyword = $2
            value = $4.strip
            remove = Regexp.escape($1)
            #single command
          elsif command_string.match(/(^\s*(\S*)\s*(\=?)\s*(\S+)\s*)/)
            #puts "Other"
            keyword = $2
            value = $4.strip
            remove = Regexp.escape($1)
          end
          #puts "DOE22::DOECommand: #{command_string}"
          #puts "K = #{keyword} V = #{value}\n"
          if (keyword != "")
            set_keyword_value(keyword,value)
          end
          command_string.sub!(/#{remove}/,"")
        end
        #puts "Keyword"
        #puts keywordPairs
      end  
    end
  
    #Returns an array of the commands parents. 
    def get_parents
      return @parents
    end

    #    def get_siblings
    #      puts "get sibs"
    #      puts output()
    #      siblings = DOE2::DOECommands.new()
    #      puts current_depth = depth()
    #      @children.shortprint()
    #      @parents[0].children.each do |child|
    #        if (child.depth() == current_depth )
    #          siblings.push(child)
    #        end
    #      end
    #      return siblings
    #    end
    
    #Returns an array of the commands children.
    def get_children
      return @children
    end
  
    
    #Kills all children. Infantcide.
    def kill_children
      #To do
    end
  
    #Creates an empty child.
    def create_child
      child_command = DOE2::DOECommand.new()
      child_command.parents.push(self)
      @parents.each { |parent| child_command.parents.push(@parents) }
      return child_command
    end
  
    # MNECB Interface.
    # Gets name. 
    def get_name()
      return @utype
    end
    
    # Check if keyword exists.
    def check_keyword?(keyword)
      @keywordPairs.each do |pair|
        if pair[0] == keyword
          return true
        end
      end
      return false
    end
    
    def get_parent(keyword)

      get_parents().each do |findcommand|
 
        if ( findcommand.commandName == keyword)
          return findcommand
        end
      end
      raise("#{keyword} parent not defined!")

    end
    
    def name()
      return utype
    end 
  
    private  
    def add_keyword_pair(keyword,pair)
      array = [keyword,pair]
      keywordPairs.push(array)
    
    end
    
    #indicates whether the wall is in contact with the ground
    def contact_with_ground?
      
    end
  
  
  end

end
