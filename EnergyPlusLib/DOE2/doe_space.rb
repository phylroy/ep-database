# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
 require("doe_command")
module DOE2
class DOESpace < DOE2::DOECommand
  attr_accessor :polygon
  attr_accessor :zone
  def initialize
    super()
  end
  
  def output
    temp_string = basic_output()
#    if @polygon != nil
#      temp_string = temp_string + "$Polygon\n"
#      temp_string = temp_string +  "$\t#{@polygon.utype} = #{@polygon.commandName}\n"
#    end
#    if @zone != nil
#      temp_string = temp_string + "$Zone\n"
#      temp_string = temp_string +  "$\t#{@zone.utype} = #{@zone.commandName}\n"
#    end
  return temp_string
  end

  # This method finds the area of the space

  def get_area
    
    # get the keyword value of shape
    shape = get_keyword_value("SHAPE")
    
    # if the shape value is nil, or it is defined as "NO-SHAPE", the get_area value
    # would be defined, and would represent the get_area of the space
    if ( shape == nil || shape == "NO-SHAPE")
      area = get_keyword_value("AREA")
      area = area.to_f
      return area
      
    # if the shape value is "BOX", the height and width key values are given,
    # and the get_area would be defined as their product
    elsif ( shape == "BOX" )
        height = get_keyword_value("HEIGHT")
        width = get_keyword_value("WIDTH")
        height = height.to_f
        width = width.to_f
        return height * width
        
    # if the shape value is defined as a polygon , the get_area of the polygon would 
    # represent the get_area of the space
    elsif ( shape == "POLYGON")
        return @polygon.get_area
    else
      raise "Error: The area could not be evaluated. Please check inputs\n "
    
    end
    
    
  end
  
  # This method finds the volume of the space
  def get_volume
    
    # get the keyword value of "SHAPE"
    shape = get_keyword_value("SHAPE")
    
    # if the shape value returns nil, or is defined as "NO-SHAPE", the volume is
    # given directly
    if ( shape == nil || shape == "NO-SHAPE")
      volume = get_keyword_value("VOLUME")
      volume = volume.to_f
      return volume
    
    # if the shape is defined as a "BOX", the values for height, width, and
    # depth are given, from which you can get the volume
    elsif ( shape == "BOX" )
        height = get_keyword_value("HEIGHT")
        width = get_keyword_value("WIDTH")
        depth = get_keyword_value("DEPTH")
        height = height.to_f
        width = width.to_f
        depth = depth.to_f
        return height * width * depth
    
    # if the shape is defined as a "POLYGON", the get_area is defined as the area 
    # of the polygon, and the height is given by the value of "HEIGHT"
    elsif ( shape == "POLYGON")
        height = getKeywordvalue("HEIGHT")
        temp = get_keyword_value("POLYGON")
        height = height.to_f
        @polygon.utype = temp
        return @polygon.get_area * height
    else
      raise "Error: The volume could not be evaluated. Please check inputs\n "
      
    end
     
  end
  
  def get_height
    height = get_keyword_value("HEIGHT")
    height = height.to_f
    return height
  end
  
  def get_width
    width = get_keyword_value("WIDTH")
    width = width.to_f
    return width
  end
  
  def get_depth
    depth = get_keyword_value("DEPTH")
    depth = depth.to_f
    return depth
  end
  
  def get_shape
    return get_keyword_value("SHAPE")
  end
  
  def get_floor
    get_parent("FLOOR")
  end
  
  def get_absolute_azimuth()
    floor = get_floor()
    floor_azimuth = floor.get_absolute_azimuth
    if check_keyword?("AZIMUTH")
      azimuth = get_keyword_value("AZIMUTH")
    else
      azimuth = 0
    end
    return azimuth.to_f + floor_azimuth
  end
  
  def get_azimuth()
    if check_keyword?("AZIMUTH")
    azimuth = get_keyword_value("AZIMUTH")
    return azimuth.to_f
    else
      return 0
    end
  end
  
end 
end