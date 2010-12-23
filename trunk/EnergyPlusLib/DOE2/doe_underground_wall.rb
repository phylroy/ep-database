# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_construction_surface")
module DOE2
  class DOEUndergroundWall < DOE2::DOEConstructionSurface
    def initialize
      super()
    end
  
    # Finds the area of the underground wall
  
    def get_area
    
      # Finds the floor and space parents and assigns them to @floor and @space 
      # variables to be used later
    
      parent = get_parents
      parent.each do |findcommand|
        if ( findcommand.commandName == "FLOOR" )
          @floor = findcommand
        end
        if ( findcommand.commandName == "SPACE")
          @space = findcommand
        end
      end
      
      # Get the keyword values for location and polygon
      begin
        location = get_keyword_value("LOCATION")
      rescue
      end
      # if the wall is in a space where the shape of the space is defined as
      # a "BOX"
      if ( @space.get_shape == "BOX" )
        height = @space.get_height
        width = @space.get_width
        return height * width
      
        # if the wall is in a space where the shape of the space is defined as
        # a "POLYGON"
      elsif ( @space.get_shape == "POLYGON")
    
        # if the location is thus defined as "TOP" or "BOTTOM", the space's polygon
        # is used to define the get_area
        if ( location == "TOP" || location == "BOTTOM" )
          return @space.polygon.get_area
        
          # if this is not the case, then it is defined by a coordinate vector, where
          # the get_area can be found by getting the polygon length for the vertex and the 
          # space height of the floor
        else
          location = location.sub( /^(.{6})/, "")
          width = @space.polygon.get_length(location)
          height = @floor.get_space_height
          return width * height
        end
        # if the shape of the space is found to be defined as "NO-SHAPE", the values
        # for width, height, and get_area could be defined directly in the interior-wall
        # class
      elsif ( @space.get_shape == "NO-SHAPE")
        begin
          width = get_keyword_value("WIDTH")
          height = get_keyword_value("HEIGHT")
        rescue
        end
      
        begin
          area = get_keyword_value("AREA")
        rescue
        end
        # if the width and height are not defined, then the get_area is directly entered
        # else, the get_area is entered and the width and height are not. If none of these
        # conditions hold, then an exception is raised for invalid inputs
   
        if ( width == nil && height == nil )
          if ( area == nil)
            raise "Error: The area could not be evaluated. Please check inputs\n "
          else
            return area.to_f
          end
        else
          width = width.to_f
          height = height.to_f
          return width * height
        end
      
      else
        raise "Error: The area could not be evaluated. Please check inputs"
      end
    
    end
  
    # This method returns the Azimuth value as a FLOAT if it exists
    # It first checks if the azimuth keyword value is present within the underground
    # wall command itself. If it does not find this, then it checks for the location
    # keyword and assigns the correct azimuth depending on the azimuth of the parent
    # space. However, if the shape of the parent space is defined as a polygon, then it
    # searches for the location of the wall and uses the polygon's get-azimuth for the vertex
    # to return the azimuth of the wall
    
    #NOTE: The FRONT is defined as 0, going clockwise, ie. RIGHT = 90 degrees
    
    #OUTPUT: Azimuth between UNDERGROUND WALL and parent SPACE
    def get_azimuth()
      space = get_parent("SPACE")
      if check_keyword?("AZIMUTH") then return get_keyword_value("AZIMUTH").to_f
      else
        if check_keyword?("LOCATION")
          location = get_keyword_value("LOCATION")
          case location
          when "TOP" 
            raise "Exception: Azimuth does not exist"
          when "BOTTOM"
            raise "Exception: Azimuth does not exist"
          when "FRONT" 
            return 0.0 + space.get_azimuth 
          when "RIGHT" 
            return 90.0 + space.get_azimuth 
          when "BACK" 
            return 180.0 + space.get_azimuth 
          when "LEFT" 
            return 270.0 + space.get_azimuth 
          end
        end
        if space.get_keyword_value("SHAPE") == "POLYGON"
          space_vertex = get_keyword_value("LOCATION")
          space_vertex.match(/SPACE-(.*)/)
          vertex = $1.strip
          return space.polygon.get_azimuth(vertex)
        end
      end
    
    end
    
    # This method returns the Azimuth value as a String if it exists
    # It first checks if the azimuth keyword value is present within the roof
    # command itself. If it does not find this, then it checks for the location
    # keyword and assigns the correct azimuth depending on the azimuth of the parent
    # space. However, if the shape of the parent space is defined as a polygon, then it
    # searches for the location of the roof and uses the polygon's get-azimuth for the vertex
    # and adding it on to the overall azimuth to get the Absolute Azimuth from True North
   
    #NOTE: The FRONT is defined as 0, going clockwise, ie. RIGHT = 90 degrees
    
    #OUTPUT: Azimuth between UNDERGROUND WALL and TRUE NORTH
    def get_absolute_azimuth
      space = get_parent("SPACE")
      if check_keyword?("AZIMUTH") 
        azimuth = get_keyword_value("AZIMUTH").to_f
        space_azimuth = space.get_absolute_azimuth
        return azimuth + space_azimuth
      else
        if check_keyword?("LOCATION")
          location = get_keyword_value("LOCATION")
          case location
          when "TOP" 
            raise "Exception: Azimuth does not exist"
          when "BOTTOM"
            raise "Exception: Azimuth does not exist"
          when "FRONT" 
            return 0.0 + space.get_absolute_azimuth 
          when "RIGHT" 
            return 90.0 + space.get_absolute_azimuth 
          when "BACK" 
            return 180.0 + space.get_absolute_azimuth 
          when "LEFT" 
            return 270.0 + space.get_absolute_azimuth  
          end
          
        end
         if space.get_keyword_value("SHAPE") == "POLYGON"
          space_vertex = get_keyword_value("LOCATION")
          space_vertex.match(/SPACE-(.*)/)
          vertex = $1.strip
          return space.polygon.get_azimuth(vertex) + space.get_absolute_azimuth
        end
       
      end
    end

  
  
  end
end