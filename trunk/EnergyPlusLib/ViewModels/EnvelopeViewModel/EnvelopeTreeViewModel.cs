using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using EnergyPlusLib.DataModel.IDF;
using EnergyPlusLib.DataAccess;
using EnergyPlusLib.ViewModels;

namespace EnergyPlusLib.ViewModel
{
    /*
    Treestructure Wish list. 

    Building
    ->Simulation Parameters
    ->Compliance Objects
    ->Location and Climate
    ->Schedules
    ->Surface Construction Elements
    Zones

    */

    public class EnvelopeTreeViewModel
    {
        readonly ReadOnlyCollection<ZoneEnvelopeViewModel> _zones;

        public EnvelopeTreeViewModel(IDFDatabase idf)
        {
            IList<IDFCommand> zones = idf.FindAllCommandsFromObjectName("Zone");


            _zones = new ReadOnlyCollection<ZoneEnvelopeViewModel>(
                (from zone in zones
                 select new ZoneEnvelopeViewModel(zone,idf))
                .ToList());


        }

        public ReadOnlyCollection<ZoneEnvelopeViewModel> Zones
        {
            get { return _zones; }
        }
    }
    

        public class ZoneEnvelopeViewModel : TreeViewItemViewModel
        {
            readonly IDFCommand _zoneEnvelope;
            IDFDatabase _idf;

            public ZoneEnvelopeViewModel(IDFCommand zone, IDFDatabase idf)
                : base(null, true)
            {
                _idf = idf;
                _zoneEnvelope = zone;
            }

            public string ZoneName
            {
                get { return _zoneEnvelope.Object.Name +"-"+ _zoneEnvelope.GetName(); }
            }

            protected override void LoadChildren()
            {
                List<IDFCommand> Surfaces = _idf.FindAllCommands("BuildingSurface:Detailed", "Zone Name", _zoneEnvelope.GetName() ).ToList<IDFCommand>();
                foreach (IDFCommand command in Surfaces)
                {
                    base.Children.Add(new SurfaceEnvelopeViewModel(command, this, _idf));
                }

            }
        }

        public class SurfaceEnvelopeViewModel : TreeViewItemViewModel
        {
            readonly IDFCommand _surface;
            IDFDatabase _idf;

            public SurfaceEnvelopeViewModel(IDFCommand surface, ZoneEnvelopeViewModel parentZone, IDFDatabase idf)
                : base(parentZone, false)
            {
                _idf = idf;
                _surface = surface;
            }

            public string SurfaceName
            {
                get { return _surface.Object.Name+"-"+_surface.GetName(); }
            }


        }


}
