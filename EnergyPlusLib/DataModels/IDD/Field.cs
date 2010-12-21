using System;
using System.Collections.Generic;
using System.Linq;

namespace EnergyPlusLib.DataModel.IDD
{
    public class IDDField
    {
        #region Properties

        public IList<IDDObject> ObjectListTypeChoices;
        public String DataName { get; private set; }
        public int Order { get; set; }
        public IDDObject Object { get; set; }
        public IList<IDDFieldSwitch> Switches { get; private set; }

        #endregion

        #region Constructors

        public IDDField(string DataName, int Order, IDDObject Object)
            : this()
        {
            this.DataName = DataName;
            this.Order = Order;
            this.Object = Object;
        }

        public IDDField()
        {
            this.Switches = new List<IDDFieldSwitch>();
            this.ObjectListTypeChoices = new List<IDDObject>();
        }

        #endregion

        #region General Methods

        public virtual bool UpdateRelationships()
        {
            //Add all the Field types that could be used to populate this field if needed. 
            string ObjectList = this.ObjectList();
            if (ObjectList != null)
            {
                //ObjectListTypeChoices = IDD.GetObjectListReferences(ObjectList);
            }


            return true;
        }

        public IDDFieldSwitch FindSwitch(string switch_name)
        {
            IEnumerable<IDDFieldSwitch> result = from Switch in this.Switches
                                                 where Switch.Name == switch_name
                                                 select Switch;
            return result.FirstOrDefault();
        }

        public string FindSwitchValue(string switch_name)
        {
            IEnumerable<string> result = from Switch in this.Switches
                                         where Switch.Name == switch_name
                                         select Switch.Value;
            return result.FirstOrDefault();
        }

        public virtual void AddSwitch(IDDFieldSwitch Switch)
        {
            this.Switches.Add(Switch);
        }

        #endregion

        #region Energyplus Field Switch Methods.

        public IList<string> FindSwitchValues(string switch_name)
        {
            IEnumerable<string> result = from Switch in this.Switches
                                         where Switch.Name == switch_name
                                         select Switch.Value;
            return result.ToList();
        }

        public bool IsSwitchPresent(string switch_name)
        {
            bool value = false;
            IEnumerable<string> result = from Switch in this.Switches
                                         where Switch.Name == switch_name
                                         select Switch.Value;
            if (result.Count() > 0) value = true;
            return value;
        }

        public string Name()
        {
            return this.FindSwitchValue(@"\field");
        }

        public IList<string> Notes()
        {
            return this.FindSwitchValues(@"\note");
        }

        public bool IsRequiredField()
        {
            return this.IsSwitchPresent(@"\required-field");
        }

        public string Units()
        {
            return this.FindSwitchValue(@"\units");
        }

        public string IPUnits()
        {
            return this.FindSwitchValue(@"\ip-units");
        }

        public string UnitsBasedOnField()
        {
            return this.FindSwitchValue(@"\unitsBasedOnField");
        }

        public string RangeMinimum()
        {
            return (this.FindSwitchValue(@"\minimum"));
        }

        public string RangeMaximum()
        {
            return (this.FindSwitchValue(@"\maximum"));
        }

        public string RangeGreaterThan()
        {
            return (this.FindSwitchValue(@"\minimum>"));
        }

        public string RangeLessThan()
        {
            return (this.FindSwitchValue(@"\maximum<"));
        }

        public string Depreciated()
        {
            return this.FindSwitchValue(@"\deprecated");
        }

        public string Default()
        {
            return this.FindSwitchValue(@"\default");
        }

        public bool IsAutoSizable()
        {
            return this.IsSwitchPresent(@"\autosizable");
        }

        public bool IsAutoCalculable()
        {
            return this.IsSwitchPresent(@"\autocalculatable");
        }

        public string Type()
        {
            return this.FindSwitchValue(@"\type");
        }

        public IList<string> Keys()
        {
            return this.FindSwitchValues(@"\key");
        }

        public string ObjectList()
        {
            return this.FindSwitchValue(@"\object-list");
        }


        public IList<string> References()
        {
            return this.FindSwitchValues(@"\reference");
        }

        #endregion
    }
}