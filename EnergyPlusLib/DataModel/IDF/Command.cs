using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnergyPlusLib.DataModel.IDD;
using EnergyPlusLib.DataAccess;

namespace EnergyPlusLib.DataModel.IDF
{
    public class IDFCommand 
    {
        #region Properties
        public IDDObject Object;
        public virtual string UserComments { get; set; }
        public IList<IDFArgument> RegularArguments { get; set; }
        public IList<IList<IDFArgument>> ExtensibleSetArguments { get; set; }
        public bool IsMuted;
        public bool HasError;
        public IDFDatabase idf;

        #endregion
        #region Constructors
        public IDFCommand(IDFDatabase idf,IDDObject Object)
            : this(idf)
        {
            this.Object = Object;
            this.idf = idf;
            int fieldcounter = 0;
            string value =null;
            foreach (IDDField field in Object.RegularFields)
            {
                
                //If required Field and within minimum field set default. Otherwise keep string empty. 
                if (field.IsRequiredField() && fieldcounter < Convert.ToInt32(Object.MinimumFields()) )
                { value = field.Default(); }
                else
                { value = null;}

                IDFArgument arg = new IDFArgument(this.idf, field, value);
                this.RegularArguments.Add(arg);
                fieldcounter++;
            }

            List<IDFArgument> ExtensibleSet = new List<IDFArgument>();
            foreach (IDDField field in this.Object.ExtensibleFields)
            {
                //If required Field and within minimum field set default. Otherwise keep string empty. 
                if (field.IsRequiredField() && fieldcounter < Convert.ToInt32(Object.MinimumFields()))
                { value = field.Default(); }
                else
                { value = null; }
                IDFArgument arg = new IDFArgument(this.idf, field, field.Default());
                ExtensibleSet.Add(arg);
                fieldcounter++;
            }

            ExtensibleSetArguments.Add(ExtensibleSet);
        }
        private IDFCommand(IDFDatabase idf)
        {
            this.RegularArguments = new List<IDFArgument>();
            this.ExtensibleSetArguments = new List<IList<IDFArgument>>();
            this.IsMuted = false;
        }


        #endregion
        #region General Methods


        public bool CheckValues()
        {
            this.HasError = false;
            foreach (IDFArgument arg in FlattenedArgumentList())
            {
                if (arg.RangeCheckValue() == true) { this.HasError = true; }
            }
            return this.HasError;
        }


        public IList<IDFArgument> FlattenedArgumentList()
        {
            IList<IDFArgument> ExtArgs = (from argumentsets in this.ExtensibleSetArguments
                                      from argument in argumentsets
                                      select argument).ToList<IDFArgument>();

            IList<IDFArgument> FullArgs = new List<IDFArgument>();
            foreach (IDFArgument item in this.RegularArguments) FullArgs.Add(item);
            foreach (IDFArgument item in ExtArgs) FullArgs.Add(item);
            return FullArgs;
        }

        public String ToIDFString()
        {
            string Prefix = "";
            if (this.IsMuted) Prefix = "!";

            string tempstring1 = Prefix + this.Object.Name + ",\r\n";
            IList<IDFArgument> FullList;
            FullList = FlattenedArgumentList();

            //Workaround for cosntruction types. 
            if (this.Object.Name.ToUpper() == "Construction".ToUpper()
                || this.Object.Name.ToUpper() == "ZoneControl:Thermostat".ToUpper()
                || this.Object.Name.ToUpper() == "FluidProperties:Temperatures".ToUpper()
                || this.Object.Name.ToUpper() == "FluidProperties:Saturated".ToUpper()
                || this.Object.Name.ToUpper() == "FluidProperties:Superheated".ToUpper()
                || this.Object.Name.ToUpper() == "AirLoopHVAC:ControllerList".ToUpper()
                || this.Object.Name.ToUpper() == "AirLoopHVAC:OutdoorAirSystem:EquipmentList".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentList".ToUpper()
                || this.Object.Name.ToUpper() == "CondenserEquipmentList".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperation:CoolingLoad,".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperation:HeatingLoad".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperation:ComponentSetpoint".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperationSchemes".ToUpper()
                || this.Object.Name.ToUpper() == "CondenserEquipmentOperationSchemes".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperation:ComponentSetpoint".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperation:CoolingLoad".ToUpper()
                || this.Object.Name.ToUpper() == "PlantEquipmentOperation:ComponentSetpoint".ToUpper()
                )
            {
                FullList = new List<IDFArgument>();
                FullList = (from arg in FlattenedArgumentList()
                            where arg.Value != null
                            select arg).ToList<IDFArgument>();
            }

            int argumentcounter = 0;
            foreach (IDFArgument argument in FullList)
            {
                
                //check to see if rest of arguments are either blank or null. 
                bool restareblank = true;
                for (int counter = argumentcounter+1; counter < FullList.Count(); counter++)
                {
                    if ((FullList[counter].Value != null && FullList[counter].Value != ""))
                    {
                        restareblank = false;
                        break;

                    }

                }

                


                String units = argument.Field.Units();
                
                //create unit string.
                if (units != null)
                {
                    units = " {" + units + "}";
                }

                string sRangeError = "";
                if (true == argument.HasError)
                {
                    sRangeError = " -RANGE ERROR- ";
                }


                if (this.Object.MinimumFields() != null 
                    && restareblank
                    && argumentcounter == (Convert.ToInt32(this.Object.MinimumFields()) - 1))                
                {
                    tempstring1 += String.Format(Prefix + "    {0,-50} !-{1,-50} \r\n", argument.Value + ";", argument.Field.Name() + units + sRangeError);
                    break;
                }


                if (FullList.Last() == argument)
                {
                    tempstring1 += String.Format(Prefix + "    {0,-50} !-{1,-50} \r\n", argument.Value + ";", argument.Field.Name() + units + sRangeError);
                }
                else
                {
                    tempstring1 += String.Format(Prefix + "    {0,-50} !-{1,-50} \r\n", argument.Value + ",", argument.Field.Name() + units + sRangeError);
                }
                argumentcounter++;
            }
            return tempstring1;
        }

        public String ToIDFStringTerse()
        {
            string Prefix = "";
            if (this.IsMuted) Prefix = "!";

            string tempstring1 = Prefix + this.Object.Name + ",";
            IList<IDFArgument> FullList;
            FullList = FlattenedArgumentList();

            //Workaround for cosntruction types. 
            if (this.Object.Name.ToUpper() == "Construction".ToUpper()
                || this.Object.Name.ToUpper() == "ZoneControl:Thermostat".ToUpper())
            {
                FullList = new List<IDFArgument>();
                FullList = (from arg in FlattenedArgumentList()
                            where arg.Value != null
                            select arg).ToList<IDFArgument>();
            }

            foreach (IDFArgument argument in FullList)
            {

                String units = argument.Field.Units();
                if (units != null)
                {
                    units = " {" + units + "}";
                }
                if (FullList.Last() == argument)
                {
                    tempstring1 += String.Format(Prefix + "{0}\r\n", argument.Value + ";");
                }
                else
                {
                    tempstring1 += String.Format(Prefix + "{0}", argument.Value + ",");
                }

            }
            return tempstring1;
        }

        public string GetName()
        {
            return this.GetArgument(@"Name").Value;
        }


        public IDFArgument GetArgument(String fieldname)
        {
            IDFArgument Argument = (from argument in this.FlattenedArgumentList()
                                 where argument.Field.Name().ToLower() == fieldname.Trim().ToLower()
                                 select argument).FirstOrDefault();
            return Argument;
        }


        public bool DoesArgumentExist(String fieldname)
        {
            IDFArgument Argument = (from argument in this.FlattenedArgumentList()
                                 where argument.Field.Name().ToLower() == fieldname.Trim().ToLower()
                                 select argument).FirstOrDefault();
            return Argument == null ? false : true;
        }

        public void SetArgument(String fieldname, String value)
        {
            IList<IDFArgument> Arguments = (from argument in this.FlattenedArgumentList()
                                        where argument.Field.Name().ToLower() == fieldname.Trim().ToLower()
                                        select argument).ToList<IDFArgument>();



            foreach (IDFArgument s in Arguments) { s.Value = value.Trim(); };
        }
        public void SetArgumentbyDataName(String DataName, String value)
        {
            IList<IDFArgument> Arguments = (from argument in this.FlattenedArgumentList()
                                        where argument.Field.DataName == DataName
                                        select argument).ToList<IDFArgument>();


            foreach (IDFArgument s in Arguments) { s.Value = value.Trim(); };
        }
        #endregion
    }
}
