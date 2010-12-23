# To change this template, choose Tools | Templates
# and open the template in the editor.
require 'win32ole'
require 'beps_reports'
require 'bepu_reports'
require 'esd_report'
require 'sva_reports'
module DOE22


  #LS-C Report
  class LSCReport

    attr_accessor :floor_area,:volume
    def initialize(lines)
      lines.each_with_index do |line,line_number|

        if line.match(/^\s*FLOOR  AREA \s*(\d*).*$/)
          @floor_area = $1.strip.to_f
        end

        if line.match(/^\sVOLUME \s*(\d*).*$/)
          @volume = $1.strip.to_f
        end
        
      end
    end
  end
  class LVDReport

    def initialize(lines)
      @headers = [ "NAME","AVERAGE U-VALUE/WINDOWS", "AVERAGE U_VALUES/WALLS", "AVERAGE U-VALUE WALLS+WINDOWS", "WINDOW AREA", "WALL AREA", "WINDOW+WALL AREA" ]
      @units   = ["","(BTU/HR-SQFT-F)","(BTU/HR-SQFT-F)","(BTU/HR-SQFT-F)","(SQFT)","(SQFT)", "(SQFT)"  ]
      @identifier = "AVERAGE             AVERAGE         AVERAGE U-VALUE         WINDOW         WALL           WINDOW+WALL"
      @skip = 6
      @enddata = 2
      @endtype = "return_type"
      lines.each_with_index do |line,line_number|
        #line == target_match
        lvd_data_table = Table.new(@identifier, @headers, @units, @enddata, @end_type, @lines, @skip )
      end
    end

  end
  class DOESim
    attr_accessor :filename
    attr_accessor :lines
    attr_accessor :beps_reports
    attr_accessor :bepu_reports
    attr_accessor :esd_report
    attr_accessor :zones
    attr_accessor :plants
    attr_accessor :systems

    def initialize()
    end

    def strip_dates()
      lines[0].match(/.(\S*)/)
      file_name = $1
      rgex_key = "(.*)." + file_name
      newlines = Array.new()
      @lines.each do |line|
        if line.match(rgex_key)
          line = $1
        end
        if line.match(/--------------------------------------------------------------------------------------------------------------\(CONTINUED\)--------/)
          newlines.pop
          newlines.pop
        else
          newlines.push(line)
        end
      end
      @lines = newlines
    end

    def read_sim_file(filename)
      #Open the file.
      f = File.open(filename, "r")
      #Read the file into an array, line by line.
      @lines = f.readlines
      strip_dates()
      puts run_name = File.basename(filename, "sim")
      #Initialize report Objects.
      @beps_reports = BEPSReports.new(run_name)
      @bepu_reports = BEPUReports.new()
      @esd_report  = ESDReport.new()
      #@sva_reports = SVAReports.new()
      #Iterate over file.
      @lines.each_with_index do |line,line_number|
        #Check if BEPS was found on this line.
        @beps_reports.scan(@lines,line,line_number)
        #Check if BEPU was found on this line.
        @bepu_reports.scan(@lines,line,line_number)
        #check if ESD is found on this line.
        @esd_report.scan(@lines, line, line_number)
        #check if SSV report is found on this line.
        #@sva_reports.scan(@lines, line, line_number)
        
      end

    
      @lsc_report  = LSCReport.new(@lines) #Floor Area
      #puts "Floor Area," + @lsc_report.floor_area.to_s
    end

    def output_text()
      @beps_reports.output_text
      #@bepu_reports.output_text
      #@esd_report.output_text
    end


    def get_underheated_zones()
      @lines.each_with_index do|line, i|
        if (myarray = line.match(/REPORT\- SS\-R Zone Performance Summary for (.*)/) )
          counter = 9 + i
          while @lines[counter].strip != ""
            # Get Zone name
            zonename = @lines[counter ].strip
            @lines[counter + 1 ].match(/.{36}(.{8})/)
            #Get underheated hours
    
            undermet_hours = $1.strip
            if undermet_hours.to_f > 99
              puts zonename + "\t" + undermet_hours
            end
            counter = counter + 2
          end
        end
      end
    end

    def total_space_heating_cost()
      report.bepu_meters_data_array.each do |meter|
        total_energy += meter.space_heating

        case
        when meter.resource == "ELECTRICITY"
          total_electricity += total_energy.to_f
        when meter.resource == "NATURAL-GAS"
          total_natural_gas += total_energy.to_f
        when meter.resource == "PROPANE"
          total_propane += total_energy.to_f
        end
      end
    end



    def regulated_energy_cost()
      #To Do: Check if simulation results for ES-D and BEPU were created.
      total_electricity = 0
      total_natural_gas = 0
      total_propane = 0
      cost_natural_gas = 0
      cost_electric = 0
      cost_propane = 0

      @bepu_reports.bepu_reports_array.each do |report|

        report.bepu_meters_data_array.each do |meter|
          #Sum energy for Meter.
          total_energy = 0.0
          total_energy += meter.lights
          total_energy += meter.space_heating
          total_energy += meter.space_cooling
          total_energy += meter.heat_reject
          total_energy += meter.pumps_aux
          total_energy += meter.vent_fans
          total_energy += meter.ht_pump_supplem
          total_energy += meter.dhw
          case
          when meter.resource == "ELECTRICITY"
            total_electricity += total_energy.to_f
          when meter.resource == "NATURAL-GAS"
            total_natural_gas += total_energy.to_f
          when meter.resource == "PROPANE"
            total_propane += total_energy.to_f
          end
        end

        puts sprintf("Total Elec   (KHW)   = %12.2f",total_electricity)
        puts sprintf("Total NatGas (Th)    = %12.2f",total_natural_gas)
        puts sprintf("Total Propane(?)     = %12.2f",total_propane)
        #Now Change the energy into cost usind ES-D data.
        @esd_report.meters_data_array.each do |meter|
          case
          when meter.resource == "ELECTRICITY"
            cost_electric = meter.virt_cost_per_unit.to_f * total_electricity.to_f
          when meter.resource == "NATURAL-GAS"
            cost_natural_gas= meter.virt_cost_per_unit.to_f * total_natural_gas.to_f
          when meter.resource == "PROPANE"
            cost_propane = meter.virt_cost_per_unit.to_f * total_natural_gas.to_f
          end
        end
        puts sprintf("Total Elec    Cost($)= %12.2f",cost_electric)
        puts sprintf("Total NatGas  Cost($)= %12.2f",cost_natural_gas)
        puts sprintf("Total Propane Cost($)= %12.2f",cost_propane)

      end
        total_cost = cost_electric + cost_natural_gas + cost_propane
        puts sprintf("Total Cost        ($)= %12.2f",total_cost)
      return total_cost
    end
  end
end
