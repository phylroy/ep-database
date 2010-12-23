#Class Containing ES-D Report.
class ESDReport
  attr_accessor :energy_cost_gross_bldg_area
  attr_accessor :energy_cost_net_bldg_area
  attr_accessor :meters_data_array
  attr_accessor :esd_report_text

  class ESDMeterData
    attr_accessor :utility_rate
    attr_accessor :resource
    attr_accessor :meter_name
    attr_accessor :units_per_year
    attr_accessor :units
    attr_accessor :cost
    attr_accessor :virt_cost_per_unit
    attr_accessor :all_year


    def set(utility_rate, resource,meter_name,units_per_year,units,cost,virt_cost_per_unit,all_year)
      @utility_rate, @resource,@meter_name,@units_per_year,@units,@cost,@virt_cost_per_unit,@all_year = utility_rate, resource,meter_name,units_per_year,units,cost,virt_cost_per_unit,all_year
    end

    def output
      puts @utility_rate, @resource,@meter_name,@units_per_year,@units,@cost,@virt_cost_per_unit,@all_year
      puts "\n"
    end
  end

  def initialize()
    @meters_data_array = Array.new()
  end

  def read(lines)
    @esd_report_text = lines
    @beps_meters_data_array = Array.new()
    lines.each_with_index do |line,line_number|
      if line.match(/REPORT- ES-D Energy Cost Summary/)
      end
      if line.match(/^(.{32})\s{3}(ELECTRICITY\s{5}|NATURAL-GAS\s{5})\s{3}(.{11})\s{3}(.{10})\s(.{8})\s{3}(.{10})\s{3}(.{10})\s*(YES|NO)\s*$/)
        meter = ESDMeterData.new()
        meter
        meter.set($1.strip,$2.strip,$3.strip,$4.strip.to_f,$5.strip,$6.strip.to_f,$7.strip.to_f,$8.strip)
        @meters_data_array.push(meter)
      end

      if line.match(/^\s*ENERGY COST\/GROSS BLDG AREA\:\s*(.*)$/)
        @energy_cost_gross_bldg_area = $1.strip.to_f
      end

      if line.match(/^\s*ENERGY COST\/NET BLDG AREA\:\s*(.*)$/)
        @energy_cost_net_bldg_area = $1.strip.to_f
      end
    end
  end

  def scan(lines,line,line_number)
    if line.match(/REPORT- ES-D Energy Cost Summary/)
      report_text = Array.new()
      line_counter = -1
      begin
        line_counter = line_counter + 1
        report_text.push(lines[line_number + line_counter])
      end until lines[line_number + line_counter].match(/^\s*ENERGY COST\/NET BLDG AREA\:\s*(.*)$/)
      read(report_text)
    end
  end

  def output_text
    puts @esd_report_text
  end

end

