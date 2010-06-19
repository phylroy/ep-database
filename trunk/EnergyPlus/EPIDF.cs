using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Reflection;


namespace EnergyPlus
{
    class EPIDF
    {
        public EPIDD epidd;


        public DataTable ConvertToDataTable<T>(IEnumerable<T> varlist)
        {
            DataTable dtReturn = new DataTable();

            // column names   
            PropertyInfo[] oProps = null;

            if (varlist == null) return dtReturn;

            foreach (T rec in varlist)
            {
                // Use reflection to get property names, to create table, Only first time, others will follow   
                if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();

                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null ? DBNull.Value : pi.GetValue
                    (rec, null);
                }

                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }  

        public EPIDF()
        {








            epidd = EPIDD.GetInstance();
            DataSet idfDataSet = new DataSet();
            //iterate through object database
            DataTable objects = epidd.IDD.Tables["objects"];
            DataTable objects_switches = epidd.IDD.Tables["object_switches"];
            DataTable objects_fields = epidd.IDD.Tables["fields"];
            DataTable objects_fields_switches = epidd.IDD.Tables["field_switches"];

            var query =
            from object1 in objects.AsEnumerable()
            join object_switch in objects_switches.AsEnumerable()
            on object1.Field<Int32>("object_id") equals
            object_switch.Field<Int32>("object_id")
            where object_switch.Field<String>("object_switch") == @"\unique-object"
            select new 
            {
            ObjectName =
            object1.Field<String>("object_name"),
            ObjectID =
            object1.Field<Int32>("object_ID"),
            ObjectSwitch =
            object_switch.Field<String>("object_switch"),
            ObjectSwitchValue =
            object_switch.Field<String>("object_switch_value")
            };

            DataTable orderTable = ConvertToDataTable(query);  

            var fieldquery =
            from object1 in objects.AsEnumerable()
            join object_field in objects_fields.AsEnumerable()
            on object1.Field<Int32>("object_id") equals
            object_field.Field<Int32>("object_id")
            select new
            {



                object_name =
                object1.Field<String>("object_name"),
                object_id =
                object1.Field<Int32>("object_id"),
                field_name =
                object_field.Field<String>("field_name"),
                field_id =
                object_field.Field<Int32>("field_id")
            };
            DataTable orderTable2 = ConvertToDataTable(fieldquery);

            var fieldquery2 =
            from object1 in orderTable2.AsEnumerable()
            join object_field_switch in objects_fields_switches.AsEnumerable()
            on object1.Field<Int32>("field_id") equals
            object_field_switch.Field<Int32>("field_id")
            select new
            {
                object_name =
                object1.Field<String>("object_name"),
                object_id =
                object1.Field<Int32>("object_id"),
                field_name =
                object1.Field<String>("field_name"),
                field_id =
                object1.Field<Int32>("field_id"),
                switch_id = 
                object_field_switch.Field<Int32>("switch_id"),
                field_switch =
                object_field_switch.Field<String>("field_switch"),
                field_switch_value =
                object_field_switch.Field<String>("field_switch_value"),
            };

            DataTable orderTable3 = ConvertToDataTable(fieldquery2);


            //

            foreach (DataRow objectRow in objects.Rows)
            {
                DataRow[] objectSwitchRows = objectRow.GetChildRows("objectSwitches");
                DataTable newTable = idfDataSet.Tables.Add(objectRow["object_name"].ToString());
                DataRow[] fields = objectRow.GetChildRows("ObjectFields");
                foreach (DataRow fieldRow in fields)
                {
                    DataRow[] fieldSwitchRows = fieldRow.GetChildRows("FieldSwitches");
                    DataColumn column = new DataColumn(fieldRow["field_name"].ToString());
                    //Console.WriteLine(objectRow["object_name"].ToString() + fieldRow["field_name"].ToString());

                    // find dataType
                    string dataType = "alpha";
                    switch (dataType)
                    {
                        case "choice":
                        case "object-list":
                        case "alpha":
                            column.DataType = System.Type.GetType("System.String");
                            break;
                        case "integer":
                            column.DataType = System.Type.GetType("System.Int32");
                            break;
                        case "real":
                            column.DataType = System.Type.GetType("System.Double");
                            break;
                        default:
                            break;
                    }
                    newTable.Columns.Add(column);
                }
            }
        }

    }
}
