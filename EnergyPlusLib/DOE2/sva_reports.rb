# To change this template, choose Tools | Templates
# and open the template in the editor.

module DOE22
  class SVAReports
    #Class containing SVA Report.
    class SVAReport
      attr_accessor :sva_report_text
      # Data relevant to the system itself
      attr_accessor :sva_meters_data_array
      attr_accessor :system_name
      attr_accessor :system_type
      attr_accessor :system_oa_ratio
      # Data relevant to fans used in the system
      attr_accessor :fan_type
      attr_accessor :fan_capacity
      attr_accessor :fan_diversity_fact
      attr_accessor :fan_power_demand
      attr_accessor :fan_delta_t
      attr_accessor :fan_static_pressure
      attr_accessor :fan_total_eff
      attr_accessor :fan_mech_eff
      # Data relevant to each zones
      attr_accessor :zone_name
      attr_accessor :zone_sa
      attr_accessor :zone_ea
      attr_accessor :zone_fan_power_demand
      attr_accessor :zone_oa

      class SVAMeterData
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
          puts "meter_name,resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux,refrig_display,ht_pump_supplem,dhw,ext_usage"
          array = [ @meter_name,@resource,@units,@lights,@task_lights,@misc_equip,@space_heating,@space_cooling,@heat_reject,@pumps_aux,@vent_fans,@refrig_display,@ht_pump_supplem,@dhw,@ext_usage]
          string = ""
          array.each do |item|
            string = string + item.to_s + ","
          end
          puts string
        end

      end

      def output_text
#        puts "SVA Report"
#        puts "meter_name,resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux, vent_fans, refrig_display,ht_pump_supplem,dhw,ext_usage"
#        @sva_meters_data_array.each do |meter|
#          meter.output
#        end

      end

      def initialize(lines)
        @sva_report_text = lines
        #Need to find a SVA report, get store in into an array of strings. May have more than one.
        @sva_meters_data_array = Array.new()
        lines.each_with_index do |line,line_number|
          if line.match(/REPORT- SV-A System Design Parameters for\s*(.*)WEATHER FILE/)
            @system_name.push($1.strip)
            lines[line_number + 6].match(/(\S*)\s*\S*\s*\S*\s*\S*\s*(\S*)/)
            @system_type.push($1)
            @system_oa_ratio.push($2)
          end
          if line.match(/\s*FAN\s*CAPACITY\s*FACTOR/)
            line_counter = 0
            until lines[line_number + 3 + line_counter] = nil
              lines[line_number + 3 + line_counter].match(/\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)/)
              @fan_type.push($1)
              @fan_capacity.push($2)
              @fan_diversity_fact.push($3)
              @fan_power_demand.push($4)
              @fan_delta_t.push($5)
              @fan_static_pressure.push($6)
              @fan_total_eff.push($7)            
              @fan_mech_eff.push($8)
              line_counter = line_counter+1
            end
          end
          if line.match(/\s*ZONE\s*FLOW\s*FLOW\s*FAN/)
            line_counter = 0
            until lines[line_number + 3 + line_counter].match(/REPORT- SV-A System Design Parameters for/ ) or lines[line_number + 3 + line_counter].match(/REPORT- SS-D Building HVAC Load Summary/ )
              lines[line_number + 3 + line_counter].match(/\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)/)
              defsdc
            end

          end



          if lines[line_number + 1].match(/^\s*(MBTU)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)/)
              meter = SVAMeterData.new()
              meter.set(meter_name,resource,$1.strip,$2.strip.to_f,$3.strip.to_f,$4.strip.to_f,$5.strip.to_f,$6.strip.to_f,$7.strip.to_f,$8.strip.to_f,$9.strip.to_f,$10.strip.to_f,$11.strip.to_f,$12.strip.to_f,$13.strip.to_f  )
              #meter.output
              @sva_meters_data_array.push(meter)            
          end
        end
      end
    end



    attr_accessor :sva_reports_array

    def initialize()
      @sva_reports_array = Array.new()
    end

    def scan(lines,line,line_number)
      #Find SVA Report and save to string array.
      if line.match(/REPORT- SV-A System Design Parameters for/)
        report_text = Array.new()
        line_counter = 0
        until lines[line_number + 1 + line_counter].match(/REPORT- SV-A System Design Parameters for/ ) or lines[line_number + 1 + line_counter].match(/REPORT- SS-D Building HVAC Load Summary/ )
          report_text.push(lines[line_number + line_counter])
          line_counter = line_counter+1
        end
        sva_report = SVAReport.new( report_text )
        #sva_report.output_text
        @sva_reports_array.push( sva_report )        
      end
    end

    def output_text
      @sva_reports_array.each do |report|
        report.output
      end
    end
  end
end




