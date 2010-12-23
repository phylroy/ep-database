# To change this template, choose Tools | Templates
# and open the template in the editor.

module DOE22
  class BEPUReports
    #Class containing BEPU Report.
    class BEPUReport
      attr_accessor :bepu_meters_data_array
      attr_accessor :total_site_energy
      attr_accessor :total_site_energy_kbtu_per_sqf_gross
      attr_accessor :total_site_energy_kbtu_per_sqf_net
      attr_accessor :total_source_energy
      attr_accessor :total_source_energy_kbtu_per_sqf_gross
      attr_accessor :total_source_energy_kbtu_per_sqf_net
      attr_accessor :percent_hrs_outside_throttle_range
      attr_accessor :percent_hrs_plant_load_unsatisfied
      attr_accessor :bepu_report_text

      class BEPUMeterData
        attr_accessor :meter_name,
          :resource,
          :units,
          :lights,
          :task_lights,
          :misc_equip,
          :space_heating,
          :space_cooling,
          :heat_reject,
          :pumps_aux,
          :vent_fans,
          :refrig_display,
          :ht_pump_supplem,
          :dhw,
          :ext_usage


        def set(meter_name,resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux,vent_fans,refrig_display,ht_pump_supplem,dhw,ext_usage)
          @meter_name,@resource,@units,@lights,@task_lights,@misc_equip,@space_heating,@space_cooling,@heat_reject,@pumps_aux,@vent_fans,@refrig_display,@ht_pump_supplem,@dhw,@ext_usage = meter_name,resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux,vent_fans,refrig_display,ht_pump_supplem,dhw,ext_usage

        end

        def output
          #puts "meter_name,resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux,refrig_display,ht_pump_supplem,dhw,ext_usage"
          array = [ @meter_name,@resource,@units,@lights,@task_lights,@misc_equip,@space_heating,@space_cooling,@heat_reject,@pumps_aux,@vent_fans,@refrig_display,@ht_pump_supplem,@dhw,@ext_usage]
          string = ""
          array.each do |item|
            string = string + item.to_s + ","
          end
          puts string
        end

      end

      def output_text
        puts @bepu_report_text
        puts " "
#        puts "BEPU Report"
#        puts "meter_name,resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux, vent_fans, refrig_display,ht_pump_supplem,dhw,ext_usage"
#        @bepu_meters_data_array.each do |meter|
#          meter.output
#        end
#        puts @total_site_energy
#        puts @total_site_energy_kbtu_per_sqf_gross
#        puts @total_site_energy_kbtu_per_sqf_net
#        puts @total_source_energy
#        puts @total_source_energy_kbtu_per_sqf_gross
#        puts @total_source_energy_kbtu_per_sqf_net
#        puts @percent_hrs_outside_throttle_range
#        puts @percent_hrs_plant_load_unsatisfied
      end

      def initialize(lines)
        @bepu_report_text = lines
        #Need to find a BEPU report, get store in into an array of strings. May have more than one.
        @bepu_meters_data_array = Array.new()
        lines.each_with_index do |line,line_number|
          if line.match(/REPORT- BEPU Building Energy Performance/)
          end
          if line.match(/^\s*(.M1)\s*(ELECTRICITY|NATURAL-GAS)/)
            meter_name = $1
            resource = $2
            if lines[line_number + 1].match(/^\s*(KWH|THERM)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)/)
              meter = BEPUMeterData.new()
              meter.set(meter_name,resource,$1.strip,$2.strip.to_f,$3.strip.to_f,$4.strip.to_f,$5.strip.to_f,$6.strip.to_f,$7.strip.to_f,$8.strip.to_f,$9.strip.to_f,$10.strip.to_f,$11.strip.to_f,$12.strip.to_f,$13.strip.to_f  )
              #meter.output
              @bepu_meters_data_array.push(meter)
            end
          end
          if line.match(/TOTAL ELECTRICITY\s*(.*)\s*KWH\s*(.*)\s*KWH\s*\/SQFT-YR GROSS-AREA\s*(.*)\s*KWH\s*\/SQFT-YR NET-AREA/)
            @total_site_energy = $1.strip.to_f
            @total_site_energy_kbtu_per_sqf_gross = $2.strip.to_f
            @total_site_energy_kbtu_per_sqf_net = $3.strip.to_f
          end
          if line.match(/NATURAL-GAS\s*(.*)\s*THERM\s*(.*)\s*THERM\s*\/SQFT-YR GROSS-AREA\s*(.*)\s*THERM\s*\/SQFT-YR NET-AREA/)
            @total_source_energy = $1.strip.to_f
            @total_source_energy_kbtu_per_sqf_gross = $2.strip.to_f
            @total_source_energy_kbtu_per_sqf_net = $3.strip.to_f
          end
          if line.match(/PERCENT OF HOURS ANY SYSTEM ZONE OUTSIDE OF THROTTLING RANGE =\s*(.*)/)
            @percent_hrs_outside_throttle_range = $1.strip.to_f
          end
          if line.match(/PERCENT OF HOURS ANY PLANT LOAD NOT SATISFIED\s*=\s*(.*)/)
            @percent_hrs_plant_load_unsatisfied= $1.strip.to_f
          end
        end
      end
    end

    attr_accessor :bepu_reports_array

    def initialize()
      @bepu_reports_array = Array.new()
    end
    
    def scan(lines,line,line_number)

      #Find BEPU Report and save to string array.
      if line.match(/REPORT- BEPU Building Utility Performance/)
        report_text = Array.new()
        line_counter = 0
        until lines[line_number + line_counter].match(/\s*NOTE:  ENERGY IS APPORTIONED HOURLY TO ALL END-USE CATEGORIES\./)
          report_text.push(lines[line_number + line_counter])
          line_counter = line_counter+1
        end
        bepu_report = BEPUReport.new(report_text)
        #bepu_report.output
        @bepu_reports_array.push(bepu_report)

      end

    end

    def output_text

      @bepu_reports_array.each do |report|

        report.output_text
      end
    end



  end
end
