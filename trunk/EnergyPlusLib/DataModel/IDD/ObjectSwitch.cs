using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnergyPlusLib.DataModel.IDD
{
    public class ObjectSwitch
    {
        #region Properties
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual IDDObject Object { get; set; }
        #endregion
        #region Constructors
        public ObjectSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
        public ObjectSwitch()
        {
        }
        #endregion
    }
}
