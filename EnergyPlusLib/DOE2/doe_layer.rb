# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_command")
module DOE2
class DOELayer < DOE2::DOECommand
  # type of material (see rule #4.3.5.2.(3))
  attr_accessor :material
  # the thickness of the material (see rule #4.3.5.2.(3))
  attr_accessor :thickness
  def initialize
    super()
  end
end
end
