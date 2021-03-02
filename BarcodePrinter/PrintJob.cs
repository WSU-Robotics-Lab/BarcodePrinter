using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BarcodePrinter.PrinterSettings;

namespace BarcodePrinter
{
    public class PrintJob
    {
        public enum PrintOptions
        {
            End,
            Label,
            Peel,
            Tear
        }

        public string Model { get; set; }
        public string Identifier { get; set; }

        Zebra.Sdk.Comm.ConnectionA connection;
        private PrinterSettings printerSettings;
        bool labelFormatSet = false;
        
        
        public PrintJob(Zebra.Sdk.Comm.ConnectionA conn, PrinterSettings settings)
        {
            printerSettings = settings;
            connection = conn;
            QueryPrinter();

            if (conn.SimpleConnectionName.Contains("usb"))
            {
                var c = (Zebra.Sdk.Comm.UsbConnection)conn;
                Identifier = c.SerialNumber;
            }
            else
            {
                Identifier = conn.SimpleConnectionName.Split('.')[0];
            }
            
        }
        
        ~PrintJob()
        {
            if (connection.Connected) 
                connection.Close(); 
        }

        public void Close()
        {
            if (connection.Connected)
                connection.Close();
        }

        public void QueryPrinter()
        {
            if (!connection.Connected) { connection.Open(); }
            StringBuilder QueryPrinter = new StringBuilder();
            QueryPrinter.AppendLine("^XA");
            QueryPrinter.AppendLine("~HI");
            QueryPrinter.AppendLine("^XZ");

            string PrinterInformation = Encoding.ASCII.GetString(connection.SendAndWaitForResponse(Encoding.ASCII.GetBytes(QueryPrinter.ToString()), 1000, 1000, ""));
            if (PrinterInformation.Contains("610"))
                Model = "ZT610";
            else if (PrinterInformation.Contains("220"))
                Model = "220";

            //USB Printers
            else if (PrinterInformation.Contains("420"))
                Model = "ZD420";
            else if (PrinterInformation.Contains("410"))
                Model = "ZD410";

        }
        public bool PrintMainLabel(int left, int top, int darkness, int rate, int tear, int iCustNum, PrintOptions opt)
        {
            StringBuilder MainLabel = new StringBuilder();
            MainLabel.Append("^XA");
            MainLabel.Append("^LH0,0");
            MainLabel.Append("^LT0");
            MainLabel.Append("^LS0");

            //fo x, y, justification (0,1,2)
            //bx orientation, height, quality, columns, rows
            //a font orientation, character height (dots), width(dots)
            //fb width (dots), numlines, add or delete space, justification, hanging indent
            
            // Print rate 
            MainLabel.Append("^PR").Append(rate);
            // Darkness
            MainLabel.Append("~SD").Append(darkness.ToString());
            // Tear / Cut offset
            MainLabel.Append("~TA").Append(string.Format("{0:000}",tear));
            // Thermal Transfer
            MainLabel.Append("^MTD");
            // print mode, T=tear, P=peel, C=cutter
            if (opt == PrintOptions.Label)  
                MainLabel.Append("^MMC");
            else if (opt == PrintOptions.Peel)
                MainLabel.Append("^MMP");
            else
                MainLabel.Append("^MMT");
            
            MainLabel.Append("^FO").Append(left.ToString()).Append(",").Append(top.ToString()).Append(",0 ^BXN,6,200,18,18 ^FD" + iCustNum.ToString() + " ^FS");//barcode
            MainLabel.Append("^FO").Append(left.ToString()).Append(",").Append(top.ToString()).Append(",0 ^A0N,60,0 ^FB400,1,0,C ^FD CUST ^FS");//CUST
            MainLabel.Append("^FO").Append(left.ToString()).Append(",").Append((top + 50).ToString()).Append(",0 ^A0N,60,0 ^FB400,1,0,C ^FD" + iCustNum.ToString() + " ^FS");//####

            MainLabel.Append("^XZ");
            try
            {
                if (!connection.Connected) { connection.Open(); }
                connection.Write(Encoding.ASCII.GetBytes(MainLabel.ToString()));
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool PrintMainLabel(int iCustNum)
        {
            return PrintMainLabel(printerSettings.MainLeft, printerSettings.MainTop, printerSettings.MainDarkness, printerSettings.PrintRate, printerSettings.TearOffset, iCustNum, printerSettings.options);
        }

        public bool SetIndividualLabelFormat(int left, int top, int darkness, int rate, int tear, out string error)
        {
            StringBuilder label = new StringBuilder();
            label.Append("^XA");
            label.Append("^DFR:LABEL.ZPL^FS");
            label.Append("^LH0,0 ");
            label.Append("^LT0");
            label.Append("^LS0");


            // Print rate 
            label.Append("^PR").Append(rate);
            // Darkness
            label.Append("~SD").Append(darkness.ToString());
            // Tear / Cut offset
            label.Append("~TA").Append(string.Format("{0:000}", tear));
            // Thermal Transfer
            label.Append("^MTD");
            
            //x, y position
            label.Append("^FO").Append(left.ToString()).Append(",").Append(top.ToString()).Append(",0 ^BXN,6,200,14,14 ^FN1^FS ");
            label.Append("^FO").Append(left.ToString()).Append(",").Append((top + 100).ToString()).Append(",0 ^A0N,30,0 ^FB400,1,0,L ^FN2^FS ");
            
            label.Append("^XZ");
            try
            {
                if (!connection.Connected) connection.Open();
                connection.Write(Encoding.ASCII.GetBytes(label.ToString()));
                labelFormatSet = true;
                error = "";
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
            
        }
        public bool PrintIndividualLabels(int left, int top, int darkness, int rate, int tear, string barcode, out string error, PrintOptions opt, bool lastLabel = false)
        {
            if (!labelFormatSet)
                if (!SetIndividualLabelFormat(left, top, darkness, rate, tear, out error))
                    return false;
                else
                    error = "";
            else
                error = "";

            
            int PrintNum = 0;

            int attempts = 0;
            StringBuilder ErrorCheck = new StringBuilder();
            ErrorCheck.AppendLine("^XA");
            ErrorCheck.AppendLine("~HQES");
            ErrorCheck.AppendLine("^XZ");

            while (attempts <= 10)
            {
                attempts++;
                string Errors = "";
                try
                {
                    Errors = Encoding.ASCII.GetString(connection.SendAndWaitForResponse(Encoding.ASCII.GetBytes(ErrorCheck.ToString()), 1000, 1000, ""));
                }
                catch (Exception e)
                {
                    error = e.Message;
                    return false;
                }

                string[] ErrorValues = Errors.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Select(respv => respv.Trim()).ToArray();
                bool errValue = false;
                foreach (string value in ErrorValues)
                {
                    if (value.Contains("ERROR") | value.Contains("WARNING"))
                    {
                        string[] values = value.Split(':').Select(respv => respv.Trim()).ToArray();
                        errValue = errValue & values[1].StartsWith("1");
                    }
                }
                if (!errValue)
                {
                    string sNumWDashes = String.Format("{0,0:0000}-000-{1,0:000-000-000}", barcode.Substring(0,4), barcode.Substring(4));
                    string sNumOnly = String.Format("{0,0:0000}000{1,0:000000000}", barcode.Substring(0, 4), barcode.Substring(4));

                    StringBuilder individualLabel = new StringBuilder();
                    individualLabel.AppendLine("^XA");
                    individualLabel.AppendLine("^XFR:LABEL.ZPL^FS");
                    individualLabel.Append("^FN1^FD").Append(sNumOnly).AppendLine("^FS");
                    individualLabel.Append("^FN2^FD").Append(sNumWDashes).AppendLine("^FS");
                    // print mode, T=tear, P=peel, C=cutter
                    if (opt == PrintOptions.Label)
                        individualLabel.Append("^MMC");
                    else if (opt == PrintOptions.End && lastLabel)
                        individualLabel.Append("^MMC");
                    else if (opt == PrintOptions.Peel)
                        individualLabel.Append("^MMP");
                    else
                        individualLabel.Append("^MMT");
                    individualLabel.AppendLine("^XZ");
                    try { connection.Write(Encoding.ASCII.GetBytes(individualLabel.ToString())); return true; }
                    catch (Exception e)
                    {
                        if (attempts == 10)
                        {
                            error = e.Message;
                            return false;
                        }
                    }
                    PrintNum++;
                } 
            }
            return false;
        }
        public bool PrintIndividualLabels(string barcode, out string error, bool lastBarcode = false)
        {
           return PrintIndividualLabels(
               printerSettings.IndividualLeft, 
               printerSettings.IndividualTop, 
               printerSettings.IndividualDarkness, 
               printerSettings.PrintRate,
               printerSettings.TearOffset,
               barcode, 
               out error,
               printerSettings.options,
               lastBarcode
               );
        }
        public bool PrintTestLabel(out string error)
        {
            return PrintIndividualLabels("1234123456", out error);
        }
    }
}
