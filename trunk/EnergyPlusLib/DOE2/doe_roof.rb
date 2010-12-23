# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
 require("doe_command")
module DOE2
class DOERoof < DOE2::DOECommand
  # area of the roof (opaque surface minus the area of any skylights)
  attr_accessor :gross_area
  # absorptance of the exterior surface of the roof as mentioned in 
  # rule #4.3.5.2.(6)
  attr_accessor :ext_absorptance
  # type of roof
  attr_accessor :type
  #the amount of heat transfered to the air between the roof
  # (as mentioned in rule #4.3.5.2.(7)
  attr_accessor :return_air_plenum
  
  def initialize
        super()
  end
  
  # This method finds the area of the roof
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
      
    # Get the keyword value for location
    begin
    location = get_keyword_value("LOCATION")
    rescue
    end
    
    # Get the keyword value for polygon
    begin
    polygon_id = get_keyword_value("POLYGON")
    rescue
    end
    
    # if the polygon_id keyword value was nil and the location value was nil, then 
    # the height and width are directly defined within the "roof" command
    
        
       if  ( location == "BOTTOM" || location == "TOP") && (@space.get_shape != "BOX")
         return @space.polygon.get_area  
         
       elsif ( location == nil  && polygon_id == nil )
          height = get_keyword_value("HEIGHT")
          width = get_keyword_value("WIDTH")
          height = height.to_f
          width = width.to_f
          return height * width
       elsif ( location == nil && polygon_id != nil)
         return @space.polygon.get_area
         
          
    # if the location was defined as "SPACE...", it is immediately followed by a
    # vertex, upon which lies the width of the roof
        elsif location.match(/SPACE.*/)
          location = location.sub( /^(.{6})/, "")
          width = @space.polygon.get_length(location)
          height = @floor.get_space_height
          return width * height
     # if the shape was a box, the width and height would be taken from the
     # "SPACE" object
        elsif ( @space.get_shape == "BOX" ) 
          width = @space.get_width
          height = @space.get_height
          return width * height
       else
         raise "The area could not be evaluated"
       end
  end
  
  
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
        # If it is a polygon or not defined, set to DOE default = 0.0
        return 0
      end
    end 
  
    # This method returns the Azimuth value as a FLOAT if it exists
    # It first checks if the azimuth keyword value is present within the roof
    # command itself. If it does not find this, then it checks for the location
    # keyword and assigns the correct azimuth depending on the azimuth of the parent
    # space. However, if the shape of the parent space is defined as a polygon, then it
    # searches for the location of the roof and uses the polygon's get-azimuth for the vertex
    # to return the azimuth of the roof
    
    #NOTE: The FRONT is defined as 0, going clockwise, ie. RIGHT = 90 degrees
    
    #OUTPUT: Azimuth between the parent SPACE and the ROOF
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
    
    # This method returns the Azimuth value as a FLOAT if it exists
    # It first checks if the azimuth keyword value is present within the roof
    # command itself. If it does not find this, then it checks for the location
    # keyword and assigns the correct azimuth depending on the azimuth of the parent
    # space. However, if the shape of the parent space is defined as a polygon, then it
    # searches for the location of the roof and uses the polygon's get-azimuth for the vertex
    # and adding it on to the overall azimuth to get the Absolute Azimuth from True North
   
    #NOTE: The FRONT is defined as 0, going clockwise, ie. RIGHT = 90 degrees
    
    #OUTPUT: Azimuth between ROOF and TRUE NORTH
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

    def get_skylights ()
      #retrieves the skylights in the roof
    end
    
end
end
