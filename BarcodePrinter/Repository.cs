using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;

namespace BarcodePrinter
{
    public class Repository {

        //string MDLConnect = "Data Source=aincrad.hq.wsuniar.org;Initial Catalog=MDL;User ID=MDLService;Password=Wn3$7|}6(2,<5_w1:0&B;Connect Timeout=5;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        string MDLConnect = "Data Source=dbsvc-325.ad.wichita.edu;User ID=MDLwebService;Password=tZgGDZ4{8Q{k9w;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        string oracleConnect = "Data Source=(DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = ociwsudb1.oraclevcn.mdl.wichita.edu)(PORT = 1521)) (CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = odbc.wsu)));  User ID=sccodbc; Password=eoSRJJ36";

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

        //public List<Customer> SelectAllCustomers()
        //{
        //    using (SqlConnection sqlConn = new SqlConnection(MDLConnect))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("SELECT * FROM Customer", sqlConn))
        //        {
        //            sqlConn.Open();

        //            SqlDataReader reader = cmd.ExecuteReader();

        //            List<Customer> customers = new List<Customer>();
        //            while (reader.Read())
        //            {
        //                //TODO: update this with the correct fields
        //                customers.Add(new Customer(
        //                    reader.GetInt32(reader.GetOrdinal("CustomerID")),
        //                    reader.GetString(reader.GetOrdinal("Customer")),
        //                    reader.GetString(reader.GetOrdinal("SubCustomer"))
        //                    ));
        //            }

        //            return customers;
        //        }
        //    }
        //}
    }
}