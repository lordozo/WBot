using System;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace Bot.Extensions.MySql
{
    public class DBConnection
    {
        private DBConnection()
        {
        }

        public string databaseName;
        public string login;

        public string password;
        private MySqlConnection connection = null;
        public MySqlConnection Connection
        {
            get { return connection; }
        }

        private static DBConnection _instance = null;
        public static DBConnection Instance()
        {
            if (_instance == null)
                _instance = new DBConnection();
           return _instance;
        }

        public bool IsConnect()
        {
            if (Connection == null)
            {
                string connstring = string.Format("Server=localhost; database={0}; UID={1}; password={2}; SslMode=none", databaseName, login, password);
                connection = new MySqlConnection(connstring);
                
                connection.Open();
                Console.WriteLine("COnnection is online.. " + connection.Ping().ToString()); 
            }

            return true;
        }

        public void Close()
        {
            connection.Close();
        }        
    }
}