using System;
using System.Collections.Generic;
using System.Linq;

namespace EnergyPlusLib.EnergyPlus
{
    public class IDFArgument
    {
        #region Constructor
        public IDFArgument(IDFDatabase idf, IDDField field, string Value)
        {
            this.IDF = idf;
            this.Field = field;
            this.Value = Value;
        }
        #endregion

        #region Fields
        
        public String _value;
        #endregion
        
        #region Properties
        public IDDField Field  { private set; get; }
        public String RangeErrorMessage { private set; get; }

        public IDFDatabase IDF { private set; get; }
        public String FieldName
        {
            get { return this.Field.Name(); }
        }
        public String FieldDataName
        {
            get { return this.Field.DataName; }
        }
        public String FieldUnits 
        {
            get { return this.Field.Units();}
        }
        public IList<String> FieldNotes
        {
            get { return this.Field.Notes(); }
        }
        public Boolean FieldIsRequired
        {
            get { return this.Field.IsRequiredField(); }
        }
        public IList<String> Choices
        {
            get
            {
                IList<string> names = new List<string>();
                switch (this.FieldType)
                {
                    case "object-list":
                        names = this.IDF.GetFieldListArgumentNames(this.Field.ObjectList());
                        break;
                    case "choice":
                        names = this.Field.Keys().ToList();
                        break;
                    default:
                        break;
                }
                return names;
            }
        }
        public Boolean HasValidationError { private set; get; }
        public Boolean HasSimulationError { private set; get; }
        public Boolean HasError { private set; get; }
        public String Value
        {
            set
            {
                if (value != null)
                {
                    this._value = value.Trim();
                    this.HasValidationError = this.Validate();
                    this.UpdateObjectListReferences();
                }
            }

            get { return this._value; }
        }
        #endregion

        #region Methods
        private void UpdateObjectListReferences()
        {
            foreach (string reference in this.Field.References())
            {
                //using TrygetValue because it is faster. 
                var ListOfArguments = new List<IDFArgument>();
                if (false == this.IDF.IDFObjectLists.TryGetValue(reference, out ListOfArguments))
                {
                    ListOfArguments = this.IDF.IDFObjectLists[reference] = new List<IDFArgument>();
                }

                if (false == ListOfArguments.Contains(this))
                {
                    ListOfArguments.Add(this);
                }
            }
        }
        public Boolean Validate()
        {
            return this.Validate(this.Value);
        }
        public Boolean Validate(String valuein)
        {
            this.HasError = false;
            //Check type if not null and if it is a required field..If it is blank and not required..leave the blank and don't do check.
            if ((String.IsNullOrEmpty(valuein)) && !this.Field.IsRequiredField())
            {
                this.Value = valuein;
            }
            else
            {
                switch (this.FieldType)
                {
                    case "integer":


                        //Convert String to int. 

                        try
                        {
                            int ivalue = Convert.ToInt32(valuein);

                            if (
                                (this.Field.RangeGreaterThan() == null ||
                                 (this.Field.RangeGreaterThan() != null &&
                                  ivalue > Convert.ToInt32(this.Field.RangeGreaterThan()))) &&
                                (this.Field.RangeLessThan() == null ||
                                 (this.Field.RangeLessThan() != null &&
                                  ivalue < Convert.ToInt32(this.Field.RangeLessThan()))) &&
                                (this.Field.RangeMaximum() == null ||
                                 (this.Field.RangeMaximum() != null &&
                                  ivalue <= Convert.ToInt32(this.Field.RangeMaximum()))) &&
                                (this.Field.RangeMinimum() == null ||
                                 (this.Field.RangeMinimum() != null &&
                                  ivalue >= Convert.ToInt32(this.Field.RangeMinimum())))
                                )
                            {
                                this.Value = ivalue.ToString();
                                this.HasError = false;
                            }
                            else
                            {
                                this.HasError = true;
                                this.RangeErrorMessage = "Value is not within accepted range.";
                            }
                        }
                        catch (Exception)
                        {
                            //string value is not convertable to a double. 
                            this.HasError = true;
                            this.RangeErrorMessage = "This is not a numeric value.";
                        }
                        break;

                    case "real":

                        bool isAutoable = (valuein != null && this.Field.IsAutoSizable() &&
                                           valuein.ToLower() == "autosize")
                                          ||
                                          (valuein != null && this.Field.IsAutoCalculable() &&
                                           valuein.ToLower() == "autocalculate");

                        //check if autocalc or autosize apparently there are bugs in E+ that allow Autosiza and AutoCalc to be use interchangably. 
                        if (isAutoable)
                        {
                            this.HasError = false;
                        }
                        else
                        {
                            try
                            {
                                double dvalue = Convert.ToDouble(valuein);


                                //Convert String to double. 
                                if (
                                    (this.Field.RangeGreaterThan() == null ||
                                     (this.Field.RangeGreaterThan() != null &&
                                      Convert.ToDouble(valuein) > Convert.ToDouble(this.Field.RangeGreaterThan()))) &&
                                    (this.Field.RangeLessThan() == null ||
                                     (this.Field.RangeLessThan() != null &&
                                      Convert.ToDouble(valuein) < Convert.ToDouble(this.Field.RangeLessThan()))) &&
                                    (this.Field.RangeMaximum() == null ||
                                     (this.Field.RangeMaximum() != null &&
                                      Convert.ToDouble(valuein) <= Convert.ToDouble(this.Field.RangeMaximum()))) &&
                                    (this.Field.RangeMinimum() == null ||
                                     (this.Field.RangeMinimum() != null &&
                                      Convert.ToDouble(valuein) >= Convert.ToDouble(this.Field.RangeMinimum())))
                                    )
                                {
                                    this.HasError = false;
                                }
                                else
                                {
                                    this.HasError = true;
                                    this.RangeErrorMessage = "Value is not within accepted range.";
                                }
                            }

                            catch (Exception)
                            {
                                //string value is not convertable to a double. 
                                this.HasError = true;
                            }
                        }
                        this.Value = valuein;
                        break;
                    case "choice":
                        List<string> keys = this.Choices.ToList();
                        if (keys.Contains(valuein, StringComparer.OrdinalIgnoreCase))
                        {
                            this.HasError = false;
                        }
                        else
                        {
                            this.HasError = true;
                            this.RangeErrorMessage = "Value is not one of the choices ";
                            foreach (string key in keys)
                            {
                                this.RangeErrorMessage += key + ", ";
                            }
                        }
                        this.Value = valuein;
                        break;

                    case "object-list":

                        string listname = this.Field.ObjectList();
                        //AutoRDD are output variables that are identified only from the result file. They are not present in the IDD so they are ignored for now. 
                        if (!listname.Contains("autoRDD"))
                        {
                            List<string> names = this.Choices.ToList();
                            if (valuein != null && names.Contains(valuein, StringComparer.OrdinalIgnoreCase))
                            {
                                this.HasError = false;
                            }
                            else
                            {
                                this.HasError = true;
                                this.RangeErrorMessage = "Value is not one of the choices ";
                                foreach (string key in names)
                                {
                                    this.RangeErrorMessage += key + ", ";
                                }
                            }
                        }
                        this.Value = valuein;
                        break;

                    case "alpha":
                    default:
                        this.Value = valuein;
                        break;
                }
            }

            //check to see if this is actually a parameter value. 
            if (this.HasError)
            {
                if (this._value.Contains("=$"))
                {
                    this.HasError = false;
                }
            }

            return this.HasError;
        }
        public String FieldType
        {
            get{ return this.Field.Type();} 
        }
        #endregion
    }
}