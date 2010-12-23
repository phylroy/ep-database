# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_command")
module DOE2
  class DOEConstructionSurface < DOE2::DOECommand
    attr_accessor :construction
    
    def initialize
      super()
    end
  
   # This method finds all the commands within the building that are "Construction"
   # and if the utype matches, it gets the construction
    def determine_user_defined_construction()
      constructions = @building.find_all_commands("CONSTRUCTION")
      constructions.each do |construction| 
     
        if ( construction.utype == get_keyword_value("CONSTRUCTION") )
          @construction = construction
        end
      end
    end
  
  
   # This method finds the u-value of the given construction
   # Output => total conductivity as a float
    def get_u_value()
      # Gets the constuction from the user input file if available.  
      determine_user_defined_construction()
    
      bdllib = DOE2::DOEBDLlib.instance

      materials = Array.new
      total_conductivity = 0.0
      material_command = ""
   
    
      type = @construction.get_keyword_value("TYPE")
    
      if ( type == "LAYERS" )
        
        layers = @construction.get_keyword_value("LAYERS")
      
        # finds the command associated with the layers keyword
      
        layers_command = building.find_command_with_utype(layers)

      
      
        # if there ends up to be more than one command with the layers keyword
        # raise an exception
        if layers_command.length > 1
          raise "Construction was defined more than once #{@construction.utype}"
      
          # if the layers command is found within the file, and there is only one
        elsif layers_command.length == 1
          
      
          layer = layers_command[0]
          
          temp_material = layer.get_keyword_value("MATERIAL")



        
          # get all the materials, separate it by the quotation marks and push it
          # onto the materials array
          materials = temp_material.scan(/(\".*?\")/)
     
          # for each of the materials, try to find the command associated with that
          # utype for material.
          materials.each do |material|
            
            material = material.to_s
          
            material_command_array = building.find_command_with_utype(material)
          
          
            # if there ends up to be more than one, raise an exception
            if material_command_array.length > 1
              raise "Material was defined more than once #{material}"
            end
          
          
            # if the material cannot be found within the file, find it within the database
            if material_command_array.length < 1

              material_command = bdllib.find_material(material)

            else
             
              material_command = material_command_array[0]

              
            end            
          

            # if the type keyword defined within the material is resistance, then the 
            # conductivity would be the inverse of this

            if ( material_command.get_keyword_value("TYPE") == "RESISTANCE" )
              
              resistance = material_command.get_keyword_value("RESISTANCE")
            
              resistance = resistance.to_f
              conductivity = 1 / resistance

              # if the type keyword defined within the material is properties, then the
              # conductivity is also defined
            elsif material_command.get_keyword_value("TYPE") == "PROPERTIES"
              conductivity = material_command.get_keyword_value("CONDUCTIVITY")
              conductivity = conductivity.to_f
            end


            # sum up all of the conductivity of all the materials and store it into 
            # total_conductivity
            total_conductivity = total_conductivity + conductivity
          end

          # if the layers command is not found within the fiile, then we find it within
          # the database
        else
          layer = bdllib.find_layer(layers)

          temp_material = layer[0].get_keyword_value("MATERIAL")
          materials = temp_material.scan(/(\".*?\")/)

          # for each material that it matches, grabs the resistance or conductivity
          # and sums it up into total_conductivity
          materials.each do |material|
            material_command = bdllib.find_material(material)

            if material_command.get_keyword_value("TYPE") == "RESISTANCE"
              resistance = material_command.get_keyword_value("RESISTANCE")
              resistance = resistance.to_f
              conductivity = 1 / resistance
            elsif material_command.get_keyword_value("TYPE") == "PROPERTIES"
              conductivity = material_command.get_keyword_value("CONDUCTIVITY")
              conductivity = conductivity.to_f
            else
              raise "Error in material properties"
            end
            total_conductivity = total_conductivity + conductivity
          end

        end
      elsif type == "U-VALUE"
        total_conductivity = @construction.get_keyword_value("U-VALUE")
        total_conductivity = total_conductivity.to_f
      end
      return total_conductivity
    end
  end
end