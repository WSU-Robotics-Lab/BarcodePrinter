using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BarcodePrinter.PrintJob;

namespace BarcodePrinter
{
    /// <summary>
    /// Print settings for barcode printer
    /// </summary>
    public class PrinterSettings
    {
        LabelSettings mainLabel;//CUST 1234
        LabelSettings individualLabel;//1234-000-123456
        public PrintOptions options;//tear, peel, cut at end, cut each label
        public int PrintRate;
        public int TearOffset;

        public PrinterSettings(PrintOptions options = PrintOptions.End)
        {
            mainLabel = new LabelSettings();
            individualLabel = new LabelSettings(150, 50, 16);
            this.options = options;
            PrintRate = 1;
            TearOffset = -20;
        }

        public PrinterSettings(PrintOptions options = PrintOptions.End, int mainLeft = 150, int mainTop = 50, int mainDarkness = 16, int indLeft = 150, int indTop = 40, int indDark = 16, int rate = 1, int tear = -20)
        {
            individualLabel = new LabelSettings(indLeft, indTop, indDark);
            mainLabel = new LabelSettings(mainLeft, mainTop, mainDarkness);
            this.options = options;
            PrintRate = rate;
            TearOffset = tear;
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
