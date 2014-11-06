using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Finisar.SQLite;

namespace CloudX.utils
{
    internal class SQLiteUtils
    {
        private static SQLiteCommand sqlCommand;
        private static SQLiteConnection sqlConnection;

        private static void InitConnection()
        {
            Console.WriteLine(Application.UserAppDataPath);

            string userAppDataPath = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8,
                Encoding.Default.GetBytes(Application.UserAppDataPath)));

            Console.WriteLine("UserAppDataPath : " + userAppDataPath);

            string dataPath = userAppDataPath + "\\" + "data";

            if (Directory.Exists(dataPath) && File.Exists(dataPath + "\\database.db"))
            {
                sqlConnection =
                    new SQLiteConnection(
                        "Data Source=" + dataPath + "\\database.db" +
                        ";Version=3;New=False;Compress=True;UTF8Encoding=True;");
                sqlConnection.Open();

                sqlCommand = sqlConnection.CreateCommand();
            }
            else
            {
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);

                    ClearFileUnderPath(dataPath);
                }
                sqlConnection =
                    new SQLiteConnection(
                        "Data Source=" + dataPath + "\\database.db" +
                        ";Version=3;New=True;Compress=True;UTF8Encoding=True;");
                sqlConnection.Open();

                ClearFileUnderPath(dataPath);

                sqlCommand = sqlConnection.CreateCommand();

                sqlCommand.CommandText = "create table movie(fileLocation nvarchar primary key)";
                sqlCommand.ExecuteNonQuery();
                sqlCommand.CommandText = "create table music(fileLocation nvarchar primary key)";
                sqlCommand.ExecuteNonQuery();
                sqlCommand.CommandText = "create table file(fileLocation nvarchar primary key)";
                sqlCommand.ExecuteNonQuery();
            }

            //string sql = "select count(*) as c from sqlite_master where type ='table' and name ='movie'";

            //sqlCommand.CommandText =
            //    "create table movie(fileLocation nvarchar primary key)";
            //sqlCommand.ExecuteNonQuery();
            //sqlCommand.CommandText = "create table music(fileLocation nvarchar primary key)";
            //sqlCommand.ExecuteNonQuery();
            //sqlCommand.CommandText = "create table file(fileLocation nvarchar primary key)";
            //sqlCommand.ExecuteNonQuery();
        }

        private static void ClearFileUnderPath(string dataPath)
        {
            var dir = new DirectoryInfo(dataPath);


            //文件
            foreach (FileInfo fChild in dir.GetFiles("*")) //这里可选文件筛选方式
            {
                if (fChild.Attributes != FileAttributes.Normal)
                    fChild.Attributes = FileAttributes.Normal; //避免文件属性为Readonly或Hidden时无权限问题
                // fChild.Delete();
            }


            //文件夹
            foreach (DirectoryInfo dChild in dir.GetDirectories("*")) //这里可选文件夹筛选方式
            {
                if (dChild.Attributes != FileAttributes.Normal)
                    dChild.Attributes = FileAttributes.Normal; //避免文件夹属性为Readonly或Hidden时无权限问题
                ClearFileUnderPath(dChild.FullName); //递归
                // dChild.Delete();
            }
        }

        public static DataTable LoadData(string table)
        {
            InitConnection();

            string CommandText = "select * from " + table;
            DataAdapter dataAdapter = new SQLiteDataAdapter(CommandText, sqlConnection);

            var dataSet = new DataSet();

            dataSet.Reset();
            dataAdapter.Fill(dataSet);

            DisposeConnection();

            return dataSet.Tables[0];
        }

        public static int Query(string table, string fileName)
        {
            InitConnection();
            sqlCommand.CommandText = "select * from " + table + " where fileLocation=" + fileName;
            int resultNumber = sqlCommand.ExecuteNonQuery();
            DisposeConnection();
            return resultNumber;
        }

        /// <summary>
        ///     返回数据库中的所有数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static SQLiteDataReader QueryTable(string tableName)
        {
            InitConnection();
            sqlCommand.CommandText = "select * from " + tableName;
            SQLiteDataReader result = sqlCommand.ExecuteReader();
            DisposeConnection();

            return result;
        }

        public static void Insert(string tableName, string fileLocation)
        {
            string txtSQLQuery = "insert into " + tableName + " values ('" + fileLocation + "')";
            ExecuteNonQuery(txtSQLQuery);
        }

        /// <summary>
        ///     todo 不确定sql语句是否正确
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fileName"></param>
        public static void Delete(string tableName, string fileName)
        {
            string sql = "delete from " + tableName + " where fileLocation=\'" + fileName + "\'";
            int result = ExecuteNonQuery(sql);
            Console.WriteLine("{0}  {1}", sql, result);
        }

        public static void DeleteAll(string tableName)
        {
            ExecuteNonQuery("delete from " + tableName);
        }

        private static int ExecuteNonQuery(string commandText)
        {
            InitConnection();
            sqlCommand.CommandText = commandText;
            int result = sqlCommand.ExecuteNonQuery();
            DisposeConnection();
            return result;
        }


        private static void DisposeConnection()
        {
            sqlConnection.Close();
            sqlConnection = null;
        }
    }
}