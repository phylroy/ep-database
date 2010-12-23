# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_construction_surface")
require("doe_building_parameter")
module DOE2
  class DOEFloor < DOE2::DOEConstructionSurface
    attr_accessor :polygon
    # a string object which defines the type of roof (e.g. attic)
    attr_accessor :type
    # The absorptance of the exterior surface of the floor
    # (see rule #4.3.5.3.(6)
    attr_accessor :absorptance
    # thermal insulation of floors
    attr_accessor :thermal_insulation
    
    def initialize
      super()
    end
  
    #This method returns the floor area
    def get_area
    
      # get the keyword for the shape of the floor
      floor = get_keyword_value("SHAPE")
    
      # if the keyword value is "BOX", the width and depth values are defined
      if ( floor == "BOX" )
        width = get_keyword_value("WIDTH")
        depth = get_keyword_value("DEPTH")
      
        depth = depth.to_f
        width = width.to_f
    
        return depth * width
    
        # if the keyword value is "POLYGON", the get_area is defined as the area of the
        # given polygon
      elsif ( floor == "POLYGON")
        return @polygon.get_area         
      
        # if the keyword value of the floor is "No-SHAPE", the get_area is given as the
        # get_area keyword value
      elsif (floor == "NO-SHAPE")
        area = get_keyword_value("AREA")
        area = area.to_f
        return area
      
      else
        raise "Error: The area could not be evaluated. Please check inputs\n "
      end
   
    end
  
    # This method returns the volume of the floor space
    def get_volume
      height = get_height
      area = get_area
      height = height.to_f
      area = area.to_f
      return height * area    
    end
  
  
    #Gets area of all surfaces in the floor.
    def get_total_area
    
    end
  
    # gets the height of the floor
    def get_height
      height = get_keyword_value("FLOOR-HEIGHT")
      height = height.to_f
      return height
    end
  
    # gets the space height
    def get_space_height
      spaceheight = get_keyword_value("SPACE-HEIGHT")
      spaceheight = spaceheight.to_f
      return spaceheight
    end
  
    # This method returns the Absolute Azimuth of the floor as a FLOAT
    # It first finds the azimuth of the building by searching for the commands 
    # with "BUILD-PARAMETERS" as the command name. 
    #
    # It then finds the building azimuth by returning the keyword value 
    # assigned to "AZIMUTH" within "BUILD-PARAMETERS".
    #
    # Following this, it precedes to find the azimuth of the floor in relationship
    # to the building, by checking if the azimuth keyword exists within the "FLOOR"
    # command.
    #
    # OUTPUT: Azimuth from True North
    def get_absolute_azimuth
      parameters = @building.commands.find_all_commands("BUILD-PARAMETERS")
      parameters.each do |sort|
        @parameter = sort
      end
      if @parameter.check_keyword?("AZIMUTH")
       building_azimuth = @parameter.get_keyword_value("AZIMUTH").to_f
      else
        building_azimuth = 0
      end

      if check_keyword?("AZIMUTH")
        floor_azimuth = get_keyword_value("AZIMUTH")
        floor_azimuth = floor_azimuth.to_f
      else
        floor_azimuth = 0
      end
      return floor_azimuth + building_azimuth
    end
  
    # This method returns the azimuth as a FLOAT if it exists
    # It first checks if the azimuth keyword value is present within the floor
    # command itself. If it does not find this, then it returns an azimuth of 0
    # because the floor would thus be aligned with the azimuth of the building
    # itself
    # 
    # OUTPUT: Azimuth between floor and building
    def get_azimuth
      if check_keyword?("AZIMUTH")
        floor_azimuth = get_keyword_value("AZIMUTH")
        floor_azimuth = floor_azimuth.to_f
        return floor_azimuth
      else
        return 0
      end
    end
    
 #calculatest area with respect to ground
    def area_wrt_ground ()
      
    end
  end
end
