using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Windows;
using Oracle.ManagedDataAccess.Client;


namespace BarcodePrinter
{
    public class Repository {

        string MDLConnect = "Data Source=aincrad.hq.wsuniar.org;Initial Catalog=MDL;User ID=MDLService;Password=Wn3$7|}6(2,<5_w1:0&B;Connect Timeout=5;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        string oracleConnect = "Data Source=(DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = ociwsudb1.oraclevcn.mdl.wichita.edu)(PORT = 1521)) (CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = odbc.wsu)));  User ID=sccodbc; Password=eoSRJJ36";

        /// <summary>
        /// Take in the badge information and get the user's information from that.
        /// </summary>
        /// <param name="badge">Badge value</param>
        /// <returns>A 'User' class that contains the information of the user (ID, name, username, role)</returns>
        
        public ConnectionState CheckMDLConnection() {
            using (var connection = new SqlConnection(MDLConnect)) {
                return connection.State;
            }
        }
        public ConnectionState CheckOracleConnection()
        {
            using (var connection = new OracleConnection(oracleConnect))
            {
                return connection.State;
            }
        }

        public List<Client> SelectAllClients()
        {
            using (OracleConnection oraConn = new OracleConnection(oracleConnect))
            {
                using (Oracle.ManagedDataAccess.Client.OracleCommand cmd = new OracleCommand("SELECT * FROM V_GC_CLINICS", oraConn))
                {
                    oraConn.Open();

                    OracleDataReader reader = cmd.ExecuteReader();

                    List<Client> clients = new List<Client>();
                    while (reader.Read())
                    {
                        //TODO: update this with the correct fields
                        clients.Add(new Client(
                            reader.GetString(reader.GetOrdinal("Name")),
                            reader.GetString(reader.GetOrdinal("Number"))));
                    }

                    return clients;
                }
            }
        }

        public List<Customer> SelectAllCustomers()//Client client)
        {
            using (SqlConnection sqlConn = new SqlConnection(MDLConnect))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Customer", sqlConn))
                {
                    //var p = new SqlParameter("Client_Num", SqlDbType.NVarChar, 128);
                    //p.Value = client.Number;
                    
                    //cmd.Parameters.Add(p);

                    sqlConn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    List<Customer> customers = new List<Customer>();
                    while (reader.Read())
                    {
                        //TODO: update this with the correct fields
                        customers.Add(new Customer(
                            reader.GetString(reader.GetOrdinal("Customer")),
                            reader.GetInt32(reader.GetOrdinal("CustomerID")),
                            reader.GetString(reader.GetOrdinal("SubCustomer"))
                            ));
                    }

                    return customers;
                }
            }
        }
    }
}