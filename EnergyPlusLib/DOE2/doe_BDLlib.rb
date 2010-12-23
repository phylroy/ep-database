# Stores and accesses the materials from the BDLLIB.dat file

require "doe_command"
require "singleton"
require 'rubygems'
require 'sequel'

# memory database
module DOE2

  class DOEBDLlib
    
    attr_accessor :db, :materials
    
    include Singleton

    
    
    
    # stores the name of the individual materials

    attr_accessor :commandList
    # stores the name of the individual layers

  
    def initialize
#      @commandList = Array.new()
#      @db = Sequel.sqlite
#      @db.create_table :materials do # Create a new table
#        primary_key :id, :integer, :auto_increment => true
#        column :command_name, :text
#        column :name, :text
#        column :type, :text
#        column :thickness, :float
#        column :conductivity, :float
#        column :resistance, :float
#        column :density, :float
#        column :spec_heat, :float
#      end
#      @materials = @db[:materials] # Create a dataset
#
#      @db.create_table :layers do # Create a new table
#        primary_key :id, :integer, :auto_increment => true
#        column :command_name, :text
#        column :name, :text
#        column :material, :text
#        column :inside_film_res, :float
#      end
#      @layers = @db[:layers] # Create a dataset

      
      store_material()
    end
  


    def find_material(utype)
      posts =  @materials.filter(:name => utype)
      record = posts.first()  
      #Create the new command object.
      command = DOE2::DOECommand.new()
      #Insert the collected information into the object.
      command.commandName = "MATERIAL"
      command.utype = record[:name]
      command.set_keyword_value("TYPE", record[:type])
      command.set_keyword_value("THICKNESS", record[:thickness])
      command.set_keyword_value("CONDUCTIVITY", record[:conductivity])
      command.set_keyword_value("DENSITY", record[:density])
      command.set_keyword_value("SPECIFIC HEAT", record[:spec_heat])

      return command
    end    
    
    
    def find_layer(utype)
      posts =  @layers.filter(:name => utype)
      record = posts.first()  
      #Create the new command object.
      command = DOE2::DOECommand.new()
      #Insert the collected information into the object.
      command.commandName = "LAYERS"
      command.utype = record[:name]
      command.set_keyword_value("MATERIAL", record[:material])
      command.set_keyword_value("THICKNESS", record[:thickness])
      command.set_keyword_value("CONDUCTIVITY", record[:conductivity])
      command.set_keyword_value("DENSITY", record[:density])
      command.set_keyword_value("SPECIFIC HEAT", record[:spec_heat])

      return command
    end  
    

    
    
    
    # stores the material information using keywordPairs into the command structure
    # accessed using the find_command method
    private
    def store_material
      
      begin
        f = File.open("../Resources/DOE2_2/bdllib.dat")
      rescue
        f = File.open("Resources/DOE2_2/bdllib.dat")   
      end

      lines = f.readlines
      # Iterating through the file.
      lines.each_index do |i|
        command_string = ""
        # If we find a material.
        if lines[i].match(/\$LIBRARY-ENTRY\s(.{32})MAT .*/)
          #Get the name strips the white space. 
          name = ("\""+$1.strip + "\"")
           
          #Is this the last line?
          command_string = get_data(command_string, i, lines)
          #Extract data for material type PROPERTIES.
          if (match = command_string.match(/^\s*TYPE\s*=\s*(\S*)\s*TH\s*=\s*(\S*)\s*COND\s*=\s*(\S*)\s*DENS\s*=\s*(\S*)\s*S-H\s*=\s*(\S*)\s*$/) )
            #Create the new command object.
            command = DOE2::DOECommand.new()
            #Insert the collected information into the object.
            command.commandName = "MATERIAL"
            command.utype = name
            command.set_keyword_value("TYPE", $1.strip)
            command.set_keyword_value("THICKNESS", $2.strip.to_f.to_s)
            command.set_keyword_value("CONDUCTIVITY", $3.strip.to_f.to_s)
            command.set_keyword_value("DENSITY", $4.strip.to_f.to_s)
            command.set_keyword_value("SPECIFIC HEAT", $5.strip.to_f.to_s)
            #Push the object into the array for storage.
            @commandList.push(command)
            @materials << {:name => name, 
              :command_name => 'MATERIAL',
              :type =>  $1.strip,
              :thickness =>  $2.strip.to_f.to_s,
              :conductivity =>  $3.strip.to_f.to_s,
              :density =>  $4.strip.to_f.to_s,
              :spec_heat =>  $5.strip.to_f.to_s}  
         
              
              
            #Extract data for material type RESISTANCE.
          elsif (match = command_string.match(/^\s*TYPE\s*=\s*(\S*)\s*RES\s*=\s*(\S*)\s*$/) )
            command = DOE2::DOECommand.new() 
            command.commandName = "MATERIAL"
            command.utype = name
            command.set_keyword_value("TYPE", $1.strip)
            command.set_keyword_value("RESISTANCE", $2.strip.to_f.to_s)
            #Push the object into the array for storage.
            @materials << {:name => name, 
              :command_name => 'MATERIAL',
              :type =>  $1.strip,
              :resistance =>  $2.strip.to_f.to_s}
 
            @commandList.push(command)            
          else
            raise("data not extracted")
          end
        end
        
        if lines[i].match(/\$LIBRARY-ENTRY\s(.{32})LA .*/)
          #Get the name
          name = ("\""+$1.strip + "\"")
          #Is this the last line?
          command_string = get_data(command_string, i, lines)
          #Extract data into the command.
          if (match = command_string.match(/^\s*MAT\s*=\s*(.*?)\s*I-F-R\s*=\s*(\S*)\s*$/) )
            command = DOE2::DOECommand.new()
            command.commandName = "LAYERS"
            command.utype = name
            command.set_keyword_value("MATERIAL",$1)
            #Push the object into the array for storage.
            @layers << {:name => name, 
              :command_name => 'LAYER',
              :material =>  $1.strip,
              :inside_film_res =>  $2.strip.to_f.to_s}
            @commandList.push(command)           
          else
            raise("data not extracted")
          end
        end 
      end
      #@materials.print
      #@layers.print
    end
    
    private
    # This method will get all the 
    def get_data(command_string, i, lines)
      #Do this while this is NOT the last line of data.
      while (! lines[i].match(/^(.*?)\.\.\s*(.{6})?\s*?(\d*)?/) )
        #Grab all the data in between.
        if ( lines[i].match(/^\$.*$/) )
        elsif ( myarray = lines[i].match(/^(.*?)\s*(.{6})?\s*?(\d*)?\s*$/) )
          command_string = command_string + $1.strip
        end
        #Increment counter.
        i = i + 1
      end
      #Get the last line
      lines[i].match(/^(.*?)\.\.\s*(.{6})?\s*?(\d*)?/)
      command_string = command_string + $1.strip  
      if command_string == ""
        raise("error")
      end
      i  = i + 1
      command_string
    end
  end
end




