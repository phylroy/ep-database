# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require 'doe_zone'
require 'doe_system'
require 'doe_window'
require 'doe_door'
require 'doe_interior_wall'
require 'doe_exterior_wall'
require 'doe_underground_wall'
require 'doe_roof'
require 'doe_space'
require 'doe_floor'
require 'doe_polygon'
require 'doe_layer'
require 'doe_material'
require 'doe_construction'
require("doe_command")

module DOE2
  class DOECommandFactory
    def initialize
    
    end
  
    def DOECommandFactory.command_factory(command_string, building)
      command = ""
      command_name = ""
      if (command_string != "")
        #Get command and u-value
        if ( command_string.match(/(^\s*(\".*?\")\s*\=\s*(\S+)\s*)/) )
          command_name=$3.strip
        else
          # if no u-value, get just the command.
          command_string.match(/(^\s*(\S*)\s*)/ )
          @command_name=$2.strip
        end
      end
      case command_name  
      when  "SYSTEM" then 
        command = DOE2::DOESystem.new()
      when  "ZONE" then 
        command = DOE2::DOEZone.new()
      when  "FLOOR" then 
        command = DOE2::DOEFloor.new()
      when  "SPACE" then 
        command = DOE2::DOESpace.new()
      when  "EXTERIOR-WALL" then 
        command = DOE2::DOEExteriorWall.new()
      when  "INTERIOR-WALL" then 
        command = DOE2::DOEInteriorWall.new()
      when  "UNDERGROUND-WALL" then 
        command = DOE2::DOEUndergroundWall.new()
      when  "ROOF" then 
        command = DOE2::DOERoof.new()
      when "WINDOW" then 
        command = DOE2::DOEWindow.new()
      when "DOOR" then 
        command = DOE2::DOEDoor.new()
      when "POLYGON" then 
        command = DOE2::DOEPolygon.new()
      when "LAYER" then
        command = DOE2::DOELayer.new()
      when "MATERIAL" then
        command = DOE2::DOEMaterial.new()
      when "CONSTRUCTION" then
        command = DOE2::DOEConstruction.new()
      
      else      
        command = DOE2::DOECommand.new()
      end
      command.get_command_from_string(command_string)
      command.building = building    
      return command       
    end
  end
end
