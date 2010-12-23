# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_command")
module DOE2
class DOEMaterial < DOE2::DOECommand
  # characteristics of the materials according to
  # rule #4.3.5.2.(3b)
  attr_accessor :density
  attr_accessor :specific_heat
  attr_accessor :thermal_conductivity
  def initialize
    super()
  end
end
end
