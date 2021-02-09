using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrinter
{
    public static class API_URLs
    {
        private static readonly string httpAddress = "http://localhost:4999/api/";
        public static readonly string barcodes = httpAddress + "barcode";
        public static readonly string customers = httpAddress + "customer";
        public static readonly string equipment = httpAddress + "equipment";
        public static readonly string orders = httpAddress + "orderDetails";
        public static readonly string orderDetails = httpAddress + "orders";
        public static readonly string orderStatus = httpAddress + "orderStatus";
        public static readonly string orderStatusUpdates = httpAddress + "orderStatusUpdates";
        public static readonly string printers = httpAddress + "printer";
        public static readonly string reagents = httpAddress + "reagent";
        public static readonly string specimens = httpAddress + "specimen";
        public static readonly string specimenStatus = httpAddress + "specimenStatus";
        public static readonly string specimenStatusUpdates = httpAddress + "specimenStatusUpdates";
        public static readonly string specimenTypes = httpAddress + "specimenTypes";
        public static readonly string stations = httpAddress + "stations";
        public static readonly string tempQuantities = httpAddress + "tempQuantity";
        public static readonly string thermocyclers = httpAddress + "thermocycler";
        public static readonly string trackings = httpAddress + "tracking";
        public static readonly string usernames = httpAddress + "username";
        public static readonly string users = httpAddress + "user";
    }
}
