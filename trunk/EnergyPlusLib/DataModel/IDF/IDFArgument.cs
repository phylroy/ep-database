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
                RangeCheckValue(value);
                UpdateObjectListReferences();
            }

            get { return value; }
        }

        private void UpdateObjectListReferences()
        {
            foreach (string reference in Field.References())
            {
                //using TrygetValue because it is faster. 
                var ListOfArguments = new List<IDFArgument>();
                if (false == idf.IDFObjectLists.TryGetValue(reference, out ListOfArguments))
                {
                    ListOfArguments = idf.IDFObjectLists[reference] = new List<IDFArgument>();
                }

                if (false == ListOfArguments.Contains(this))
                {
                    ListOfArguments.Add(this);
                }
            }
        }

        public bool RangeCheckValue()
        {
            return RangeCheckValue(Value);
        }

        public bool RangeCheckValue(string value)
        {
            HasError = false;
            //Check type if not null and if it is a required field..If it is blank and not required..leave the blank and don't do check.
            if ((value == null || value == "") && !Field.IsRequiredField())
            {
                this.value = value;
            }
            else
            {
                switch (FieldType())
                {
                    case "integer":


                        //Convert String to int. 

                        try
                        {
                            int ivalue = Convert.ToInt32(value);

                            if (
                                (Field.RangeGreaterThan() == null ||
                                 (Field.RangeGreaterThan() != null && ivalue > Convert.ToInt32(Field.RangeGreaterThan()))) &&
                                (Field.RangeLessThan() == null ||
                                 (Field.RangeLessThan() != null && ivalue < Convert.ToInt32(Field.RangeLessThan()))) &&
                                (Field.RangeMaximum() == null ||
                                 (Field.RangeMaximum() != null && ivalue <= Convert.ToInt32(Field.RangeMaximum()))) &&
                                (Field.RangeMinimum() == null ||
                                 (Field.RangeMinimum() != null && ivalue >= Convert.ToInt32(Field.RangeMinimum())))
                                )
                            {
                                this.value = ivalue.ToString();
                                HasError = false;
                            }
                            else
                            {
                                HasError = true;
                            }
                        }
                        catch (Exception)
                        {
                            //string value is not convertable to a double. 
                            HasError = true;
                        }
                        break;

                    case "real":

                        bool isAutoable = (value != null && Field.IsAutoSizable() && value.ToLower() == "autosize")
                                          ||
                                          (value != null && Field.IsAutoCalculable() &&
                                           value.ToLower() == "autocalculate");

                        //check if autocalc or autosize apparently there are bugs in E+ that allow Autosiza and AutoCalc to be use interchangably. 
                        if (isAutoable)
                        {
                            HasError = false;
                        }
                        else
                        {
                            try
                            {
                                double dvalue = Convert.ToDouble(value);


                                //Convert String to double. 
                                if (
                                    (Field.RangeGreaterThan() == null ||
                                     (Field.RangeGreaterThan() != null &&
                                      Convert.ToDouble(value) > Convert.ToDouble(Field.RangeGreaterThan()))) &&
                                    (Field.RangeLessThan() == null ||
                                     (Field.RangeLessThan() != null &&
                                      Convert.ToDouble(value) < Convert.ToDouble(Field.RangeLessThan()))) &&
                                    (Field.RangeMaximum() == null ||
                                     (Field.RangeMaximum() != null &&
                                      Convert.ToDouble(value) <= Convert.ToDouble(Field.RangeMaximum()))) &&
                                    (Field.RangeMinimum() == null ||
                                     (Field.RangeMinimum() != null &&
                                      Convert.ToDouble(value) >= Convert.ToDouble(Field.RangeMinimum())))
                                    )
                                {
                                    HasError = false;
                                }
                                else
                                {
                                    HasError = true;
                                }
                            }

                            catch (Exception)
                            {
                                //string value is not convertable to a double. 
                                HasError = true;
                            }
                        }
                        this.value = value;
                        break;
                    case "choice":
                        List<string> keys = Choices().ToList();
                        if (keys.Contains(value, StringComparer.OrdinalIgnoreCase))
                        {
                            HasError = false;
                        }
                        else
                        {
                            HasError = true;
                        }
                        this.value = value;
                        break;

                    case "object-list":

                        string listname = Field.ObjectList();
                        //AutoRDD are output variables that are identified only from the result file. They are not present in the IDD so they are ignored for now. 
                        if (!listname.Contains("autoRDD"))
                        {
                            List<string> names = Choices().ToList();
                            if (value != null && names.Contains(value, StringComparer.OrdinalIgnoreCase))
                            {
                                HasError = false;
                            }
                            else
                            {
                                HasError = true;
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

            return HasError;
        }


        //To-Do
        public string FieldName()
        {
            return Field.Name();
        }

        public string FieldType()
        {
            return Field.Type();
        }

        public IList<string> Choices()
        {
            IList<string> names = new List<string>();
            switch (FieldType())
            {
                case "object-list":
                    names = idf.GetFieldListArgumentNames(Field.ObjectList());
                    break;
                case "choice":
                    names = Field.Keys().ToList();
                    break;
                default:
                    break;
            }
            return names;
        }

        public bool IsRequired()
        {
            return Field.IsRequiredField();
        }

        public IList<string> Notes()
        {
            return Field.Notes();
        }

        //public bool HasError();

        #endregion

        #region Constructor

        public IDFArgument(IDFDatabase idf, IDDField field, string Value)
        {
            this.idf = idf;
            Field = field;
            this.Value = Value;
        }

        #endregion
    }
}