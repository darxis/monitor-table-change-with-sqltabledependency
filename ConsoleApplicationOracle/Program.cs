﻿using System;
using Oracle.ManagedDataAccess.Client;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.Mappers;
using TableDependency.OracleClient;

namespace ConsoleApplicationOracle
{
    class Program
    {
        private const string ConnectionString = "Data Source = " +
                                                "(DESCRIPTION = " +
                                                " (ADDRESS_LIST = " +
                                                " (ADDRESS = (PROTOCOL = TCP)" +
                                                " (HOST = 127.0.0.1) " +
                                                " (PORT = 1521) " +
                                                " )" +
                                                " )" +
                                                " (CONNECT_DATA = " +
                                                " (SERVICE_NAME = XE)" +
                                                " )" +
                                                ");" +
                                                "User Id=SYSTEM;" +
                                                "password=Casadolcecasa1;";

        private const string TableName = "AAAITEM";

        internal static void DropAndCreateTable(string connectionString, string tableName)
        {
            using (var connection = new OracleConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"BEGIN EXECUTE IMMEDIATE 'DROP TABLE {tableName.ToUpper()}'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; END;";
                    command.ExecuteNonQuery();
                    command.CommandText = $"CREATE TABLE {tableName} (ID INTEGER, RAWCOLUMN RAW(2000), XMLCOLUMN XMLTYPE, NAME VARCHAR2(50), DESCRIPTION VARCHAR2(4000))";
                    command.ExecuteNonQuery();
                }
            }
        }

        static void Main()
        {
            DropAndCreateTable(ConnectionString, TableName);

            var mapper = new ModelToTableMapper<Item>();
            mapper.AddMapping(c => c.XmlColumn, "XMLCOLUMN");

            using (var tableDependency = new OracleTableDependency<Item>(ConnectionString, TableName, mapper))
            {
                tableDependency.OnChanged += Changed;
                tableDependency.OnError += tableDependency_OnError;

                tableDependency.Start();
                Console.WriteLine(@"Waiting for receiving notifications: change some records in the table...");
                Console.WriteLine(@"Press a key to exit");
                Console.ReadKey();
            }
        }

        static void tableDependency_OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Error.Message);
            Console.WriteLine(e.Error.StackTrace);
        }

        static void Changed(object sender, RecordChangedEventArgs<Item> e)
        {
            Console.WriteLine(Environment.NewLine);

            if (e.ChangeType != ChangeType.None)
            {
                var changedEntity = e.Entity;
                Console.WriteLine(@"At " + DateTime.Now.ToString("HH:mm:ss") + @" DML operation: " + e.ChangeType);
                Console.WriteLine(@"ID: " + changedEntity.Id);
                Console.WriteLine(@"Name: " + changedEntity.Name);
                Console.WriteLine(@"XmlColumn: " + changedEntity.XmlColumn);
                Console.WriteLine(@"RAW Column: " + GetString(changedEntity.RAWCOLUMN));
            }
            else
            {
                Console.WriteLine("NO CHANGE");
            }
        }

        static string GetString(byte[] bytes)
        {
            if (bytes == null) return null;
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}
