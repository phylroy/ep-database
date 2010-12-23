# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require 'doe_command'
module DOE2
class DOEDoor < DOE2::DOECommand
  #Contains uvalue of door
   def initialize
        super()
  end
  
  # This method finds the area of the door
  def get_area
    height = get_keyword_value("HEIGHT")
    width = get_keyword_value("WIDTH")
    
    if height == nil || width == nil
     raise "Error: In the command #{@utype}:#{@command_name} the area could not be evaluated. Either the HEIGHT or WIDTH is invalid.\n #{output}"
    end
    
    height = height.to_f
    width = width.to_f
    
    return height * width
  end
  
  def get_wall()
    #gets the wall on where the door is located
  end
end
end
