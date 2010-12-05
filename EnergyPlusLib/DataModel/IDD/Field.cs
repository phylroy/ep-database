using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnergyPlusLib.DataModel.IDD
{
    public class Field
    {
        #region Properties
        public String DataName { get; private set; }
        public int Order { get; set; }
        public IDDObject Object { get; set; }
        public IList<FieldSwitch> Switches { get; private set; }
        public IList<IDDObject> ObjectListTypeChoices = null;
        #endregion
        #region Constructors
        public Field(string DataName, int Order, IDDObject Object)
            : this()
        {

            this.DataName = DataName;
            this.Order = Order;
            this.Object = Object;

        }

        public Field()
        {
            Switches = new List<FieldSwitch>();
            ObjectListTypeChoices = new List<IDDObject>();
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
        public FieldSwitch FindSwitch(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch;
            return result.FirstOrDefault();
        }
        public string FindSwitchValue(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            return result.FirstOrDefault();
        }
        public virtual void AddSwitch(FieldSwitch Switch)
        {
            this.Switches.Add(Switch);
        }
        #endregion
        #region Energyplus Field Switch Methods.


        public IList<string> FindSwitchValues(string switch_name)
        {
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            return result.ToList<String>();
        }

        public bool IsSwitchPresent(string switch_name)
        {
            bool value = false;
            var result = from Switch in Switches
                         where Switch.Name == switch_name
                         select Switch.Value;
            if (result.Count() > 0) value = true;
            return value;
        }

        public string Name()
        { return FindSwitchValue(@"\field"); }

        public IList<string> Notes()
        { return FindSwitchValues(@"\note"); }

        public bool IsRequiredField()
        { return IsSwitchPresent(@"\required-field"); }

        public string Units()
        { return FindSwitchValue(@"\units"); }

        public string IPUnits()
        { return FindSwitchValue(@"\ip-units"); }

        public string UnitsBasedOnField()
        { return FindSwitchValue(@"\unitsBasedOnField"); }

        public string RangeMinimum()
        { return FindSwitchValue(@"\minimum"); }

        public string RangeMaximum()
        { return FindSwitchValue(@"\maximum"); }

        public string RangeGreaterThan()
        { return FindSwitchValue(@"\minimum>"); }

        public string RangeLessThan()
        { return FindSwitchValue(@"\maximum<"); }

        public string Depreciated()
        { return FindSwitchValue(@"\deprecated"); }

        public string Default()
        { return FindSwitchValue(@"\default"); }

        public bool IsAutoSizable()
        { return IsSwitchPresent(@"\autosizable"); }

        public bool IsAutoCalculable()
        { return IsSwitchPresent(@"\autocalculatable"); }

        public string Type()
        { return FindSwitchValue(@"\type"); }

        public IList<string> Keys()
        { return FindSwitchValues(@"\key"); }

        public string ObjectList()
        { return FindSwitchValue(@"\object-list"); }


        public IList<string> References()
        { return FindSwitchValues(@"\reference"); }
        #endregion
    }
}
