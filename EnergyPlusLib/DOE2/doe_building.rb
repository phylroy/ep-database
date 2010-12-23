# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_command_factory")
require("doe_commands")
require("building")
require("doe_sim")
module DOE2
  class DOEBuilding < Building
  
    #An array to contain all the DOE
    attr_accessor  :commands

    #An array to contain the current parent when reading in the input files.
    attr_accessor  :parents

    #DOE 2.2 Engine path
    attr_accessor :engine

    #Weather File.
    attr_accessor :weather_file

    #Input File.
    attr_accessor :doe_inp_file

    #doe_sim results
    attr_accessor :doe_sim

  
    # This method makes a deep copy of the building object.
    def deep_clone  
      Marshal::load(Marshal.dump(self))  
    end    
  
    # The Constructor.
    def initialize()
      super()
      @engine = "C:\\DOE22\\DOE22.BAT exent "
      @commands= DOE2::DOECommands.new()
      @parents= DOE2::DOECommands.new()
      @commandList = Array.new()
      @doe_sim = DOE22::DOESim.new()
    end
  

    # Find the surface get_area of a wall, roof, space or zone.. 
    # Example if the command is a zone, roof or surface it will return the get_area. 
    # If it is anything else, it will throw an exception. 
    def find_area_of(command)
      #only if needed...About 1 days work for window, wall and space/zone.
    end 
  
    # Will read an input file into memory and store all the commands into the 
    # @commands array.
    def read_input_file(filename,weather_file)
      @doe_inp_file = filename
      @weather_file = weather_file
      #Open the file.
      @commands.clear
      @parents.clear
      f = File.open(@doe_inp_file, "r")
      #Read the file into an array, line by line.
      lines = f.readlines
      #Set up the temp string.
      command_string =""
      #iterate through the file.
      parents = Array.new()
      children = Array.new()
      lines.each do|line|
        #puts line
        #Ignore comments (To do!...strip from file as well as in-line comments.
        if (!line.match(/\$.*/) )
          #Is this the last line?
          if (myarray = line.match(/(.*?)\.\./) )
            #Add the last part of the command to the newline...may be blank.
            command_string = command_string + myarray[1]
            #Determine correct command class to create, then populates it.

            command = DOE2::DOECommandFactory.command_factory(command_string, self)

            #Sets parents of command.
          
            parents = determine_current_parents(command)
            if (!parents.empty?)
              #puts "parent assigned"
              command.parents = parents
            end
            #inserts current command into the parent's children.
            if (!command.parents.empty?)
              command.parents.last.children.push(command)
              if command.commandName == "WINDOW"

              end
            end

            @commands.push(command)
            #            add_command_to_db(command)
            command_string = ""
          else
            myarray = line.match(/(.*)/)
            command_string = command_string + myarray[1]
          end
        end
      end
      organize_data()
      #@commandsdb.print
      #@keywordsdb.to_csv(true)
    end
  
    # This will right a clean output file, meaning no comments. Good for doing 
    # diffs
    def write_clean_output_file(string)
      puts "Writing" + string
      puts Dir.pwd()
      array = @commands
      # Second search the results to find the ZONEs with "SIZING-OPTION = ADJUST-LOADS"
      #array = building.find_keyword_value(array, "SIZING-OPTION", "ADJUST-LOADS")
      w = File.open(string, 'w')
      array.each { |command| w.print command.output }
      w.close
    end

    #Helper method to print banner comments in eQuest style. 
    def header(string)
      outstring ="$ ---------------------------------------------------------\n$              #{string}\n$ ---------------------------------------------------------\n"
      return outstring
    end 
  
    #Helper method to print big banner comments in eQuest style.  
    def big_header(string)
      outstring = "$ *********************************************************\n$ **                                                     **\n$ **            #{string}             \n$ **                                                     **\n$ *********************************************************\n"
      return outstring
    end
 
    # This method determines the current parents of the current command. ONLY TO 
    # BE USED BY READINPUTFILE method! 
    def determine_current_parents(new_command)
      if (@last_command == nil)
        @last_command = new_command
      end
      #Check to see if scope (HVAC versus Envelope) has changed or the parent depth is undefined "0"
      if (!@parents.empty? and (new_command.doe_scope != @parents.last.doe_scope or new_command.depth == 0 ))
        @parents.clear
        #puts "Change of scope or no parent"
        #@last_command = new_command
        #return 
      end
      #no change in parent.
      if ( (new_command.depth  == @last_command.depth)) 
        #no change
        @last_command = new_command
        #puts "#{new_command.commandName}"
      end
      #Parent depth added
      if ( new_command.depth  > @last_command.depth)
        @parents.push(@last_command)
        #puts "Added parent#{@last_command.commandName}"
        @last_command = new_command
      end
      #parent depth removed.
      if ( new_command.depth  < @last_command.depth) 
        parent = @parents.pop
        #puts "Removed parent #{parent}"
        @last_command = new_command
      end
      array = DOE2::DOECommands.new()
      @parents.reverse_each {|parent| array.push(parent) }
      return array
    end   


    #This routine organizes the hierarchy of the space <-> zones and the polygon 
    # associations that are not formally identified by the sequential relationship
    # like the floor, walls, windows. It would seem that zones and spaces are 1 to 
    # one relationships.  So each zone will have a reference to its space and vice versa. 
    # If there is a polygon command in the space or floor definition, a reference to the 
    # polygon class will be set. 
    def organize_data()
    
      # Associating the polygons with the FLoor and spaces. 
      polygons =  @commands.find_all_commands("POLYGON")
      spaces = @commands.find_all_commands("SPACE")
      floors = @commands.find_all_commands("FLOOR")
      zones = @commands.find_all_commands("ZONE")
      ext_walls = @commands.find_all_commands("EXTERIOR-WALL")
      roof = @commands.find_all_commands("ROOF")
      door = @commands.find_all_commands("DOOR")
      int_walls = @commands.find_all_commands("INTERIOR-WALL")
      underground_walls = @commands.find_all_commands("UNDERGROUND-WALL")
      underground_floors = @commands.find_all_commands("UNDERGROUND-FLOOR")
      constructions =@commands.find_all_commands("CONSTRUCTION")


      surface_lists = [ ext_walls, roof, door, int_walls, underground_walls, underground_floors]
      surface_lists.each do |surfaces|  
        #Find Polygons associated with  floor and and reference to floor.
        surfaces.each do |surface|  
          constructions.each do |construction| 
        
            if ( construction.utype == surface.get_keyword_value("CONSTRUCTION") )
              surface.construction = construction
            end
          end
        end
      end
    
    
    
      #Find Polygons associated with  floor and and reference to floor.
      floors.each do |floor|  
        polygons.each do |polygon| 
          if ( polygon.utype == floor.get_keyword_value("POLYGON") )
            floor.polygon = polygon
          end
        end
      end

      #Find Polygons for space and add reference to the space.
      spaces.each do |space|  

        polygons.each do |polygon|
          #make sure we don't find a polygon for NO-shape spaces.
          if ( space.get_keyword_value("SHAPE") != "NO-SHAPE")
            if ( polygon.utype ==    space.get_keyword_value("POLYGON") )
              space.polygon = polygon
            end
          end
        end
      end

    
      #    Find spaces that belong to the zone. 
      zones.each do |zone|
        spaces.each do |space| 
          if ( space.utype ==  zone.get_keyword_value("SPACE") )
            space.zone = zone
            zone.space = space
          end
        end
      end
    end
  
    #MNECB Commands Using MNECB terminology.  
  
    def get_all_thermal_blocks()
      zones = @commands.find_all_commands("ZONE")
    end
    
    def run_simulation(simulation_name)
      #Set folder names and file names.
      sim_name = File.basename(simulation_name, ".inp")
      dir_name = File.dirname(simulation_name)
      simfile = dir_name+ "\\" + sim_name + ".sim"

      #Delete old file if any.
      if File.exist?(simfile)
        File.delete(simfile)
      end
      if File.exist?(simulation_name)
        File.delete(simulation_name)
      end
     puts "Writing DOE input file."
      write_clean_output_file(simulation_name)

      puts command =  @engine + dir_name+ "\\" + sim_name + @weather_file + ">" + sim_name + ".output"
      puts "Running Simulation - " + sim_name
      system(command)
      #Read sim file result in.
      @doe_sim.read_sim_file(simfile)
      puts "Simulation Completed."
    end

    def run_rotated_simulation(file, weather_file)
      puts ""
      puts ""
      puts "Run Rotated Simulation (A90.1 2004)"
      read_input_file(file, weather_file)
      build_parm = @commands.find_all_commands("BUILD-PARAMETERS")
      cost = 0.0
      [0,90,180,270].each do |rotation|
        azimuth = 0.0
        if build_parm[0].check_keyword?("AZIMUTH") == true
          azimuth = build_parm[0].get_keyword_value("AZIMUTH").to_f
        end
        value = azimuth + rotation
        if value > 360 
          value = value - 360
        end
        build_parm[0].set_keyword_value("AZIMUTH", value.to_f)
        puts "runnning " + rotation.to_s
        run_simulation(rotation.to_s + ".inp")
        puts cost += @doe_sim.regulated_energy_cost()
      end
      puts "Average Energy Cost for 4 rotated runs = " + (cost / 4).to_s
      return cost / 4
    end

    def get_verticle_wall_area
      ext_walls = @commands.find_all_commands("EXTERIOR-WALL")
      ext_wall_area = 0.0
      ext_walls.each do |wall|
        #puts wall.utype
        if wall.get_tilt > 60.0
          #puts area = wall.get_area()
          ext_wall_area = ext_wall_area + wall.get_area.to_f
        end
      end
      puts "External Vertical Wall Area is " + ext_wall_area.to_s
      return ext_wall_area
    end

    def get_verticle_window_area
      ext_wins = @commands.find_all_commands("WINDOW")
      ext_wins_area = 0.0
      ext_wins.each do |win|
        #print win.get_keyword_value("HEIGHT") + "," + win.get_keyword_value("WIDTH") +"\n"
        #puts wall.utype
        #if win.get_tilt > 60.0
        #puts area = wall.get_area()
        ext_wins_area = ext_wins_area + win.get_area.to_f
        #end
      end
      puts "External Vertical Window Area is " + ext_wins_area.to_s
      return ext_wins_area
    end
    def get_glazing_to_wall_ratio()
      return get_verticle_window_area() / get_verticle_wall_area()
    end


    #Change fenestration % from excel file example.
    def changeFWRExcel(excelfile, doeinpfile , outputname)
      #Dumps Excel file into an array of array.
      rows = ExcelToArray(excelfile)
      #Create Building Object.
      building = DOE2::DOEBuilding.new()
      #Read Building File into mememory, and make sure it runs.
      building.read_input_file(doeinpfile, " ctmy\\VANCCTMY ")
      building.get_verticle_wall_area()
      #Find all windows.
      windows =  building.commands.find_all_commands("WINDOW")
      #Set Current Window to nothing.
      current_win = ""
      #Loop each row.
      rows.each do |row|
        #Set Found flag to flase.
        bfoundflag = false
        #Check the second column in row (The percentage) and make sure it is greater than 0.1. This pervents setting a Zero Window.
        if row[1].to_f > 0.01
          #Loop through each window.
          windows.each do |window|
            #Check to see if the window name matches the name in the excel file.
            if  (window.utype == "\"" + row[0].to_s + "\"")
              #Set the window to wall ratio to the value in the excel file.
              window.set_fenestration_to_wall_ratio(row[1].to_f)
              # Let user knwo you found this window.
              #Set the flag to true so it stops looking for this file.
              bfoundflag = true
            end
          end
          #Check to see if window was found.
          if bfoundflag == false
            #Tell user you didn't find the window.
            puts "did not find " + current_win
          end
        end
      end
      #Run the simulation to make sure it works.
      building.run_simulation(outputname)
      #Returnt the building object.
      return building
    end

  end
end


