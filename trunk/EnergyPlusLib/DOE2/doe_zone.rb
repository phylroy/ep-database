# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_command")
#require("doe_lighting")
module DOE2
 class DOE2::DOEZone < DOECommand
  attr_accessor :space
  # a vector of spaces used when the declaration of space is "combined"
  attr_accessor :space_uses
  # a lighting object which stores the lighting characteristics of each zone
  attr_accessor :lighting
  #defines the thermal mass characteristics of the zone. 
  #could be a string object or a user defined object
  attr_accessor :thermal_mass
  # stores a constant floating value of the amount of air leakage, 
  #accoriding to rule #4.3.5.9.
  attr_accessor :air_leakage
  # this will be a vector consisting of heat transfer objects, 
  # which contains a pointer to the adjacent thermal block and a pointer 
  # to the wall in between them
  attr_accessor :heat_transfers
  def initialize
    super()
  end
  
  def output
    
    temp_string = basic_output()
    if (@space == nil)
      temp_string = temp_string + "$ No space found to match zone!\n"
    else
      temp_string = temp_string + "$Space\n"
      temp_string = temp_string +  "$\t#{@space.utype} = #{@space.commandName}\n" 
    end
    return temp_string
  end
  
  # This method finds all the exterior surfaces, ie. Exterior Wall and Roof
  # Output => surfaces as an Array of commands
  def get_exterior_surfaces()
    surfaces = Array.new()
    @space.get_children().each do |child|

      if child.commandName == "EXTERIOR-WALL" || 
          child.commandName == "ROOF"
        surfaces.push(child)
      end
    end
    return surfaces   
  end
  
  # This method returns all the children of the space
  def get_children()
    return @space.get_children()
  end
  
  # This method returns "Electricity" as the default fuel source
  def get_heating_fuel_source()
  return "Electricity"
  end

  # This method returns "direct" as the default condition type
  def condition_type()
    return "direct"
    #return "indirect"
  end
  
  # This method returns the area of the space
  def get_area()
    @space.get_area()
  end
  
  # This method returns "office" as the default usage of the space 
  def get_space_use()
    return "Office"
  end
  
  def set_occupant_number(value)
    #according to rule 4.3.1.3.2 if the condition is "indirect 
    #then the number of occupants is set to zero
  end
  
  def set_recepticle_power( value)
    #according to rule 4.3.1.3.2 if the condition is "indirect 
    #then the receptical power is set to zero
  end
  
  def set_service_water_heating( value )
    #according to rule 4.3.1.3.2 if the condition is "indirect 
    #then the service water heating is set to zero
  end
  
  def set_min_outdoor_air( value )
    #according to rule 4.3.1.3.2 if the condition is "indirect 
    #then the minimum outdoor air is set to zero
  end
  
  #set the values for operating schedules according
  #to Table 4.3.2.B.
  def set_occupancy_schedule(value)
    
  end
  
  #this is according to rule 4.3.2.1.2, which requires that
    # the ompliance shell shall automatically set the default 
    # values according to Table 4.3.2.A. based on the building type
    # selected and the floor area. 
  def set_schedules_based_on_building_type( type )
    
    set_occupant_number(value)
    set_recepticle_power( value)
    set_service_water_heating( value )
    set_min_outdoor_air( value )
    set_occupancy_schedule(value)
  end
  
  #sets the schedules of space type buildings based on Table 4.3.2.B. 
  #according to rule 4.3.2.2.(2)
  def set_schedules_based_on_space_type (type)
    
  end
  
  #sets the schedule for combined spaces based on the weighted average of
  #the combined values and Table 4.3.2.B., according to rule 4.3.2.2.(4).(c)
  def set_schedules_combined(zone_arrays)
    
  end
  
  #calculates heat gain due to occupants according to rule #4.3.2.3.
  def heat_gain()
    
  end
  
  def get_exposed_floors
    #gets all the exposed floors of the zone
  end
  
  def get_doors
    #gets all the doors in the zone
  end
  
  #gets windows in the zone
  def get_windows
    
  end
end 

end