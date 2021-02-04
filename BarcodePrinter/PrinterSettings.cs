using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrinter
{
    public class PrinterSettings
    {
        int _left;
        int _top;
        int _darkness;
        public int Left
        {
            get { return _left; }
        }
        public int Top
        {
            get
            {
                return _top;
            }
        }
        public int Darkness
        {
            get
            {
                return _darkness;
            }
        }
        public PrinterSettings()
        {
            _left = 15;
            _top = 50;
            _darkness = 10;
        }
        
        public PrinterSettings(int left, int top, int darkness)
        {
            _left = left;
            _top = top;
            _darkness = darkness;
        }

        
    }
}
