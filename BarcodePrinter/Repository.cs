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

        //string MDLConnect = "Data Source=aincrad.hq.wsuniar.org;Initial Catalog=MDL;User ID=MDLService;Password=Wn3$7|}6(2,<5_w1:0&B;Connect Timeout=5;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        string MDLConnect = "Data Source=dbsvc-325.ad.wichita.edu;User ID=MDLwebService;Password=tZgGDZ4{8Q{k9w;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
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
                        clients.Add(new Client(
                            reader.GetString(reader.GetOrdinal("GC_CL_NAME")),
                            reader.GetString(reader.GetOrdinal("GC_CL_CODE"))
                            ));
                    }

                    return clients;
                }
            }
        }

        public List<Customer> SelectAllCustomers()
        {
            using (SqlConnection sqlConn = new SqlConnection(MDLConnect))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Customer", sqlConn))
                {
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

        public bool TryAddCustomer(Client c)
        {
            using (SqlConnection sqlConn = new SqlConnection(MDLConnect))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.CreateCustomer", sqlConn))
                {
                    //todo: update these fields and params
                    var p = new SqlParameter("Client_Num", SqlDbType.NVarChar, 128);
                    p.Value = c.Name;
                    cmd.Parameters.Add(p);

                    p = new SqlParameter("Client_Code", SqlDbType.Int);
                    p.Value = c.Code;
                    cmd.Parameters.Add(p);

                    p = new SqlParameter("Success", SqlDbType.Bit);
                    p.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p);

                    return (bool)cmd.Parameters["Success"].Value;
                }
            }
        }

        public string GetBarcode(Client c)
        {
            //using (OracleConnection oracleConnection = new OracleConnection(oracleConnection))
            using (SqlConnection sqlConnection = new SqlConnection(MDLConnect))
            {
                //using (OracleCommand = new OracleCommand("SELECT Barcode FROM V_GET_BARCODES, oracleConnection))
                using (SqlCommand cmd = new SqlCommand("dbo.GetLastBarcode"))//todo: update the command and parameter names
                {
                    var p = new SqlParameter("Client_Num", SqlDbType.NVarChar, 128);
                    p.Value = c.Code;
                    cmd.Parameters.Add(p);

                    p = new SqlParameter("Client_Name", SqlDbType.NVarChar, 128);
                    p.Value = c.Name;
                    cmd.Parameters.Add(p);

                    p = new SqlParameter("Barcode", SqlDbType.NVarChar, 128);
                    p.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(p);

                    cmd.ExecuteNonQuery();

                    return (string)cmd.Parameters["Barcode"].Value;
                }
            }
        }
    }
}