using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrinter
{
    public class PrinterSettings
    {
        LabelSettings mainLabel;
        LabelSettings individualLabel;
        public bool UseCutter;
        public PrinterSettings(bool cutter)
        {
            mainLabel = new LabelSettings();
            individualLabel = new LabelSettings(60, 50, 16);
            UseCutter = cutter;
        }
        
        public PrinterSettings(int mainLeft = 15, int mainTop = 60, int mainDarkness = 16, int indLeft = 15, int indTop = 40, int indDark = 16, bool cutter = false)
        {
            mainLabel = new LabelSettings(mainLeft, mainTop, mainDarkness);
            individualLabel = new LabelSettings(indLeft, indTop, indDark);
            UseCutter = cutter;
        }

        #region getters/setters
        public int MainLeft
        {
            get { return mainLabel.Left; }
            set 
            { 
                if (value > 0 && value <= 32000)
                    mainLabel.Left = value; 
            }
        }
        public int MainTop
        {
            get { return mainLabel.Top; }
            set
            {
                if (value > 0 && value <= 32000)
                    mainLabel.Top = value;
            }
        }
        public int MainDarkness
        {
            get { return mainLabel.Darkness; }
            set 
            { 
                if (value >= 0 && value <= 30)
                    mainLabel.Darkness = value; 
            }
        }

        public int IndividualLeft
        {
            get { return individualLabel.Left; }
            set {
                if (value > 0 && value <= 32000) 
                    individualLabel.Left = value; }
        }
        public int IndividualTop
        {
            get { return individualLabel.Top; }
            set {
                if (value > 0 && value <= 32000) 
                    individualLabel.Top = value; }
        }
        public int IndividualDarkness
        {
            get { return individualLabel.Darkness; }
            set { 
                if (value > 0 && value <= 30) 
                    individualLabel.Darkness = value; }
        }
        #endregion
        
    }
}
