using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrinter
{
    public class Customer
    {
        public int CustomerID;
        public string CustomerName;
        public string SubCustomer;
        public Customer(string name, int ID, string subCustomer)
        {
            CustomerName = name;
            CustomerID = ID;
            SubCustomer = subCustomer;
        }
    }
}
