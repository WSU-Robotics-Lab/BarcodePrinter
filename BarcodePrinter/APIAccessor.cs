using System;
using System.Collections.Generic;
using System.Linq;
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
    public class APIAccessor
    {
        private static HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = true };
        private static HttpClient client = new HttpClient(handler, false);
        private static bool _auth = false;
        
        public static void SetAuth(string username, string pass)
        {
            string creds = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + pass));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            _auth = true;
        }

        #region baseRequests
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

        private static async Task<bool> Put<T>(string url, T data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var res = await client.PutAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }

        private static async Task<bool> Post<T>(string url, T data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            var res = await client.PostAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }

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
        //private static async Task<bool> Delete(string url)
        //{
        //    var res = await client.DeleteAsync(url);
        //    return res.IsSuccessStatusCode;
        //}
        
        #endregion

        public class LabelAccessor
        {
            private static string url = API_Lib.Routes.AllRoutes.LabelRoutes.FullURL;
            
            public static async Task<List<Label>> GetAllLabelsAsync()
            {
                return await Get<List<Label>>(url + AllRoutes.All);
            }

            public static async Task<Label> GetLabelAsync(int id)
            {
                return await Get<Label>(url, id);
            }

            public static async Task<string> GetPrintLabelAsync(string id, bool print = false)
            {
                return await Get<string>(url + "print" + id + Queries.Print + print.ToString());
            }
            public static async Task<CreateLabelOutput> PostCreateLabel(CreateLabelInput input)
            {
                return await Post<CreateLabelInput, CreateLabelOutput>(url + "create", input) as CreateLabelOutput;
            }
        }

        public static class BarcodeAccessor
        {
            public static string url = AllRoutes.BarcodeRoutes.FullURL;

            public static async Task<int> GetLastBarcodeAsync(int custID)
            {
                return await Get<int>(url + "lastnum" + custID);
            }

            public static async Task<List<Barcode>> GetAllBarcodesAsync()
            {
                return await Get<List<Barcode>>(url + AllRoutes.All);
            }

            public static async Task<Barcode> GetBarcode(int barcodeID)
            {
                return await Get<Barcode>(url, barcodeID);
            }

            //public static async Task<int> GetLastNum(int customerID)
            //{
            //    return await Get<int>(url + "lastnum" + customerID.ToString());
            //}
        }

        public static class CustomerAccessor
        {
            private static string url = AllRoutes.CustomerRoutes.FullURL;
            public static async Task<Customer> GetCustomerAsync(int custID)
            {
                return await Get<Customer>(url, custID);
            }

            public static async Task<List<Customer>> GetAllCustomersAsync()
            {
                return await Get<List<Customer>>(url + AllRoutes.All);
            }

            public static async Task<bool> GetCustomerExistsAsync(string id)
            {
                return await Get<bool>(url + id + AllRoutes.Exists);
            }
        }

        public static class EquipmentAccessor
        {
            private static string url = AllRoutes.EquipmentRoutes.FullURL;
            public static async Task<Equipment> GetEquipmentAsync(int equipID)
            {
                return await Get<Equipment>(url, equipID);
            }

            //public static string GetAllEquipmentAsync()
            public static async Task<List<Equipment>> GetAllEquipmentAsync()
            {
                return await Get<List<Equipment>>(url + AllRoutes.All);
            }
        }
        //this needs updating when we add the orders controller

        //public static class OrderAccessor
        //{
        //    private static string url = AllRoutes.OrdersRoutes.FullURL;
        //    public static async Task<Order> GetOrderAsync(int orderID)
        //    {
        //        return await Get<Order>(url, orderID);
        //    }

        //    public static async Task<List<Order>> GetAllOrdersAsync()
        //    {
        //        return await Get<List<Order>>(url + AllRoutes.All);
        //    }

        //    public static async Task<CreateOrderOutputParams> PostCreateOrder(CreateOrderInputParams parms)
        //    {
        //        return await Post<CreateOrderInputParams, CreateOrderOutputParams>(url + AllRoutes.Add, parms) as CreateOrderOutputParams;
        //    }
                 
        //    //todo: add post request
        //    //todo: add put request
        //}

        public static class OrderDetailsAccessor
        {
            private static string url = AllRoutes.OrderDetailsRoutes.FullURL;

            public static async Task<OrderDetails> GetOrderDetails(int orderDetailsID)
            {
                return await Get<OrderDetails>(url, orderDetailsID);
            }

            public static async Task<List<OrderDetails>> GetAllOrderDetails()
            {
                return await Get<List<OrderDetails>>(url + AllRoutes.All);
            }
        
            //todo: add post request
            //todo: add put request
        }

        public static class OrderStatusAcceessor
        {
            private static string url = AllRoutes.OrderStatusRoutes.FullURL;

            public static async Task<OrderStatus> GetOrderStatusAsync(int orderStatusID)
            {
                return await Get<OrderStatus>(url, orderStatusID);
            }

            public static async Task<List<OrderStatus>> GetAllOrderStatusAsync()
            {
                return await Get<List<OrderStatus>>(url + AllRoutes.All);
            }
            
            //todo: add post request
            //todo: add put request
        }

        public static class PrintersAccessor
        {
            private static string url = AllRoutes.PrintersRoutes.FullURL;

            public static async Task<Printer>GetPrinterAsync(byte printerId)
            {
                return await Get<Printer>(url, printerId);
            }

            public static async Task<List<Printer>> GetAllPrintersAsync()
            {
                return await Get<List<Printer>>(url + AllRoutes.All);
            }

            public static async Task<bool> GetPrinterSetStatus(int id, bool inUse)
            {
                var res = await Get<Printer>(url + id + AllRoutes.PrintersRoutes.Procedures.Status + Queries.InUse + inUse);
                return res.InUse == inUse;
            }

            public static async Task<bool> PostPrinterAsync(Printer p)
            {
                return await Post(url + AllRoutes.Create, p);
            }

        }

        public static class ReagentAccessor
        {
            private static string url = AllRoutes.ReagentsRoutes.FullURL;

            public static async Task<Reagent> GetReagentAsync(int reagentID)
            {
                return await Get<Reagent>(url, reagentID);
            }

            public static async Task<List<Reagent>> GetAllReagentsAsync() 
            {
                return await Get<List<Reagent>>(url + AllRoutes.All);
            }
            
            //todo: add post request
            //todo: add put request
        }

        public static class SpecimenAccessor 
        {
            private static string url = AllRoutes.SpecimensRoutes.FullURL;

            public static async Task<Specimen> GetSpecimenAsync(int specimenID)
            {
                return await Get<Specimen>(url, specimenID);
            }

            public static async Task<List<Specimen>> GetAllSpecimensAsync()
            {
                return await Get<List<Specimen>>(url + AllRoutes.All);
            }
            //todo: add post request
            //todo: add put request
        }

        public static class SpecimenStatusAccessor 
        {
            private static string url = AllRoutes.SpecimenStatusRoutes.FullURL;

            public static async Task<SpecimenStatus> GetSpecimenStatusAsync(int SpecimenStatusID)
            {
                return await Get<SpecimenStatus>(url, SpecimenStatusID);
            }

            public static async Task<List<SpecimenStatus>> GetAllSpecimenStatusAsync()
            {
                return await Get<List<SpecimenStatus>>(url + AllRoutes.All);
            }
            //todo: add post request
            //todo: add put request
        }

        public static class SpecimenStatusUpdateAccessor
        {
            private static string url = AllRoutes.SpecimenStatusUpdatesRoutes.FullURL;

            public static async Task<SpecimenStatusUpdate> GetSpecimenStatusUpdateAsync(int SpecimenStatusUpdateID)
            {
                return await Get<SpecimenStatusUpdate>(url, SpecimenStatusUpdateID);
            }

            public static async Task<List<SpecimenStatusUpdate>> GetAllSpecimenStatusUpdatesAsync()
            {
                return await Get<List<SpecimenStatusUpdate>>(url + AllRoutes.All);
            }
         
         //todo: add post request
         //todo: add put request
        }

        public static class SpecimenTypeAccessor 
        {
            private static string url = AllRoutes.SpecimenTypesRoutes.FullURL;

            public static async Task<SpecimenType> GetSpecimenTypeAsync(int specimenTypeID)
            {
                return await Get<SpecimenType>(url, specimenTypeID);
            }

            public static async Task<List<SpecimenType>> GetAllSpecimenTypesAsync()
            {
                return await Get<List<SpecimenType>>(url + AllRoutes.All);
            }
            
            //todo: add post request
            //todo: add put request
        }

        public static class StationAccessor 
        {
            private static string url = AllRoutes.StationsRoutes.FullURL;

            public static async Task<Station> GetStationAsync(int stationID)
            {
                return await Get<Station>(url, stationID);
            }

            public static async Task<List<Station>> GetAllStationsAsync()
            {
                return await Get<List<Station>>(url + AllRoutes.All);
            }
            
            //todo: add post request
            //todo: add put request
        }

        //Thermocyclers and tempQuantity, tracking not working right now
        //probably need to build the json converter
        //public static class TempQuantityAccessor 
        //{
        //    private static string url = AllRoutes.TempQuantitiesRoutes.FullURL;

        //    public static async Task<TempQuantity> GetTempQuantityAsync(int tempQtyID) 
        //    {
        //        return await Get<TempQuantity>(url, tempQtyID);
        //    }

        //    public static async Task<List<TempQuantity>> GetAllTempQuantitiesAsync()
        //    {
        //        return await Get<List<TempQuantity>>(url + AllRoutes.All);
        //    }

        //    //todo: add post request
        //    //todo: add put request
        //}

        //public static class ThermocyclerAccessor 
        //{
        //    private static string url = AllRoutes.ThermocyclersRoutes.FullURL;

        //    public static async Task<Thermocycler> GetThermocyclerAsync(int thermoID)
        //    {
        //        return await Get<Thermocycler>(url, thermoID);
        //    }

        //    public static async Task<List<Thermocycler>> GetAllThermoCyclersAsync()
        //    {
        //        return await Get<List<Thermocycler>>(url + AllRoutes.All);
        //    }

        //    //todo: add post request
        //    //todo: add put request
        //}

        //public static class TrackingAccessor 
        //{
        //    private static string url = AllRoutes.TrackingsRoutes.FullURL;

        //    public static async Task<Tracking> GetTrackingAsync(int trackingID)
        //    {
        //        return await Get<Tracking>(url, trackingID);
        //    }

        //    public static async Task<List<Tracking>> GetAllTrackingsAsync()
        //    {
        //        return await Get<List<Tracking>>(url + AllRoutes.All);
        //    }
            
        //    //todo: add post request
        //    //todo: add put request
        //}

        public static class UserAccessor 
        {
            private static string url = AllRoutes.UsersRoutes.FullURL;

            public static async Task<User> GetUserAsync(int userID)
            {
                return await Get<User>(url, userID);
            }

            public static async Task<List<User>> GetAllUsersAsync()
            {
                return await Get<List<User>>(url + AllRoutes.All);
            }

            public static async Task<int> PostCreateUser(User user)
            {
                return (int)await Post<User, int>(url, user);
            }
           
            //todo: add post request
            //todo: add put request
        }

        public static class UsernameAccessor 
        {
            private static string url = AllRoutes.UsernamesRoutes.FullURL;

            public static async Task<Username> GetUsernameAsync(int usernameID)
            {
                return await Get<Username>(url, usernameID);
            }

            public static async Task<List<Username>> GetAllUsernamesAsync() 
            {
                return await Get<List<Username>>(url + AllRoutes.All);
            }
            
            //todo: add post request
            //todo: add put request
        }
    }
}
