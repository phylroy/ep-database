using System;
using System.Collections.Generic;
using System.Linq;
using EnergyPlusLib.DataAccess;
using EnergyPlusLib.DataModel.IDD;

namespace EnergyPlusLib.DataModel.IDF
{
    public class IDFArgument
    {
        #region Properties

        public IDDField Field;
        public IDFDatabase idf;
        private string value;
        public bool HasError { private set; get; }

        public string Value
        {
            set
            {
                this.value = value;
                this.RangeCheckValue(value);
                this.UpdateObjectListReferences();
            }

            get { return this.value; }
        }

        private void UpdateObjectListReferences()
        {
            foreach (string reference in this.Field.References())
            {
                //using TrygetValue because it is faster. 
                var ListOfArguments = new List<IDFArgument>();
                if (false == this.idf.IDFObjectLists.TryGetValue(reference, out ListOfArguments))
                {
                    ListOfArguments = this.idf.IDFObjectLists[reference] = new List<IDFArgument>();
                }

                if (false == ListOfArguments.Contains(this))
                {
                    ListOfArguments.Add(this);
                }
            }
        }

        public bool RangeCheckValue()
        {
            return this.RangeCheckValue(this.Value);
        }

        public bool RangeCheckValue(string valuein)
        {
            this.HasError = false;
            //Check type if not null and if it is a required field..If it is blank and not required..leave the blank and don't do check.
            if ((String.IsNullOrEmpty(valuein)) && !this.Field.IsRequiredField())
            {
                this.value = valuein;
            }
            else
            {
                switch (this.FieldType())
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
                                this.value = ivalue.ToString();
                                this.HasError = false;
                            }
                            else
                            {
                                this.HasError = true;
                            }
                        }
                        catch (Exception)
                        {
                            //string value is not convertable to a double. 
                            this.HasError = true;
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
                                }
                            }

                            catch (Exception)
                            {
                                //string value is not convertable to a double. 
                                this.HasError = true;
                            }
                        }
                        this.value = valuein;
                        break;
                    case "choice":
                        List<string> keys = this.Choices().ToList();
                        if (keys.Contains(valuein, StringComparer.OrdinalIgnoreCase))
                        {
                            this.HasError = false;
                        }
                        else
                        {
                            this.HasError = true;
                        }
                        this.value = valuein;
                        break;

                    case "object-list":

                        string listname = this.Field.ObjectList();
                        //AutoRDD are output variables that are identified only from the result file. They are not present in the IDD so they are ignored for now. 
                        if (!listname.Contains("autoRDD"))
                        {
                            List<string> names = this.Choices().ToList();
                            if (valuein != null && names.Contains(valuein, StringComparer.OrdinalIgnoreCase))
                            {
                                this.HasError = false;
                            }
                            else
                            {
                                this.HasError = true;
                            }
                        }
                        this.value = valuein;
                        break;

                    case "alpha":
                    default:
                        this.value = valuein;
                        break;
                }
            }

            return this.HasError;
        }


        //To-Do
        public string FieldName()
        {
            return this.Field.Name();
        }

        public string FieldType()
        {
            return this.Field.Type();
        }

        public IList<string> Choices()
        {
            IList<string> names = new List<string>();
            switch (this.FieldType())
            {
                case "object-list":
                    names = this.idf.GetFieldListArgumentNames(this.Field.ObjectList());
                    break;
                case "choice":
                    names = this.Field.Keys().ToList();
                    break;
                default:
                    break;
            }
            return names;
        }


        //public double MaxValue();
        //public double MinValue();
        public bool IsRequired()
        {
            return this.Field.IsRequiredField();
        }

        public IList<string> Notes()
        {
            return this.Field.Notes();
        }

        //public bool HasError();

        #endregion

        #region Constructor

        public IDFArgument(IDFDatabase idf, IDDField field, string Value)
        {
            this.idf = idf;
            this.Field = field;
            this.Value = Value;
        }

        #endregion
    }
}