using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;
using EnergyPlusLib;
using EnergyPlusLib.DataModel.IDF;
namespace EPTreeTest.EPFullTreeView
{
    class GroupViewModel
    {

        /// <summary>
        /// The ViewModel for the LoadOnDemand demo.  This simply
        /// exposes a read-only collection of regions.
        /// </summary>

            readonly ReadOnlyCollection<CommandViewModel> _commands;

            public GroupViewModel(Command[] commands)
            {
                _commands = new ReadOnlyCollection<CommandViewModel>(
                    (from command in commands
                     select new CommandViewModel(command))
                    .ToList());
            }

            public ReadOnlyCollection<CommandViewModel> Regions
            {
                get { return _commands; }
            }
        


    }
}
