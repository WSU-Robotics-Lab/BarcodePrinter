using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrinter
{
    public class LabelSettings
    {
        int _left;
        int _top;
        int _darkness;

        #region getters/setters
        public int Left
        {
            get { return _left; }
            set { _left = value; }
        }
        public int Top
        {
            get { return _top; }
            set { _top = value; }
        }
        public int Darkness
        {
            get { return _darkness; }
            set { _darkness = value; }
        }
        #endregion

        public LabelSettings(int left = 60, int top = 50, int darkness = 16)
        {
            _left = left;
            _top = top;
            _darkness = darkness;
        }
    }
}
