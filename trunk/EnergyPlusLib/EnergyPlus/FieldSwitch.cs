namespace EnergyPlusLib.EnergyPlus
{
    public class IDDFieldSwitch
    {
        #region Properties

        public string Name { get; set; }
        public string Value { get; set; }
        public IDDField Field { get; set; }

        #endregion

        #region Constructors

        public IDDFieldSwitch(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public IDDFieldSwitch()
        {
        }

        #endregion
    }
}