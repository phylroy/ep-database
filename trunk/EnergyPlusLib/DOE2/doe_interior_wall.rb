# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 

 require("doe_construction_surface")
module DOE2
class DOEInteriorWall < DOE2::DOEConstructionSurface
  attr_accessor :floor, :space, :construction
  def initialize
        super()
  end
  
  # Finds the area of the interior wall
  
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
      elsif location.match(/SPACE.*/)
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
      area = get_keyword_value("AREA")
      rescue
      end
      
      begin
      width = get_keyword_value("WIDTH")
      height = get_keyword_value("HEIGHT")
      rescue
      end
   # if the width and height are not defined, then the get_area is directly entered
   # else, the get_area is entered and the width and height are not. If none of these
   # conditions hold, then an exception is raised for invalid inputs
   
      if ( area != nil)
        area = area.to_f
        return area
      elsif ( area == nil && width != nil && height != nil)
        width = width.to_f
        height = height.to_f
        return width * height
      else
        raise "The area could not be evaluated"
      
      end
    else
      raise "The area could not be evaluated"
     
    end

  
  end
  
  # This method returns the Azimuth as a FLOAT if it exists
  # It first finds the shape of the parent space class
  # Only if the parent shape is "NO-SHAPE" is the azimuth defined
  
  #OUTPUT: returns the Azimuth between the Interior Wall and the parent Space
  def get_azimuth
    space = get_parent("SPACE")
    shape = space.get_keyword_value("SHAPE")
    if shape == "NO-SHAPE"
      return get_keyword_value("AZIMUTH").to_f
    end
  end
  
  # This method returns the Absolute Azimuth as a FLOAT if it exists
  # It first finds the shape of the parent space class
  # Only if the parent shape is "NO-SHAPE" is the azimuth defined
  
  #OUTPUT: returns the azimuth between the Interior Wall and True North
  def get_absolute_azimuth
    space = get_parent("SPACE")
    shape = space.get_keyword_value("SHAPE")
    if shape == "NO-SHAPE"
      return get_keyword_value("AZIMUTH").to_f + space.get_absolute_azimuth()
    end
  end
  
end
end
