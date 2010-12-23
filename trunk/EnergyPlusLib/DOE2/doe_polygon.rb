# 
# To change this template, choose Tools | Templates
# and open the template in the editor.
 

require("doe_command")

module DOE2
  #This class makes it easier to deal with DOE Polygons. 
  class DOEPolygon < DOE2::DOECommand

    #The constructor.
    def initialize
      super()
    end

    # This method returns the area of the polygon.
    def get_area
      points_list = Array.new()
      #Convert Keywork Pairs to points.
      @keywordPairs.each do |array|
        
        array[1].match(/\(\s*(\d*\.?\d*)\s*\,\s*(\d*\.?\d*)\s*\)/)
        #puts array[1]
        points_list.push([$1.to_f,$2.to_f])
        #puts "#{$1.to_f},#{$2.to_f}"

      end 
      #Determine the get_area of the polygon with the cross product     
      p0 = points_list.last
      area = 0
      points_list.each { |p|
        #p "get_area = #{p0[0]}*#{p[1]} - #{p[0]}*#{p0[1]}"
        area += p0[0]*p[1] - p[0]*p0[1]
        p0 = p
      }
      if area == 0.0
        raise ("Polygon area cannot be zero. \n #{output}")
      end
      return (area / 2.0).abs
    end
  
  
    # This method must determine the length of the given point to the next point 
    # in the polygon list. If the point is the last point, then it will be the 
    # distance from the last point to the first. 
    # point_name is the string named keyword in the keyword pair list. 
    # Example:  
    # "DOEPolygon 2" = POLYGON         
    #   V1               = ( 0, 0 )
    #   V2               = ( 0, 1 )
    #   V3               = ( 2, 1 )
    #   V4               = ( 2 ,0 )
    # get_length("V4") should return "2"
    # get_length("V3") should return "1"

    def get_length(point_name)
      x = Array.new()
      y = Array.new()
  
      #files = Files.new()
      found = 0
      for i in 0..@keywordPairs.length - 1
        temp = @keywordPairs[i]
        if temp[0] == point_name
          found = 1
          value1 = @keywordPairs[i]
          value2 = @keywordPairs[i+1]
          if @keywordPairs[i+1] == nil
            value2 = @keywordPairs[0]
          end
        end   
      end
      if (found == 0)
        raise "Error: In the command #{@utype}:#{@command_name} the length could not be evaluated. The point name is invalid.\n #{output}"
      end
      value1[1].match(/\s(.*),(.*)\s/)
      x1 = $1
      y1 = $2.lstrip
      x1 = x1.to_f
      y1 = y1.to_f
      x.push(x1)
      y.push(y1)
      value2[1].match(/\s(.*),(.*)\s/)
      x2 = $1
      y2 = $2.lstrip
      x2 = x2.to_f
      y2 = y2.to_f
      x.push(x2)
      y.push(y2)
   
      length = Math.hypot((x[1]-x[0]), (y[1]-y[0]))
      return length
  
    end  
  
        # This method must determine the length of the given point to the next point 
    # in the polygon list. If the point is the last point, then it will be the 
    # distance from the last point to the first. 
    # point_name is the string named keyword in the keyword pair list. 
    # Example:  
    # "DOEPolygon 2" = POLYGON         
    #   V1               = ( 0, 0 )
    #   V2               = ( 0, 1 )
    #   V3               = ( 2, 1 )
    #   V4               = ( 2 ,0 )
    # get_length("V4") should return "2"
    # get_length("V3") should return "1"

    #Returns the azimuth in degrees for the point given.
    def get_azimuth(point_name)
      x = Array.new()
      y = Array.new()
  
      #files = Files.new()
      found = 0
      for i in 0..@keywordPairs.length - 1
        temp = @keywordPairs[i]
        if temp[0] == point_name
          found = 1
          value1 = @keywordPairs[i]
          value2 = @keywordPairs[i+1]
          if @keywordPairs[i+1] == nil
            value2 = @keywordPairs[0]
          end
        end   
      end
      if (found == 0)
        raise "Error: In the command #{@utype}:#{@command_name} the length could not be evaluated. The point name is invalid.\n #{output}"
      end
      value1[1].match(/\s(.*),(.*)\s/)
      x1 = $1
      y1 = $2.lstrip
      x1 = x1.to_f
      y1 = y1.to_f
      x.push(x1)
      y.push(y1)
      value2[1].match(/\s(.*),(.*)\s/)
      x2 = $1
      y2 = $2.lstrip
      x2 = x2.to_f
      y2 = y2.to_f
      x.push(x2)
      y.push(y2)
      azimuth = (Math.atan2((y[1]-y[0]),(x[1]-x[0]) ) ) * 180.0 / Math::PI

      return azimuth
  
    end  
    
   
    
    
    
    
  end
end