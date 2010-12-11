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
                RangeCheckValue(value);
            }

            get{ return this.value; } 
        }
        public IDFDatabase idf;

        public bool RangeCheckValue()
        {
            return RangeCheckValue(this.Value);
        }

        public bool RangeCheckValue(string value)
        {
            
            this.HasError = false;
            //Check type if not null and if it is a required field..If it is blank and not required..leave the blank and don't do check.
            if ((value == null || value == "") && !this.Field.IsRequiredField())
            {
                this.value = value;
            }
            else
            {
                switch (this.FieldType())
                {

                    case "integer":

                        int ivalue = Convert.ToInt32(Convert.ToDecimal(value));
                        //Convert String to int. 
                        if (value == null || value == "") { value = "0"; }
                        if (
                                (Field.RangeGreaterThan() == null || (Field.RangeGreaterThan() != null && ivalue > Convert.ToInt32(Field.RangeGreaterThan()))) &&
                                (Field.RangeLessThan() == null || (Field.RangeLessThan() != null && ivalue < Convert.ToInt32(Field.RangeLessThan()))) &&
                                (Field.RangeMaximum() == null || (Field.RangeMaximum() != null && ivalue <= Convert.ToInt32(Field.RangeMaximum()))) &&
                                (Field.RangeMinimum() == null || (Field.RangeMinimum() != null && ivalue >= Convert.ToInt32(Field.RangeMinimum()))) 
                            
                            )
                        {
                            this.value = ivalue.ToString();
                            this.HasError = false;
                        }
                        else
                        {
                            this.HasError = true;
                        }
                        break;

                    case "real":
                        if (value == null || value == "") { value = "0"; }
                        //check if autocalc or autosize apparently there are bugs in E+ that allow Autosiza and AutoCalc to be use interchangably. 
                        if ((value.ToLower() == "autosize" || value.ToLower() == "autocalculate") && (this.Field.IsAutoSizable()
                             || this.Field.IsAutoCalculable()) )
                        { this.HasError = false; }
                        else
                        {

                            //Convert String to double. 
                            if (
                                (Field.RangeGreaterThan() == null || (Field.RangeGreaterThan() != null && Convert.ToDouble(value) > Convert.ToDouble(Field.RangeGreaterThan() ) )) &&
                                (Field.RangeLessThan() ==null || ( Field.RangeLessThan() != null && Convert.ToDouble(value) < Convert.ToDouble(Field.RangeLessThan())) ) &&
                                (Field.RangeMaximum() == null || (Field.RangeMaximum() != null && Convert.ToDouble(value) <= Convert.ToDouble(Field.RangeMaximum())) ) &&
                                (Field.RangeMinimum() == null || (Field.RangeMinimum() != null && Convert.ToDouble(value) >= Convert.ToDouble(Field.RangeMinimum()))) 
                                )
                            {
                                this.HasError = false;
                            }
                            else
                            {
                                this.HasError = true;
                            }
                        }
                        this.value = value;
                        break;
                    case "choice":
                        List<string> keys = this.Choices().ToList<string>();
                        if (keys.Contains(value, StringComparer.OrdinalIgnoreCase))
                        { this.HasError = false; }
                        else
                        { this.HasError = true; }
                        this.value = value;
                        break;

                    case "object-list":

                        string listname = this.Field.ObjectList();
                        //AutoRDD are output variables that are identified only from the result file. They are not present in the IDD so they are ignored for now. 
                        if (!listname.Contains("autoRDD"))
                        {
                            List<string> names = this.Choices().ToList<string>();
                            if (value != null && names.Contains(value, StringComparer.OrdinalIgnoreCase))
                            {
                                this.HasError = false;
                            }
                            else
                            {
                                this.HasError = true;
                            }
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
        public IList<string> Choices()
        {
            IList<string> names = new List<string>();
            switch (this.FieldType())
            {   
                case "object-list":
                    names = idf.GetObjectListCommandNames(this.Field.ObjectList());
                    break;
                case "choice":
                    names = Field.Keys().ToList<string>();
                    break;
                default:
                    break;
            }
            return names;
        }
            
            
            
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
