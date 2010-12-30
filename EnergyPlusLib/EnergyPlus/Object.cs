using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnergyPlusLib.EnergyPlus
{
    public class IDDObject
    {
        #region Properties

        public virtual string Name { get; set; }
        public virtual IList<IDDObjectSwitch> Switches { get; set; }
        public virtual IList<IDDField> RegularFields { get; set; }
        public virtual int NumberOfRegularFields { get; set; }
        public virtual IList<IDDField> ExtensibleFields { get; set; }
        public virtual int NumberOfExtensibleFields { get; set; }
        public virtual string Group { get; set; }

        #endregion

        #region Constructors

        public IDDObject()
        {
            this.Switches = new List<IDDObjectSwitch>();
            this.RegularFields = new List<IDDField>();
            this.ExtensibleFields = new List<IDDField>();
        }

        public IDDObject(string Name, string Group)
            : this()
        {
            this.Name = Name;
            this.Group = Group;
        }

        #endregion

        # region General Methods.

        public virtual void AddSwitch(IDDObjectSwitch switch_pass)
        {
            this.Switches.Add(switch_pass);
        }

        public virtual void AddField(IDDField Field_pass)
        {
            this.RegularFields.Add(Field_pass);
        }

        public virtual void SortFields()
        {
            this.NumberOfRegularFields = this.RegularFields.Count();
            if (this.NumberOfExtensibleFields > 0)
            {
                this.NumberOfRegularFields = this.RegularFields.Count() - this.NumberOfExtensibleFields;
                if (this.NumberOfRegularFields == 0)
                {
                    for (int i = 0; i < this.RegularFields.Count(); i++)
                    {
                        IDDFieldSwitch switch1 = this.RegularFields[i].FindSwitch(@"\field");
                        switch1.Value = Regex.Replace(switch1.Value, @" (1)", @" ");
                        this.ExtensibleFields.Add(this.RegularFields[i]);
                    }
                    this.RegularFields.Clear();
                }
                else
                {
                    for (int iField = this.NumberOfRegularFields;
                         iField <= (this.NumberOfRegularFields + this.NumberOfExtensibleFields) - 1;
                         iField++)
                    {
                        IDDFieldSwitch switch1 = this.RegularFields[iField].FindSwitch(@"\field");
                        switch1.Value = Regex.Replace(switch1.Value, @" (1)", @" ");

                        this.ExtensibleFields.Add(this.RegularFields[iField]);
                        //this.RegularFields.Remove(this.RegularFields[iField]);
                    }
                    //now remove the fields we added to the extensible array from the oringinal Regular field array. 
                    foreach (IDDField field in this.ExtensibleFields)
                    {
                        this.RegularFields.Remove(field);
                    }
                }
            }
        }

        public string FindSwitchValue(string switch_name)
        {
            IEnumerable<string> result = from Switch in this.Switches
                                         where Switch.Name == switch_name
                                         select Switch.Value;
            return result.FirstOrDefault();
        }

        public IList<string> FindSwitchValues(string switch_name)
        {
            IEnumerable<string> result = from Switch in this.Switches
                                         where Switch.Name == switch_name
                                         select Switch.Value;
            return result.ToList();
        }

        public IList<IDDField> FlattenedFieldList()
        {
            IList<IDDField> FullFields = new List<IDDField>();


            foreach (IDDField item in this.RegularFields) FullFields.Add(item);
            foreach (IDDField item in this.ExtensibleFields) FullFields.Add(item);
            return FullFields;
        }

        #endregion

        #region IDD Switch Methods

        public bool IsSwitchPresent(string switch_name)
        {
            bool value = false;
            IEnumerable<string> result = from Switch in this.Switches
                                         where Switch.Name == switch_name
                                         select Switch.Value;
            if (result.Count() > 0) value = true;
            return value;
        }

        public IList<string> Memos()
        {
            return this.FindSwitchValues(@"\memo");
        }

        public bool IsUniqueObject()
        {
            return this.IsSwitchPresent(@"\unique-object");
        }

        public bool IsRequiredObject()
        {
            return this.IsSwitchPresent(@"\required-object");
        }

        public string MinimumFields()
        {
            return this.FindSwitchValue(@"\min-fields");
        }

        public bool IsObsolete()
        {
            return this.IsSwitchPresent(@"\required-object");
        }

        public string ExtensibleNumber()
        {
            return this.FindSwitchValue(@"\extensible");
        }

        public string Format()
        {
            return this.FindSwitchValue(@"\format");
        }

        #endregion
    }
}