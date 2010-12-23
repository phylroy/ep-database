# 
# To change this template, choose Tools | Templates
# and open the template in the editor. 

required "doe_command"
module DOE2
  class DOE2::DOEHeatTransfer
    #pointers to adjacent thermal block and wall between the two blocks
    attr_accessor :adj_thermal_block
    attr_accessor :adj_wall
       
    #returns the wall between two thermal blocks/zone 
    def adjacent_wall(zone)
      
    end      
  end
end 