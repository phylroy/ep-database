using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using System.Data.SQLite;

namespace EnergyPlusLib
{
    public class SqliteDB
    {
        #region some sqlite commands *************
        // You can add you own commands as reminder ;)
        // "CREATE TABLE mytable (id INTEGER PRIMARY KEY, data TEXT, num double, timeEnter DATE)";
        // "SELECT * FROM mytable limit 2";
        // "INSERT INTO mytable VALUES(1, 'This is sample data', 3, NULL)";
        // "INSERT INTO mytable (data) VALUES ('myvalue')";
        // "DELETE FROM mytable WHERE id = 1";
        // "UPDATE  mytable SET data = "my description" WHERE id = 1";
        #endregion

        #region Variables Definitions *********************

        public string sqliteVersion = "3";
        public string dbPath;

        private SQLiteConnection sql_con;
        private SQLiteCommand sql_cmd;
        private SQLiteDataAdapter sql_da;
        private SQLiteTransaction sql_trans;

        private DataSet _DS = new DataSet();
        private DataTable _DT = new DataTable();

        private string message = "";
        private bool success = false;

        #endregion

        #region Constructors



        public SqliteDB(string path)
        {
            this.dbPath = path;
            this.Connect();
        }

        #endregion

        #region Messages *********************

        /// <summary>
        /// return Exception Message
        /// </summary>
        public string msg
        {
            get { return message; }
        }

        /// <summary>
        /// Return succefull query
        /// </summary>
        public bool successQuery
        {
            get { return success; }
        }

        #endregion

        #region sqlite queries *************

        /// <summary>
        /// Starts sqlite connection
        /// </summary>
        private void Connect()
        {
            try
            {
                if (File.Exists(dbPath))
                {
                    sql_con = new SQLiteConnection("Data Source=" + dbPath + ";Version=" + sqliteVersion + ";New=False;Compress=True;");
                }
                else
                {
                    SQLiteConnection.CreateFile(dbPath);
                    sql_con = new SQLiteConnection("Data Source=" + dbPath + ";Version=" + sqliteVersion + ";New=False;Compress=True;");
                }
            }
            catch (Exception e) { message = "Error Connection:\r\n" + e.ToString(); }
            finally { success = true; }
        }

        /// <summary>
        /// Insert big Data with transaction
        /// </summary>
        /// <param name="command">sqlite command seperated with ;</param>
        public void insertData(string command)
        {
            try
            {
                string[] cmd = command.Split(';');
                Connect();
                sql_con.Open();

                sql_cmd = new SQLiteCommand();

                // Start a local transaction 
                sql_trans = sql_con.BeginTransaction(IsolationLevel.Serializable);
                // Assign transaction object for a pending local transaction 
                sql_cmd.Transaction = sql_trans;

                for (int i = 0; i < cmd.Length; i++)
                {
                    if (cmd[i] != "")
                    {
                        sql_cmd.CommandText = cmd[i].Trim();
                        sql_cmd.ExecuteNonQuery();
                    }
                }
                sql_trans.Commit();
            }
            catch (Exception e)
            {
                sql_trans.Rollback();
                message = "Error insertData: Non of the data has been inserted to Database\r\n\r\n" + e.ToString();
                success = false;
                sql_con.Close();
            }
            finally
            {
                sql_con.Close();
                message = "Success insertData";
                success = true;
            }
        }


        /// <summary>
        /// returns DataTable from Db
        /// </summary>
        /// <param name="command">SQL Command</param>
        /// <returns></returns>
        public DataTable LoadData(string command)
        {
            DataSet dataset = new DataSet();
            try
            {
                Connect();
                sql_con.Open();

                sql_cmd = sql_con.CreateCommand();
                sql_da = new SQLiteDataAdapter(command, sql_con);

                
                dataset.Reset();

                sql_da.Fill(dataset);

                

                sql_con.Close();
            }
            catch (Exception e) { message = "Error:\r\n" + e.ToString(); success = false; }
            finally { message = "Success LoadData"; success = true; }

            return dataset.Tables[0];
        }


        /// <summary>
        /// returns DataTable from Db
        /// </summary>
        /// <param name="command">SQL Command</param>
        /// <returns></returns>
        public DataSet LoadDataSet()
        {
            try
            {
                Connect();
                sql_con.Open();

                sql_cmd = sql_con.CreateCommand();
                sql_da = new SQLiteDataAdapter("SELECT *", sql_con);

                _DS.Reset();

                sql_da.Fill(_DS);

                sql_con.Close();
            }
            catch (Exception e) { message = "Error:\r\n" + e.ToString(); success = false; }
            finally { message = "Success LoadData"; success = true; }

            return _DS;
        }


        /// <summary>
        /// returns DataTable from Db
        /// </summary>
        /// <param name="command">SQL Command</param>
        /// <returns></returns>
        public DataTable LoadDataTable(string TableName)
        {
            return this.LoadData("SELECT * FROM " + TableName);
        }


        /// <summary>
        /// returns DataTable from Db
        /// </summary>
        /// <param name="command">SQL Command</param>
        /// <returns></returns>
        public DataTable ListDataTables()
        {
             DataTable table = this.LoadData("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
             return table;
        }



        /// <summary>
        /// CREATE TABLE, INSERT, UPDATE, DELETE
        /// </summary>
        /// <param name="command">sql command line</param>
        public void SingleQuery(string command)
        {
            try
            {
                Connect();
                sql_con.Open();

                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = command;

                sql_cmd.ExecuteNonQuery();
                sql_con.Close();
            }
            catch (Exception e) { message = "Error SingleQuery:\r\n" + e.ToString(); success = false; }
            finally { message = "Success SingleQuery"; success = true; }
        }

        /// <summary>
        /// return number of Rows
        /// </summary>
        /// <param name="cmd">Sqlite command</param>
        /// <returns>Integer</returns>
        public int numRows(string cmd)
        {
            int res = 0;
            try
            {
                _DT = LoadData(cmd);
                res = _DT.Rows.Count;
            }
            catch (Exception e) { message = "Error numRows:\r\n" + e.ToString(); success = false; }
            return res;
        }

        #endregion

        #region Methods *************

        /// <summary>
        /// Read txtFile and send to insertData function
        /// </summary>
        /// <param name="filePath">txt file path</param>
        public void insertFromTxt(string filePath)
        {
            string str;

            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        str = sr.ReadToEnd();
                        sr.Close();

                        insertData(str);
                    }
                }
                catch (Exception e) { message = "Error insertFromTxt:\r\n" + e.ToString(); success = false; }
            }
        }

        /// <summary>
        /// encode qouts (", ') to htmlEntitites before saving
        /// </summary>
        /// <param name="str">string to encode</param>
        /// <returns></returns>
        public string htmlQouts(string str)
        {
            str = str.Replace("'", "&acute;");
            str = str.Replace("\"", "&quot;");
            return str;
        }

        /// <summary>
        /// decode qouts (", ') from htmlEntitites before saving
        /// </summary>
        /// <param name="str">string to decode</param>
        /// <returns></returns>
        public string htmlDeQouts(string str)
        {
            str = str.Replace("&acute;", "'");
            str = str.Replace("&quot;", "\"");
            return str;
        }
        #endregion
    }
}
