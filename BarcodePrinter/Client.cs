using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrinter
{
    public class Client
    {
        public string Name { get; set; }
        public string Code { get; set; }

        public Client(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
