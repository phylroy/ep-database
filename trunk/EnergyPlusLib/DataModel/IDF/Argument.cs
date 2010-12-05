using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnergyPlusLib.DataModel.IDD;

namespace EnergyPlusLib.DataModel.IDF
{
    public class Argument  
    {
        #region Properties
        public Field Field;
        public string Value { set; get; }


        //To-Do
        public string FieldName()
        { return Field.Name(); }
        public string FieldType()
        { return Field.Type(); }
        //public IList<string> Choices();
        //public double MaxValue();
        //public double MinValue();
        public bool IsRequired()
        { return Field.IsRequiredField(); }
        public IList<string> Notes()
        { return Field.Notes(); }
        //public bool HasError();



        #endregion
        #region Constructor
        public Argument(Field field, string Value)
        {
            this.Field = field;
            this.Value = Value;
        }

        #endregion


    }
}
