# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
required "doe_command"
module DOE2
  class DOE2::DOELighting
    # connected lighting power as mentioned in rule #4.3.4.1.(2)
    attr_accessor :lighting_power
    # type of lighting (incadecent or flourescent)
    attr_accessor :type
    # location of lighting (as mentioned in rule #4.3.4.1.(2))
    attr_accessor :location
    # proportion of radiant heat as defined in rule #4.3.4.1(2)
    attr_accessor :radiation_ratio
    # ratio of heat that goes into the space of the zone
    attr_accessor :space_heat
    # ration of heat that goes into the return air in the roof
    attr_accessor :returnair_heat
               
    def calculate_rad_ratio()
      #calculates the ratio of heat released through radiation out of total. 
      #returns a decimal number below one
    end
    
  end
end