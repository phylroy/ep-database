# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
 
require("doe_construction_surface")
require("doe_BDLlib")
module DOE2
  class DOEExteriorWall < DOE2::DOEConstructionSurface
    # contains a string object/float object that defines the thermal
    #insulation, according to rule #4.3.5.7.
    attr_accessor :thermal_insulation
    def initialize
      super()
      
    end
  
    # This method finds the area of the exterior wall
    def get_area()
      # Finds the floor and space parents and assigns them to @floor and @space 
      # variables to be used later
      begin
        floor = get_floor()
        space = get_space()
        # Get the keyword value for location
        location = get_keyword_value("LOCATION")
        # Get the keyword value for polygon
        polygon_id = get_keyword_value("POLYGON")
      rescue
      end
  
    
      # if the polygon_id keyword value was nil and the location value was nil, then 
      # the height and width are directly defined within the "exteriorWall" command
    
      if  ( location == "BOTTOM" || location == "TOP") && (space.get_shape != "BOX")
        return space.polygon.get_area  
         
      elsif ( location == nil  && polygon_id == nil )
        height = get_keyword_value("HEIGHT")
        width = get_keyword_value("WIDTH")
        #puts "Direct:" + height + " times " + width
        height = height.to_f
        width = width.to_f
        
        return height * width
      elsif ( location == nil && polygon_id != nil)
        return space.polygon.get_area
         
          
        # if the location was defined as "SPACE...", it is immediately followed by a
        # vertex, upon which lies the width of the exteriorwall
      elsif location.match(/SPACE.*/)
        location = location.sub( /^(.{6})/, "")
        width = space.polygon.get_length(location)
        if space.check_keyword?("HEIGHT")
          height = space.get_height
        else
          height = floor.get_space_height
        end
        #puts floor.utype
        #puts "Space:" + height.to_s + " times " + width.to_s
        return width * height
        # if the shape was a box, the width and height would be taken from the
        # "SPACE" object
      elsif ( space.get_shape == "BOX" ) 
        width = space.get_width
        height = space.get_height
        return width * height
     
       
      else
        raise "The area could not be evaluated"
      end
    end
    
    #This method finds the floor parent
    def get_floor()
      get_parent("FLOOR")
    end
    
    #This method finds the space parent
    def get_space()
      get_parent("SPACE")
    end
    
    #This method gets the construction
    def get_construction_name()
      get_keyword_value("CONSTRUCTION")
    end
    
    def set_construction_name()
      
    end
  
    #This method randomizes whether it is a solid masonry
    def is_solid_masonery?()

      # p "randomize is_solid_masonery?"
      srand();rand(2)
    end
  
    #This method randomizes whether it is an above-ground foundation wall
    def is_above_ground_foundation_wall?()
      #p "randomize is_above_ground_foundation_wall?"
      srand();rand(2)
    end
  
    #This method randomizes the "is existing" flag
    def is_existing?()
      #p "randomize is_existing flag"
      false
    end
  
    #This method returns the window area
    def get_window_area()
      get_children_area("WINDOW")
    end
    
    #This method returns the door area
    def get_door_area()
      get_children_area("DOOR")
    end
    
    # This method returns the difference between the wall area and the window
    # and door
    def get_opaque_area()
      get_area.to_f - get_window_area().to_f - get_door_area().to_f
    end
    
    # This method returns the fraction of the wall dominated by the window
    def get_fwr()
      get_window_area().to_f / get_area.to_f
    end
    
    # This method returns the area of the children classes based on the given
    # commandname.
    # Input => A command_name as a String
    # Output => Total area as a float
    def get_children_area(scommand_name)
      area = 0.0
      @children.each do |child|

        if child.commandName == scommand_name
          area = child.get_area() + area
        end
      end
      return area
    end
    
    # This method returns the Azimuth value as a FLOAT if it exists
    # It first checks if the azimuth keyword value is present within the exterior
    # wall command itself. If it does not find this, then it checks for the location
    # keyword and assigns the correct azimuth depending on the azimuth of the parent
    # space. However, if the shape of the parent space is defined as a polygon, then it
    # searches for the location of the wall and uses the polygon's get-azimuth for the vertex
    # to return the azimuth of the wall
   
    #NOTE: The FRONT is defined as 0, going clockwise, ie. RIGHT = 90 degrees
    
    #OUTPUT: Azimuth between the parent SPACE and the EXTERIOR WALL
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
    
    # This method returns the Absolute Azimuth value as a FLOAT if it exists
    # It first checks if the azimuth keyword value is present within the exterior
    # wall command itself. If it does not find this, then it checks for the location
    # keyword and assigns the correct azimuth depending on the azimuth of the parent
    # space. However, if the shape of the parent space is defined as a polygon, then it
    # searches for the location of the wall and uses the polygon's get-azimuth for the vertex
    # and adding it on to the overall azimuth to get the Absolute Azimuth from True North
   
    #NOTE: The FRONT is defined as 0, going clockwise, ie. RIGHT = 90 degrees
    #
    #OUTPUT: Azimuth between EXTERIOR WALL and TRUE NORTH
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

    # This method will return the tilt of the wall. Since DOE defines this in many 
    # places, some work is required looking at the "LOCATION" keyword. 
    def get_tilt()
      if check_keyword?("TILT") then return get_keyword_value("TILT").to_f
      else
        if check_keyword?("LOCATION")
          location = get_keyword_value("LOCATION")
          case location
          when "TOP" 
            return 0.0            
          when "BOTTOM"
            return 180.0
          when "LEFT", "RIGHT", "BACK", "FRONT" 
            return 90.0
          end
        end
        # If it is a polygon or not defined, set to DOE default = 90.0
        return 90.0
      end
    end  
    
    # This method checks if the construction only has a defined U-value
    def just_u_value?()
      @construction.check_keyword?("U-VALUE")
    end
    
    # This method checks for the Absorptance keyword value within the construction
    def check_absorptance?()
      
      @construction.check_keyword?("ABSORPTANCE") 
      
    end
    
    # This method gets the Absorptance if it is found
    def get_absorptance()
      begin
        if (not @construction.check_keyword?("ABSORPTANCE") )
          @construction.set_keyword_value("ABSORPTANCE", "0.2" )
        end
        return @construction.get_keyword_value("ABSORPTANCE")
      rescue
        raise("Could not get absortance, is this really an exterior wall?\n #{output}")  
      end
    end

    # This method sets the absorptance as the String inputed
    # Inputs => absorptance value as String
    def set_absorptance(string)

      @construction.set_keyword_value("ABSORPTANCE", string )

    end 
    
    # This method returns the u-type
    def name()
      utype
    end
    
    #indicates whether the wall is in contact with the ground
    def contact_with_ground?
      
    end
    
    #calculatest area with respect to ground
    def area_wrt_ground ()
      
    end


    def get_width()
      # Finds the floor and space parents and assigns them to @floor and @space
      # variables to be used later
      begin
        floor = get_floor()
        space = get_space()
        # Get the keyword value for location
        location = get_keyword_value("LOCATION")
        # Get the keyword value for polygon
        polygon_id = get_keyword_value("POLYGON")
      rescue
      end
      if ( location == nil  && polygon_id == nil )
        width = get_keyword_value("WIDTH")
        width = width.to_f
        return width
        # if the location was defined as "SPACE...", it is immediately followed by a
        # vertex, upon which lies the width of the exteriorwall
      elsif location.match(/SPACE.*/)
        location = location.sub( /^(.{6})/, "")
        width = space.polygon.get_length(location)

        return width
        # if the shape was a box, the width and height would be taken from the
        # "SPACE" object
      elsif ( space.get_shape == "BOX" )
        width = space.get_width
        return width
      else
        raise "The width could not be evaluated"
      end
    end

    def get_height()
      # Finds the floor and space parents and assigns them to @floor and @space
      # variables to be used later
      begin
        floor = get_floor()
        space = get_space()
        # Get the keyword value for location
        location = get_keyword_value("LOCATION")
        # Get the keyword value for polygon
        polygon_id = get_keyword_value("POLYGON")
      rescue
      end


      # if the polygon_id keyword value was nil and the location value was nil, then
      # the height and width are directly defined within the "exteriorWall" command

      if ( location == nil  && polygon_id == nil )
        height = get_keyword_value("HEIGHT")

        height = height.to_f

        return height

        # if the location was defined as "SPACE...", it is immediately followed by a
        # vertex, upon which lies the width of the exteriorwall
      elsif location.match(/SPACE.*/)
        location = location.sub( /^(.{6})/, "")
        height = floor.get_space_height
        return  height
        # if the shape was a box, the width and height would be taken from the
        # "SPACE" object
      elsif ( space.get_shape == "BOX" )
        height = space.get_height
        return height


      else
        raise "The height could not be evaluated"
      end
    end















    
  end

end
