using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using API_Lib.Models;
using API_Lib.Routes;
using Newtonsoft.Json;
using API_Lib.Models.ProcedureModels.InputModels;
using API_Lib.Models.ProcedureModels.OutputModels;
using System.Net.Http.Headers;

namespace BarcodePrinter
{
    /// <summary>
    /// this class is for accessing the 
    /// MDL database through the MDL webserver API
    /// </summary>
    public class APIAccessor
    {
        #region Base Elements

        private static HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = true };
        private static HttpClient client = new HttpClient(handler, false);
        private static bool _auth = false;
        
        /// <summary>
        /// set the authorization header of the httpclient
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pass"></param>
        public static void SetAuth(string username, string pass)
        {
            string creds = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + pass));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            _auth = true;
        }

        #region baseRequests

        /// <summary>
        /// basic GET request
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="url">url to make the request to </param>
        /// <returns></returns>
        private static async Task<T> Get<T>(string url)
        {
               
            try
            {
                if (!_auth) throw new Exception("Must supply credentials");

                var res = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<T>(res);   
            }
            catch (Exception e)
            {
                if (e.Message.ToUpper().Contains("404"))
                {
                    return default(T);
                }
                else
                {
                    throw e;
                }

            }
        }

        /// <summary>
        /// basic GET request for a particular element
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="url">where to tmake the GET request</param>
        /// <param name="id">primary key of the wanted element</param>
        /// <returns></returns>
        private static async Task<T> Get<T>(string url, int id)
        {
            try
            {
                var res = await client.GetStringAsync(url + id.ToString());
                return JsonConvert.DeserializeObject<T>(res);
            }
            catch (Exception e)
            {
                return default(T);
            }
        }

        /// <summary>
        /// basic POST request
        /// API sometimes uses this as a trigger to perform stored procedures
        /// </summary>
        /// <typeparam name="T">type to be sent in the request</typeparam>
        /// <param name="url">where to make the request</param>
        /// <param name="data">object to be sent in the request body </param>
        /// <returns></returns>
        private static async Task<bool> Post<T>(string url, T data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var res = await client.PostAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }

        /// <summary>
        /// 2 type POST request
        /// for when the sent type and the returned type are different
        /// </summary>
        /// <typeparam name="T">the type to be sent in the request body</typeparam>
        /// <typeparam name="U">the type to be returned by the request</typeparam>
        /// <param name="url">where to send the request</param>
        /// <param name="data">object to be sent</param>
        /// <returns></returns>
        private static async Task<object> Post<T, U>(string url, T data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var res = await client.PostAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            var res2 = await res.Content.ReadAsStringAsync();
            if (typeof(U) == typeof(int))
                return int.Parse(res2);
           
            return JsonConvert.DeserializeObject<U>(res2);
            
        }

        //todo: test Delete requests
        ///// <summary>
        ///// basic DELETE request
        ///// UNTESTED
        ///// </summary>
        ///// <param name="url">url to make the request to</param>
        ///// <returns></returns>
        //private static async Task<bool> Delete(string url)
        //{
        //    var res = await client.DeleteAsync(url);
        //    return res.IsSuccessStatusCode;
        //}

        ///// <summary>
        ///// basic PUT request
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="url">url to make the request</param>
        ///// <param name="data">the object we want to update</param>
        ///// <returns></returns>
        //private static async Task<bool> Put<T>(string url, T data)
        //{
        //    var jsonData = JsonConvert.SerializeObject(data);
        //    var res = await client.PutAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        //    return res.IsSuccessStatusCode;
        //}

        #endregion

        #endregion

        #region Labels

        /// <summary>
        /// making requests to for the labels table
        /// </summary>
        public class LabelAccessor
        {
            private static string url = API_Lib.Routes.AllRoutes.LabelRoutes.FullURL;//url to labels route in API
            
            /// <summary>
            /// get all the labels in the table
            /// </summary>
            /// <returns></returns>
            public static async Task<List<Label>> GetAllLabelsAsync()
            {
                return await Get<List<Label>>(url + AllRoutes.All);
            }

            /// <summary>
            /// get a particular label from the table
            /// </summary>
            /// <param name="id">primary key of label</param>
            /// <returns>Label for that key</returns>
            public static async Task<Label> GetLabelAsync(int id)
            {
                return await Get<Label>(url, id);
            }

            /// <summary>
            /// get the next label that needs to be printed
            /// </summary>
            /// <param name="id">customer id we're printing for</param>
            /// <param name="print">Actively printing the label</param>
            /// <returns></returns>
            public static async Task<string> GetPrintLabelAsync(string id, bool print = false)
            {
                return await Get<string>(url + "print" + id + Queries.Print + print.ToString());
            }

            /// <summary>
            /// calls the CreateLabel stored procedure
            /// </summary>
            /// <param name="input">label information we're adding to the db</param>
            /// <returns>primary keys for the created label and barcode</returns>
            public static async Task<CreateLabelOutput> PostCreateLabel(CreateLabelInput input)
            {
                return await Post<CreateLabelInput, CreateLabelOutput>(url + "create", input) as CreateLabelOutput;
            }
        }

        #endregion

        #region Barcodes

        /// <summary>
        /// making requests for the barcodes table
        /// </summary>
        public static class BarcodeAccessor
        {
            public static string url = AllRoutes.BarcodeRoutes.FullURL;//url to barcodes route in APi

            /// <summary>
            /// runs SelectLastNum stored procedure
            /// </summary>
            /// <param name="custID">customer primary key we want to get the last barcode for</param>
            /// <returns>the last barcode that was added to the barcode table for the given customerid</returns>
            public static async Task<int> GetLastBarcodeAsync(int custID)
            {
                return await Get<int>(url + "lastnum" + custID);
            }

            /// <summary>
            /// Get all the barcodes in the table
            /// </summary>
            /// <returns>List of barcodes</returns>
            public static async Task<List<Barcode>> GetAllBarcodesAsync()
            {
                return await Get<List<Barcode>>(url + AllRoutes.All);
            }

            /// <summary>
            /// get a single barcode
            /// </summary>
            /// <param name="barcodeID">the barcode primary key</param>
            /// <returns>the Barcode row for that key</returns>
            public static async Task<Barcode> GetBarcode(int barcodeID)
            {
                return await Get<Barcode>(url, barcodeID);
            }
        }

        #endregion

        #region Customers

        /// <summary>
        /// making API requests for the customers table
        /// </summary>
        public static class CustomerAccessor
        {
            private static string url = AllRoutes.CustomerRoutes.FullURL;//url to customers route in API

            /// <summary>
            /// get a single customer
            /// </summary>
            /// <param name="custID">primary key of customer we want returned</param>
            /// <returns>Customer for that key</returns>
            public static async Task<Customer> GetCustomerAsync(int custID)
            {
                return await Get<Customer>(url, custID);
            }

            /// <summary>
            /// return all customers in the customers table
            /// </summary>
            /// <returns>List of all customers</returns>
            public static async Task<List<Customer>> GetAllCustomersAsync()
            {
                return await Get<List<Customer>>(url + AllRoutes.All);
            }

            /// <summary>
            /// see if a particular customer exists in the table
            /// </summary>
            /// <param name="id">primary key of the customer</param>
            /// <returns>true if customer exists</returns>
            public static async Task<bool> GetCustomerExistsAsync(string id)
            {
                return await Get<bool>(url + id + AllRoutes.Exists);
            }
        }

        #endregion

        #region Printers

        /// <summary>
        /// for making requests to the printers table through the API
        /// </summary>
        public static class PrintersAccessor
        {
            private static string url = AllRoutes.PrintersRoutes.FullURL;//url to printers routes in the API

            /// <summary>
            /// get a printer from the table
            /// </summary>
            /// <param name="printerId">primary key of printer we want returned</param>
            /// <returns>Printer object</returns>
            public static async Task<Printer>GetPrinterAsync(byte printerId)
            {
                return await Get<Printer>(url, printerId);
            }

            /// <summary>
            /// get all the Printers from the table
            /// </summary>
            /// <returns>List of Printer objects</returns>
            public static async Task<List<Printer>> GetAllPrintersAsync()
            {
                return await Get<List<Printer>>(url + AllRoutes.All);
            }

            /// <summary>
            /// adds a new printer to the Printers table
            /// </summary>            
            /// <param name="p">Printer to be added</param>
            /// <returns>success or failure</returns>
            public static async Task<bool> PostPrinterAsync(Printer p)
            {
                return await Post(url + AllRoutes.Create, p);
            }

            //TODO: not currently in API
            /// <summary>
            /// Updates the printer status
            /// </summary>
            /// <param name="id">primary key of printer we want to update</param>
            /// <param name="inUse">value t be updated</param>
            /// <returns></returns>
            //public static async Task<bool> GetPrinterSetStatus(int id, bool inUse)
            //{
            //    var res = await Get<Printer>(url + id + AllRoutes.PrintersRoutes.Procedures.Status + Queries.InUse + inUse);
            //    return res.InUse == inUse;
            //}
        }

        #endregion

        #region Unused
        
        #region Equipments

        /// <summary>
        /// for accessing the equipment table through the API
        /// </summary>
        public static class EquipmentAccessor
        {
            private static string url = AllRoutes.EquipmentRoutes.FullURL;//url to equipment routes in API

            /// <summary>
            /// get the equipment from for the given key
            /// </summary>
            /// <param name="equipID">primary key of the equipment row we want</param>
            /// <returns>Equipment object for the key</returns>
            public static async Task<Equipment> GetEquipmentAsync(int equipID)
            {
                return await Get<Equipment>(url, equipID);
            }

            /// <summary>
            /// get all the equipment rows in the table
            /// </summary>
            /// <returns>List of equipment objects</returns>
            public static async Task<List<Equipment>> GetAllEquipmentAsync()
            {
                return await Get<List<Equipment>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region Orders

        ////TODO: this needs updating
        ///// <summary>
        ///// for making requests to Orders table through the API
        ///// </summary>
        //public static class OrderAccessor
        //{
        //    private static string url = AllRoutes.OrdersRoutes.FullURL;//url to Orders routes in API

        //    /// <summary>
        //    /// Get a single row from the Orders table
        //    /// </summary>
        //    /// <param name="orderID">Primary key of row to be returned</param>
        //    /// <returns>Order object</returns>
        //    public static async Task<Order> GetOrderAsync(int orderID)
        //    {
        //        return await Get<Order>(url, orderID);
        //    }

        //    /// <summary>
        //    /// Get all rows from the Orders table
        //    /// </summary>
        //    /// <returns>List of Order objects</returns>
        //    public static async Task<List<Order>> GetAllOrdersAsync()
        //    {
        //        return await Get<List<Order>>(url + AllRoutes.All);
        //    }

        //    /// <summary>
        //    /// Add a new Order to the Orders table
        //    /// runs CreateOrder stored procedure
        //    /// </summary>
        //    /// <param name="parms">procedure input parameters</param>
        //    /// <returns>Order primary key, customerid, barcode it and quantity of order</returns>
        //    public static async Task<CreateOrderOutputParams> PostCreateOrder(CreateOrderInputParams parms)
        //    {
        //        return await Post<CreateOrderInputParams, CreateOrderOutputParams>(url + AllRoutes.Add, parms) as CreateOrderOutputParams;
        //    }
        //}

        #endregion
        #region OrderDetails

        /// <summary>
        /// for making requests to the OrderDetails table through the API
        /// </summary>
        public static class OrderDetailsAccessor
        {
            private static string url = AllRoutes.OrderDetailsRoutes.FullURL;//url to orderdetails routes in the API

            /// <summary>
            /// Get a particular row from the OrderDetails table
            /// </summary>
            /// <param name="orderDetailsID">primary key for row we want to return</param>
            /// <returns>OrderDetails object</returns>
            public static async Task<OrderDetails> GetOrderDetails(int orderDetailsID)
            {
                return await Get<OrderDetails>(url, orderDetailsID);
            }

            /// <summary>
            /// get all rows from the order details table
            /// </summary>
            /// <returns>list of OrderDetails objects</returns>
            public static async Task<List<OrderDetails>> GetAllOrderDetails()
            {
                return await Get<List<OrderDetails>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region OrderStatuses

        /// <summary>
        /// for making requests to the API for the OrderStatus table
        /// </summary>
        public static class OrderStatusAcceessor
        {
            private static string url = AllRoutes.OrderStatusRoutes.FullURL;//url for making requests to the API

            /// <summary>
            /// get a single row from the OrderStatus table
            /// </summary>
            /// <param name="orderStatusID">primary key for row to be returned</param>
            /// <returns>a single OrderStatus object</returns>
            public static async Task<OrderStatus> GetOrderStatusAsync(int orderStatusID)
            {
                return await Get<OrderStatus>(url, orderStatusID);
            }

            /// <summary>
            /// get all the rows from the OrderStatus table
            /// </summary>
            /// <returns>List of OrderStatus objects</returns>
            public static async Task<List<OrderStatus>> GetAllOrderStatusAsync()
            {
                return await Get<List<OrderStatus>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region Reagents

        /// <summary>
        /// for making API requests for Reagents table
        /// </summary>
        public static class ReagentAccessor
        {
            private static string url = AllRoutes.ReagentsRoutes.FullURL;//url to Reagent routes in API

            /// <summary>
            /// get a single row from the reagents table            
            /// </summary>
            /// <param name="reagentID">primary key of row to be returned</param>
            /// <returns>Reagent object</returns>
            public static async Task<Reagent> GetReagentAsync(int reagentID)
            {
                return await Get<Reagent>(url, reagentID);
            }

            /// <summary>
            /// Get all rows from Reagents table
            /// </summary>
            /// <returns>list of reagent objects</returns>
            public static async Task<List<Reagent>> GetAllReagentsAsync() 
            {
                return await Get<List<Reagent>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region Specimens

        /// <summary>
        /// for making requests to the Specimens table
        /// </summary>
        public static class SpecimenAccessor
        {
            private static string url = AllRoutes.SpecimensRoutes.FullURL;//url for Specimen routes in the API

            /// <summary>
            /// Get a single from from the Specimen table
            /// </summary>
            /// <param name="specimenID">primary key of row to be returned</param>
            /// <returns>specimen row from table</returns>
            public static async Task<Specimen> GetSpecimenAsync(int specimenID)
            {
                return await Get<Specimen>(url, specimenID);
            }

            /// <summary>
            /// Get all rows from the Specimens table
            /// </summary>
            /// <returns>List of specimen objects</returns>
            public static async Task<List<Specimen>> GetAllSpecimensAsync()
            {
                return await Get<List<Specimen>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region SpecimenStatuses

        /// <summary>
        /// for making requests to SpecimenStatuses table
        /// </summary>
        public static class SpecimenStatusAccessor 
        {
            private static string url = AllRoutes.SpecimenStatusRoutes.FullURL;//url to SpecimenStatus routes in API

            /// <summary>
            /// Get a single row from Specimen Status table
            /// </summary>
            /// <param name="SpecimenStatusID">primary key of row to be returned</param>
            /// <returns>row for the primary key</returns>
            public static async Task<SpecimenStatus> GetSpecimenStatusAsync(int SpecimenStatusID)
            {
                return await Get<SpecimenStatus>(url, SpecimenStatusID);
            }

            /// <summary>
            /// Get all rows from Specimen Status table
            /// </summary>
            /// <returns>List of rows from table</returns>
            public static async Task<List<SpecimenStatus>> GetAllSpecimenStatusAsync()
            {
                return await Get<List<SpecimenStatus>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region SpecimenStatusUpdates

        /// <summary>
        /// For making requests to SpecimenStatusUpdate table through API
        /// </summary>
        public static class SpecimenStatusUpdateAccessor
        {
            private static string url = AllRoutes.SpecimenStatusUpdatesRoutes.FullURL;//url to SpecimenStatusUpdate routes in API

            /// <summary>
            /// Get a single row from SpecimenStatusUpdate table
            /// </summary>
            /// <param name="SpecimenStatusUpdateID">primary key for row to be returned</param>
            /// <returns>SpecimenStatusUpdate object</returns>
            public static async Task<SpecimenStatusUpdate> GetSpecimenStatusUpdateAsync(int SpecimenStatusUpdateID)
            {
                return await Get<SpecimenStatusUpdate>(url, SpecimenStatusUpdateID);
            }

            /// <summary>
            /// Get all rows from the SpecimenStatusUpdate table
            /// </summary>
            /// <returns>List of SpecimenStatusUpdate objects</returns>
            public static async Task<List<SpecimenStatusUpdate>> GetAllSpecimenStatusUpdatesAsync()
            {
                return await Get<List<SpecimenStatusUpdate>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region SpecimenTypes

        /// <summary>
        /// For making requests to the SpecimeType table through the API
        /// </summary>
        public static class SpecimenTypeAccessor 
        {
            private static string url = AllRoutes.SpecimenTypesRoutes.FullURL;//url for SpecimenType routes

            /// <summary>
            /// Get single row from SpecimenType table
            /// </summary>
            /// <param name="specimenTypeID">Primary key of row to be returned</param>
            /// <returns>SpecimenType object</returns>
            public static async Task<SpecimenType> GetSpecimenTypeAsync(int specimenTypeID)
            {
                return await Get<SpecimenType>(url, specimenTypeID);
            }

            /// <summary>
            /// Get all rows from SpecimenType table
            /// </summary>
            /// <returns>List of SpecimenType objects</returns>
            public static async Task<List<SpecimenType>> GetAllSpecimenTypesAsync()
            {
                return await Get<List<SpecimenType>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region Stations

        /// <summary>
        /// for making requests to Station table through API
        /// </summary>
        public static class StationAccessor 
        {
            private static string url = AllRoutes.StationsRoutes.FullURL;//url to Station routes in API

            /// <summary>
            /// Get a single row from the stations table
            /// </summary>
            /// <param name="stationID">primary key of row to be returned</param>
            /// <returns>Station object</returns>
            public static async Task<Station> GetStationAsync(int stationID)
            {
                return await Get<Station>(url, stationID);
            }

            /// <summary>
            /// Get all rows from Stations table
            /// </summary>
            /// <returns>List of Station objects</returns>
            public static async Task<List<Station>> GetAllStationsAsync()
            {
                return await Get<List<Station>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region User

        /// <summary>
        /// for making requests to Users table through API
        /// </summary>
        public static class UserAccessor 
        {
            private static string url = AllRoutes.UsersRoutes.FullURL;//url to User routes through API

            /// <summary>
            /// Get single row from Users table
            /// </summary>
            /// <param name="userID">primary key for row to be returned</param>
            /// <returns>User object</returns>
            public static async Task<User> GetUserAsync(int userID)
            {
                return await Get<User>(url, userID);
            }

            /// <summary>
            /// Get all rows from Users table
            /// </summary>
            /// <returns>List of User objects</returns>
            public static async Task<List<User>> GetAllUsersAsync()
            {
                return await Get<List<User>>(url + AllRoutes.All);
            }

            /// <summary>
            /// Create a new user in the User table
            /// </summary>
            /// <param name="user">user to be added to the table</param>
            /// <returns>Primary key of entered row</returns>
            public static async Task<int> PostCreateUser(User user)
            {
                return (int)await Post<User, int>(url, user);
            }
        }

        #endregion
        #region Usernames

        /// <summary>
        /// for making requests to Username table through the API
        /// </summary>
        public static class UsernameAccessor 
        {
            private static string url = AllRoutes.UsernamesRoutes.FullURL;//url to Username routes in API

            /// <summary>
            /// Get a single row from Username table
            /// </summary>
            /// <param name="usernameID">Primery key of row to be returned</param>
            /// <returns>username object</returns>
            public static async Task<Username> GetUsernameAsync(int usernameID)
            {
                return await Get<Username>(url, usernameID);
            }

            /// <summary>
            /// Get all rows from Username table
            /// </summary>
            /// <returns>List of Username objects</returns>
            public static async Task<List<Username>> GetAllUsernamesAsync() 
            {
                return await Get<List<Username>>(url + AllRoutes.All);
            }
        }

        #endregion

        //Thermocyclers and tempQuantity, tracking not working right now
        //probably need to build the json converter
        #region TempQuantities

        /// <summary>
        /// for making requests to TempQuantity table through API
        /// </summary>
        public static class TempQuantityAccessor
        {
            private static string url = AllRoutes.TempQuantitiesRoutes.FullURL;//url for TempQuantity routes in API

            /// <summary>
            /// Get a single row from the TempQuantity table
            /// </summary>
            /// <param name="tempQtyID">Primary key of row to be returned</param>
            /// <returns>TempQuantity object</returns>
            public static async Task<TempQuantity> GetTempQuantityAsync(int tempQtyID)
            {
                return await Get<TempQuantity>(url, tempQtyID);
            }

            /// <summary>
            /// Get all rows from TempQuantity table
            /// </summary>
            /// <returns>List of TempQuantity objects</returns>
            public static async Task<List<TempQuantity>> GetAllTempQuantitiesAsync()
            {
                return await Get<List<TempQuantity>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region Thermocyclers

        /// <summary>
        /// for making requests to Thermocyclers table through API
        /// </summary>
        public static class ThermocyclerAccessor
        {
            private static string url = AllRoutes.ThermocyclersRoutes.FullURL;//url to Thermocycler routes in API

            /// <summary>
            /// Get a single row from Thermocyclers table
            /// </summary>
            /// <param name="thermoID">Primary key of row to be returned</param>
            /// <returns>Thermocycler object</returns>
            public static async Task<Thermocycler> GetThermocyclerAsync(int thermoID)
            {
                return await Get<Thermocycler>(url, thermoID);
            }

            /// <summary>
            /// Get all rows from Thermocyclers table
            /// </summary>
            /// <returns>List of Thermocycler objects</returns>
            public static async Task<List<Thermocycler>> GetAllThermoCyclersAsync()
            {
                return await Get<List<Thermocycler>>(url + AllRoutes.All);
            }
        }

        #endregion
        #region Trackings

        /// <summary>
        /// For making requests to Trackings table through API
        /// </summary>
        public static class TrackingAccessor
        {
            private static string url = AllRoutes.TrackingsRoutes.FullURL;//url to Tracking routes in API

            /// <summary>
            /// Get a single row from Tracking table
            /// </summary>
            /// <param name="trackingID">Primary key of row to be returned</param>
            /// <returns>Tracking object</returns>
            public static async Task<Tracking> GetTrackingAsync(int trackingID)
            {
                return await Get<Tracking>(url, trackingID);
            }

            /// <summary>
            /// Get all rows from Trackings table
            /// </summary>
            /// <returns>List of Tracking objects</returns>
            public static async Task<List<Tracking>> GetAllTrackingsAsync()
            {
                return await Get<List<Tracking>>(url + AllRoutes.All);
            }
        }

        #endregion

        #endregion
    }
}
