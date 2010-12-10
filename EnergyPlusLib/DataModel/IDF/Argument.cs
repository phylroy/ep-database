using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnergyPlusLib.DataModel.IDD;
using EnergyPlusLib.DataAccess;

namespace EnergyPlusLib.DataModel.IDF
{
    public class Argument  
    {
        #region Properties
        private string value;
        public Field Field;
        public bool HasError { private set; get; }
        public string Value 
        {
            set
            {
                this.value = value;
                CheckValue(value);
            }

            get{ return this.value; } 
        }
        public IDFDatabase idf;
        public bool CheckValue(string value)
        {
            
            this.HasError = false;
            //Check type if not null
            if (value != null)
            {
                switch (this.FieldType())
                {

                    case "integer":
                        //Convert String to int. 

                        if (
                            (Field.RangeGreaterThan() != null && Convert.ToInt32(value) > Convert.ToInt32(Field.RangeGreaterThan())) &&
                            (Field.RangeLessThan() != null && Convert.ToInt32(value) < Convert.ToInt32(Field.RangeLessThan())) &&
                            (Field.RangeMaximum() != null && Convert.ToInt32(value) >= Convert.ToInt32(Field.RangeMaximum())) &&
                            (Field.RangeMinimum() != null && Convert.ToInt32(value) <= Convert.ToInt32(Field.RangeMinimum())))
                        {
                            this.value = value;
                            this.HasError = false;
                        }
                        else
                        {
                            this.HasError = true;
                        }
                        break;

                    case "real":
                        //check if autocalc or autosize
                        if (value.ToLower() == "autosize" && this.Field.IsAutoSizable()
                            || value.ToLower() == "autocalculate" && this.Field.IsAutoCalculable())
                        { this.HasError = false; }
                        else
                        {
                            //Convert String to double. 

                            if (
                                (Field.RangeGreaterThan() != null && Convert.ToDouble(value) > Convert.ToDouble(Field.RangeGreaterThan())) &&
                                (Field.RangeLessThan() != null && Convert.ToDouble(value) < Convert.ToDouble(Field.RangeLessThan())) &&
                                (Field.RangeMaximum() != null && Convert.ToDouble(value) >= Convert.ToDouble(Field.RangeMaximum())) &&
                                (Field.RangeMinimum() != null && Convert.ToDouble(value) <= Convert.ToDouble(Field.RangeMinimum())))
                            {
                                this.HasError = true;
                            }
                            else
                            {
                                this.HasError = false;
                            }
                        }
                        this.value = value;
                        break;
                    case "choice":
                        List<string> keys = Field.Keys().ToList<string>();
                        if (keys.Contains(value, StringComparer.OrdinalIgnoreCase))
                        { this.HasError = false; }
                        else
                        { this.HasError = true; }
                        this.value = value;
                        break;

                    case "object-list":
                        idf.UpdateAllObjectLists();
                        List<string> names = idf.GetObjectListCommandNames(this.Field.ObjectList());
                        if (value != null && names.Contains(value))
                        {
                            this.HasError = false;
                        }
                        else
                        {
                            this.HasError = true;
                        }
                        this.value = value;
                        break;

                    case "alpha":
                    default:
                        this.value = value;
                        break;
                }

            }
            return this.HasError;
        }


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
        public Argument(IDFDatabase idf,  Field field, string Value)
        {
            this.idf = idf;
            this.Field = field;
            this.Value = Value;
        }

        #endregion


    }
}
