using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace BarcodePrinter
{
    public static class APIAccessor
    {
        

        
        public static async string GetLastBarcodeAsync(int custID)
        { 
            using (HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = true })
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    var res = await client.GetStringAsync(API_URLs.barcodes);

                }
            }
        }
    }
}
