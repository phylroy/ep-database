namespace EnergyPlusLib.DataModel.IDD
{
    public class IDDObjectSwitch
    {
        #region Properties

        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
        public virtual IDDObject Object { get; set; }

        #endregion

        #region Constructors

        public IDDObjectSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public IDDObjectSwitch()
        {
        }

        #endregion
    }
}