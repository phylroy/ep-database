using System;
using System.Collections.Generic;
using System.Linq;
using EnergyPlusLib.EnergyPlus;

namespace EnergyPlusLib
{
    public class IDFCommand
    {
        #region Properties


        public String ObjectName 
        {
            get
            {
                return Object.Name;
            }
        }
        public String CommandName
        {
            get
            {
                return this.GetName();
            }
        }


        public readonly IDDObject Object ;
        private IDFDatabase IDF;
        public IList<IDFArgument> RegularArguments { get; private set; }
        public IList<IList<IDFArgument>> ExtensibleSetArguments { get; private set; }
        public IList<IDFArgument> ComplianceArguments { get; private set; }
        public IList<IDFArgument> FlattenedArgumentList
        {
            get
            {
                IList<IDFArgument> ExtArgs = (from argumentsets in this.ExtensibleSetArguments
                                              from argument in argumentsets
                                              select argument).ToList();

                IList<IDFArgument> FullArgs = new List<IDFArgument>();
                foreach (IDFArgument item in this.RegularArguments) FullArgs.Add(item);
                foreach (IDFArgument item in ExtArgs) FullArgs.Add(item);
                return FullArgs;
            }
        }
        public bool HasError { get; set; }
        public bool IsMuted { get; set; }

        #endregion

        #region Constructors

        public IDFCommand(IDFDatabase idf, IDDObject Object)
            : this(idf)
        {
            this.Object = Object;
            this.IDF = idf;
            int fieldcounter = 0;
            string value = null;
            foreach (IDDField field in Object.RegularFields)
            {
                //If required Field and within minimum field set default. Otherwise keep string empty. 
                if (field.IsRequiredField() && fieldcounter < Convert.ToInt32(Object.MinimumFields()))
                {
                    value = field.Default();
                }
                else
                {
                    value = null;
                }

                var arg = new IDFArgument(this.IDF, field, value);
                this.RegularArguments.Add(arg);
                fieldcounter++;
            }

            var ExtensibleSet = new List<IDFArgument>();
            foreach (IDDField field in this.Object.ExtensibleFields)
            {
                //If required Field and within minimum field set default. Otherwise keep string empty. 
                if (field.IsRequiredField() && fieldcounter < Convert.ToInt32(Object.MinimumFields()))
                {
                    value = field.Default();
                }
                else
                {
                    value = null;
                }
                var arg = new IDFArgument(this.IDF, field, field.Default());
                ExtensibleSet.Add(arg);
                fieldcounter++;
            }

            this.ExtensibleSetArguments.Add(ExtensibleSet);
        }

        private IDFCommand(IDFDatabase idf)
        {
            this.RegularArguments = new List<IDFArgument>();
            this.ExtensibleSetArguments = new List<IList<IDFArgument>>();
            this.IsMuted = false;
        }

        #endregion


        public bool CheckValues()
        {
            bool Error = false;
            foreach (IDFArgument arg in this.FlattenedArgumentList)
            {
                if (arg.Validate())
                {
                    Error = true;
                }
            }
            return Error;
        }

        private String ToIDFString(bool _noComments, bool _terse)
        {
            bool noComments = _noComments;
            bool Terse = _terse;

            string Prefix = "";
            if (this.IsMuted) Prefix = "!";
            string comment = "";
            string newline = "\r\n";
            string commandTerminator = ";";
            string fieldTerminator = ",";
            string format = "    {0,-50} {1,-50} ";
            if (true == Terse)
            {
                format = "{0}{1}";
                newline = "";
            }

            

            
            IList<IDFArgument> FullList;
            FullList = this.FlattenedArgumentList;

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
                FullList = (from arg in this.FlattenedArgumentList
                            where arg.Value != null
                            select arg).ToList();
            }

            int argumentcounter = 0;
            //print out Object Command Name. 
            string tempstring1 = Prefix + this.Object.Name + fieldTerminator + newline;
            

            



            foreach (IDFArgument argument in FullList)
            {
                //check to see if rest of arguments are either blank or null. 
                bool restareblank = true;
                for (int counter = argumentcounter + 1; counter < FullList.Count(); counter++)
                {
                    if (!(FullList[counter].Value == null) || FullList[counter].FieldIsRequired)
                    {
                        restareblank = false;
                        break;

                    }
                }


                String units = argument.FieldUnits;

                //create unit string.
                if (units != null)
                {
                    units = " {" + units + "}";
                }


                //Standard comment format.
                if (false == noComments)
                {
                    comment = "!-" + argument.FieldName + units;
                    if (argument.Validate())
                    {
                        argument.Validate();
                        comment += " -RANGE ERROR- ";
                    }
                }
                else
                {
                    comment = ""; 
                }

                if ( (FullList.Last() == argument) 
                    || (this.Object.MinimumFields() == null && restareblank )
                    || (this.Object.MinimumFields() != null && restareblank
                        && argumentcounter >= (Convert.ToInt32(this.Object.MinimumFields()) - 1)) )
                {
                    tempstring1 += String.Format(Prefix + format + newline + newline, argument.Value + commandTerminator, comment);
                    break;
                }
                else
                {
                    tempstring1 += String.Format(Prefix + format + newline, argument.Value + fieldTerminator, comment);                           
                }
                argumentcounter++;
            }
            return tempstring1;
        }

        public String ToIDFStringNoComments()
        {
            return ToIDFString(true, false);
        }

        public String ToIDFString()
        {
            return ToIDFString(false, false);
        }

        public String ToIDFStringTerse()
        {

            return ToIDFString(true, true);
        }

        public string GetName()
        {
            if ( this.DoesArgumentExist(@"Name"))
        {
            return this.GetArgument(@"Name").Value;
        }
        else return null;
        }

        public IDFArgument GetArgument(String fieldname)
        {
            IDFArgument Argument = (from argument in this.FlattenedArgumentList
                                    where argument.FieldName.ToLower() == fieldname.Trim().ToLower()
                                    select argument).FirstOrDefault();
            return Argument;
        }

        public bool DoesArgumentExist(String fieldname)
        {
            IDFArgument Argument = (from argument in this.FlattenedArgumentList
                                    where argument.FieldName.ToLower() == fieldname.Trim().ToLower()
                                    select argument).FirstOrDefault();
            return Argument == null ? false : true;
        }

        public void SetArgument(String fieldname, String value)
        {
            IList<IDFArgument> Arguments = (from argument in this.FlattenedArgumentList
                                            where argument.FieldName.ToLower() == fieldname.Trim().ToLower()
                                            select argument).ToList();


            foreach (IDFArgument s in Arguments)
            {
                s.Value = value.Trim();
            }
            ;
        }

        public void SetArgumentbyDataName(String DataName, String value)
        {
            IList<IDFArgument> Arguments = (from argument in this.FlattenedArgumentList
                                            where argument.FieldDataName== DataName
                                            select argument).ToList();


            foreach (IDFArgument s in Arguments)
            {
                s.Value = value.Trim();
            }
            ;
        }

        public void FindParentCommandsOfType(string IDDObjectName)
        {
        }

        public void FindChildrenCommandsOfType(string IDDObjectName)
        {
        }


    }
}