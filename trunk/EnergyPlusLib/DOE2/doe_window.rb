# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 
require("doe_command")

module DOE2
  class DOEWindow < DOE2::DOECommand
    #initializes the solar heat gain coefficient
    attr_accessor :shgc, :type, :extra_shading
    def initialize
      super()
    end
    
    # This method returns the area of the window
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


    def get_absolute_azimuth
      get_parent("EXTERIOR-WALL").get_absolute_azimuth
    end


    # This method finds the tilt of the window
    def get_tilt()
      get_parent("EXTERIOR-WALL").get_tilt
    end
    
    #This method gets the wall on which the window is on
    def get_wall
      get_parent("EXTERIOR-WALL")
    end
    
    #converts the solar heat gain coefficient to form appropriate for
    #energy analysis module just before transfer    
    def convert_shgc ()
      
    end

    def set_fenestration_to_wall_ratio(percentage)
      #Get Parent Height
      if percentage >= 0.99
        percentage = 0.99
      end

      if percentage < 0.01
        percentage = 0.01
      end
      parent_height = get_parent("EXTERIOR-WALL").get_height.to_f

      parent_width = get_parent("EXTERIOR-WALL").get_width.to_f
      #Get Parent Width
      #Get Framing THickness

      window_height = Math.sqrt(parent_height * parent_height * percentage) - 2 * get_keyword_value("FRAME-WIDTH").to_f
      window_width = Math.sqrt(parent_width * parent_width * percentage) - 2 * get_keyword_value("FRAME-WIDTH").to_f

      if window_height < 0.001
        window_height = 0.001
      end

      if window_width < 0.001
        window_width = 0.001
      end

      x = ( parent_width - window_width ) / 2.0 
      y = ( parent_height - window_height ) / 2.0 

      set_keyword_value("HEIGHT", window_height.to_s)
      set_keyword_value("WIDTH",window_width.to_s)
      set_keyword_value("X", x.to_s)
      set_keyword_value("Y", y.to_s)
      #puts (window_width * window_height) / (parent_width * parent_height)
      #puts utype + " Win/Wall set to " + (percentage * 100.0).to_s
    end

    def get_fwr()
      get_area.to_f / get_parent("EXTERIOR-WALL").get_area()
    end


  end
end
