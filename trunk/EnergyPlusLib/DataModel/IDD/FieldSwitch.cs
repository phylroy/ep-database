using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnergyPlusLib.DataModel.IDD
{
    public class FieldSwitch
    {
        #region Properties
        public string Name { get; set; }
        public string Value { get; set; }
        public Field Field { get; set; }
        #endregion
        #region Constructors
        public FieldSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
        public FieldSwitch() { }
        #endregion
    }
}
