# To change this template, choose Tools | Templates
# and open the template in the editor.
require("excel_chart")
module DOE22

  def pair(&block)
    varname = block.call.to_s
    return [varname,eval(varname,block) ]
  end


      class BEPSReport
      attr_accessor :beps_meters_data_array
      attr_accessor :total_site_energy
      attr_accessor :total_site_energy_kbtu_per_sqf_gross
      attr_accessor :total_site_energy_kbtu_per_sqf_net
      attr_accessor :total_source_energy
      attr_accessor :total_source_energy_kbtu_per_sqf_gross
      attr_accessor :total_source_energy_kbtu_per_sqf_net
      attr_accessor :percent_hrs_outside_throttle_range
      attr_accessor :percent_hrs_plant_load_unsatisfied
      attr_accessor :beps_report_text
      attr_accessor:run_name

      class BEPSMeterData
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
          #puts "meter_name",resource,units,lights,task_lights,misc_equip,space_heating,space_cooling,heat_reject,pumps_aux,refrig_display,ht_pump_supplem,dhw,ext_usage"



          array = [ @meter_name,@resource,@units,@lights,@task_lights,@misc_equip,@space_heating,@space_cooling,@heat_reject,@pumps_aux,@vent_fans,@refrig_display,@ht_pump_supplem,@dhw,@ext_usage]
          string = ""
          array.each do |item|
            string = string + item.to_s + ","
          end
          puts string

        end

        def get_array
          array = [ @meter_name,@resource,@units,@lights,@task_lights,@misc_equip,@space_heating,@space_cooling,@heat_reject,@pumps_aux,@vent_fans,@refrig_display,@ht_pump_supplem,@dhw,@ext_usage]
        end


      end

      def output_excel_histogram

        header = ["meter_name","resource","units","lights","task_lights","misc_equip","space_heating","space_cooling","heat_reject","pumps_aux","vent_fans","refrig_display","ht_pump_supplem","dhw","ext_usage"]
        chart = ExcelChart.new(@run_name + "BEPS-Histo" ,header)

        @beps_meters_data_array.each do |meter|
          values = meter.get_array()
          chart.add_data_row(values)
        end
        chart.chart_stacked()
        puts @total_site_energy
        puts @total_site_energy_kbtu_per_sqf_gross
        puts @total_site_energy_kbtu_per_sqf_net
        puts @total_source_energy
        puts @total_source_energy_kbtu_per_sqf_gross
        puts @total_source_energy_kbtu_per_sqf_net
        puts @percent_hrs_outside_throttle_range
        puts @percent_hrs_plant_load_unsatisfied

      end


      def output_excel_pie
        header = ["meter_name","resource","units","lights","task_lights","misc_equip","space_heating","space_cooling","heat_reject","pumps_aux","vent_fans","refrig_display","ht_pump_supplem","dhw","ext_usage"]
        chart = ExcelChart.new(@run_name + "BEPS-Pie" ,header)
        sum_array = Array.new()
        @beps_meters_data_array.each do |meter|
          values = meter.get_array()
          values.each_with_index do |value , i|
            if i > 3
              sum_array[i] = sum_array[i].to_f + value
            end
          end

        end
        chart.add_data_row(sum_array)
        chart.chart_pie()
      end

      def initialize(run_name,lines)
        @run_name = run_name
        @beps_report_text = lines
        #Need to find a BEPS report, get store in into an array of strings. May have more than one.
        @beps_meters_data_array = Array.new()
        lines.each_with_index do |line,line_number|
          if line.match(/REPORT- BEPS Building Energy Performance/)
          end
          if line.match(/^\s*(.M1)\s*(ELECTRICITY|NATURAL-GAS)/)
            meter_name = $1
            resource = $2
            if lines[line_number + 1].match(/^\s*(MBTU)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)/)
              meter = BEPSMeterData.new()
              meter.set(meter_name,resource,$1.strip,$2.strip.to_f,$3.strip.to_f,$4.strip.to_f,$5.strip.to_f,$6.strip.to_f,$7.strip.to_f,$8.strip.to_f,$9.strip.to_f,$10.strip.to_f,$11.strip.to_f,$12.strip.to_f,$13.strip.to_f  )
              #meter.output
              @beps_meters_data_array.push(meter)
            end
          end
          if line.match(/TOTAL SITE ENERGY\s*(.*)\s*MBTU\s*(.*)KBTU\/SQFT-YR GROSS-AREA\s*(.*)KBTU\/SQFT-YR NET-AREA/)
            @total_site_energy = $1.strip.to_f
            @total_site_energy_kbtu_per_sqf_gross = $2.strip.to_f
            @total_site_energy_kbtu_per_sqf_net = $3.strip.to_f
          end
          if line.match(/TOTAL SOURCE ENERGY\s*(.*)\s*MBTU\s*(.*)KBTU\/SQFT-YR GROSS-AREA\s*(.*)KBTU\/SQFT-YR NET-AREA/)
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

      def get_space_heating()
        total= 0.0
        @beps_meters_data_array.each do |meter|
          total += meter.space_heating.to_f
        end
        return total
      end

      def get_space_cooling()
        total= 0.0
        @beps_meters_data_array.each do |meter|
          total += meter.space_cooling.to_f
        end
        return total
      end

      def get_vent_fans()
        total= 0.0
        @beps_meters_data_array.each do |meter|
          total += meter.vent_fans.to_f
        end
        return total
      end
    end
  

  class BEPSReports
    #Class containing BEPS Report.

    attr_accessor :beps_reports_array
    attr_accessor :run_name
    def initialize(run_name)
      @beps_reports_array = Array.new()
      @run_name = run_name
    end
    def scan(lines,line,line_number)

      #Find BEPS Report and save to string array.
      if line.match(/REPORT- BEPS Building Energy Performance/)
        report_text = Array.new()
        line_counter = 0
        until lines[line_number + line_counter].match(/\s*NOTE\:  ENERGY IS APPORTIONED HOURLY TO ALL END-USE CATEGORIES\./)
          report_text.push(lines[line_number + line_counter])
          line_counter = line_counter+1
        end
        beps_report = BEPSReport.new( @run_name, report_text )
        #beps_report.output
        @beps_reports_array.push( beps_report )
      end
    end
    def output_text
      @beps_reports_array.each do |report|
        puts @beps_report_text
      end
    end
    def output_excel
      @beps_reports_array.each do |report|
        report.output_excel_histogram
        report.output.excel_pie
      end
    end
    def total_heating
      total = 0
      @beps_reports_array.each do |report|
        total += report.get_space_heating()
      end
      return total
    end
    def total_cooling
      total = 0
      @beps_reports_array.each do |report|
        total += report.get_space_cooling()
      end
      return total
    end
    def total_vent_fans
      total = 0
      @beps_reports_array.each do |report|
        total += report.get_vent_fans()
      end
      return total
    end

  end
end



