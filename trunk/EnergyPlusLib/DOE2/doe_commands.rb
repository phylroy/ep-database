# To change this template, choose Tools | Templates
# and open the template in the editor.

module DOE2
  class DOECommands < Array
    def initialize
      super()
    end

    def find_all_commands (sCOMMAND)
      array = DOECommands.new()

      self.each do |command|
        if (command.commandName == sCOMMAND)
          array.push(command)
        end
      end
      return array
    end

    # This method will find all Commands given the command name string.
    # Example
    # def find_all_Command("Default Construction")  will return an array of all
    # the commands with "Default Construction" as the u-type used in the building.
    def find_command_with_utype (utype)
      array = DOECommands.new()
      self.each do |command|
        if (command.utype == utype)
          array.push(command)
        end
      end
      return array
    end


    # Same as find_all_commands except you can use regular expressions.
    def find_all_with_utype_regex(sCOMMAND)
      array = DOECommands.new()
      search =/#{sCOMMAND}/
      self.each do |command|
        if (command.utype.match(search) )
          array.push(command)
        end

      end
      return array
    end

    # Find a matching keyword value pair in from an array of commands.
    # Example:
    # find_keyword_value(building.commands, "TYPE", "CONDITIONED")  will return
    # all the commands that have the
    # TYPE = CONDITIONED"
    # Keyword pair.
    def find_keyword_value(arrayOfCommands, keyword, value)
      returnarray = DOECommands.new()
      arrayOfCommands.each do |command|
        if ( command.keywordPairs[keyword] == value )
          returnarray.push(command)
        end
      end
      return returnarray
    end


    def shortprint()
  self.each do |command|
    puts command.utype + "=" + command.commandName

  end
end

def detailprint()
    self.each do |command|
    puts command.output
  end
end

  end
end
