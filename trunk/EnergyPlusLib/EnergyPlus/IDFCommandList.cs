using System.Collections.Generic;
using System.Linq;

namespace EnergyPlusLib.EnergyPlus
{
    public class IDFCommands : List<IDFCommand>
    {
        private readonly IDDDataBase idd = IDDDataBase.GetInstance();

        #region Methods to Search / Find Commands and Arguments.

        public IList<IDFCommand> FindCommandsOfObjectType(IDDObject objecttype)
        {
            List<IDFCommand> objects = (
                                           from command in this
                                           where command.Object == objecttype
                                           select command).ToList();
            return objects;
        }

        /// <summary>
        /// Returns all Commands which match the IDDObject name type in the IDFCommands list. 
        /// </summary>
        /// <param name="ObjectName">The string iddobject name.</param>
        /// <returns>List of IDFCommands whos obmatching the Object name. </returns>
        public IList<IDFCommand> WhereObjectNameEquals(string ObjectName)
        {
            IDDObject iddobject = this.idd.GetObject(ObjectName);
            return this.FindCommandsOfObjectType(iddobject);
        }

        public IList<IDFCommand> FindCommands(string ObjectName, string FieldName, string FieldValue)
        {
            List<IDFCommand> objects = (from surface in this.WhereObjectNameEquals(ObjectName)
                                        where
                                            surface.DoesArgumentExist(FieldName) &&
                                            surface.GetArgument(FieldName).Value == FieldValue
                                        select surface).ToList();
            return objects;
        }

        public IList<IDFCommand> WhereArgumentEquals(string FieldName, string FieldValue)
        {
            List<IDFCommand> objects = (from command in this
                                        where
                                            command.DoesArgumentExist(FieldName) &&
                                            command.GetArgument(FieldName).Value == FieldValue
                                        select command).ToList();
            return objects;
        }

        public IList<IDFCommand> WherePartOfGroup(string groupin)
        {
            groupin = groupin.ToUpper();
            List<IDFCommand> commands = (from command in this
                                         where command.Object.Group.ToUpper() == groupin
                                         select command).ToList();
            return commands;
        }

        #endregion
    }
}