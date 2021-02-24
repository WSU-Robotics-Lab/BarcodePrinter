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
        
        private PrinterSettings printerSettings;
        string model = "";
        Zebra.Sdk.Comm.ConnectionA connection;
        bool labelFormatSet = false;
        
        
        public PrintJob(Zebra.Sdk.Comm.ConnectionA conn, PrinterSettings settings)
        {
            printerSettings = settings;
            connection = conn;
            QueryPrinter();
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
                model = "610";
            else if (PrinterInformation.Contains("220"))
                model = "220";

            //USB Printers
            //else if (PrinterInformation.Contains("420"))
            //    model = "420";
            //else if (PrinterInformation.Contains("410"))
            //    model = "410";

        }
        public bool PrintMainLabel(int left, int top, int darkness, int iCustNum, out string sent)
        {
            StringBuilder MainLabel = new StringBuilder();
            MainLabel.Append("^XA");
            MainLabel.Append("^LH0,0 ");
            MainLabel.Append("^LT0");
            MainLabel.Append("^LS0");

            //fo x, y, justification (0,1,2)
            //bx orientation, height, quality, columns, rows
            //a font orientation, character height (dots), width(dots)
            //fb width (dots), numlines, add or delete space, justification, hanging indent
            if (model.Contains("610"))
            {
                // Print rate 
                MainLabel.Append("^PR6");
                // Darkness
                MainLabel.Append("~SD").Append(darkness.ToString());
                // Tear / Cut offset
                MainLabel.Append("~TA-010");
                // Thermal Transfer
                MainLabel.Append("^MTD");
                // print mode, T=tear, P=peel, C=cutter
                if (printerSettings.UseCutter)
                    MainLabel.Append("^MMC");
                else
                    MainLabel.Append("^MMT");
            }
            else if (model.Contains("220"))
            {
                MainLabel.Append("^PR1");
                MainLabel.Append("~SD").Append(darkness.ToString());
                MainLabel.Append("~TA-010");
                MainLabel.Append("^MTD");
                MainLabel.Append("^MMP");
            }
            //USB Printers
            //else if (model.Contains("420"))
            //{
            //    MainLabel.Append("^PR1");
            //    MainLabel.Append("~SD").Append(darkness.ToString());
            //    MainLabel.Append("~TA020");
            //    MainLabel.Append("^MTD");
            //    MainLabel.Append("^MMP");
            //}
            //else if (model.Contains("410"))
            //{
            //    MainLabel.Append("^PR1");
            //    MainLabel.Append("~SD").Append(darkness.ToString());
            //    MainLabel.Append("~TA020");
            //    MainLabel.Append("^MTD");
            //    MainLabel.Append("^MMT");
            //}

            MainLabel.Append("^FO").Append(left.ToString()).Append(top.ToString()).Append(",0 ^BXN,6,200,18,18 ^FD" + iCustNum.ToString() + " ^FS");//barcode
            MainLabel.Append("^FO").Append(left.ToString()).Append(top.ToString()).Append(",0 ^A0N,60,0 ^FB400,1,0,C ^FD CUST ^FS");//CUST
            MainLabel.Append("^FO").Append(left.ToString()).Append((top + 50).ToString()).Append(",0 ^A0N,60,0 ^FB400,1,0,C ^FD" + iCustNum.ToString() + " ^FS");//####

            MainLabel.Append("^XZ");
            try
            {
                if (!connection.Connected) { connection.Open(); }
                connection.Write(Encoding.ASCII.GetBytes(MainLabel.ToString()));
                sent = MainLabel.ToString();
                return true;
            }
            catch
            {
                sent = ""; ;
                return false;
            }
        }
        public bool PrintMainLabel(int iCustNum, out string sent)
        {
            return PrintMainLabel(printerSettings.MainLeft, printerSettings.MainTop, printerSettings.MainDarkness, iCustNum, out sent);
        }

        public bool SetIndividualLabelFormat(int left, int top, int darkness, out string error)
        {
            StringBuilder label = new StringBuilder();
            label.Append("^XA");
            label.Append("^DFR:LABEL.ZPL^FS");
            label.Append("^LH0,0 ");
            label.Append("^LT0");
            label.Append("^LS0");


            if (model == "610")
            {
                // Print rate 
                label.Append("^PR6");
                // Darkness
                label.Append("~SD").Append(darkness.ToString());
                // Tear / Cut offset
                label.Append("~TA-010");
                // Thermal Transfer
                label.Append("^MTD");
                // print mode, T=tear, P=peel, C=cutter
                if (printerSettings.UseCutter)
                    label.Append("^MMC");
                else
                    label.Append("^MMT");
            }
            else if (model == "220")
            {
                label.Append("^PR1");
                label.Append("~SD").Append(darkness.ToString());
                label.Append("~TA-010");
                label.Append("^MTD");
                label.Append("^MMP");
            }
            else if (model == "420")
            {
                label.Append("^PR1");
                label.Append("~SD").Append(darkness.ToString());
                label.Append("~TA020");
                label.Append("^MTD");
                label.Append("^MMP");
            }
            else if (model == "410")
            {
                label.Append("^PR1");
                label.Append("~SD").Append(darkness.ToString());
                label.Append("~TA020");
                label.Append("^MTD");
                label.Append("^MMT");
            }
            //if (model == "610")
            //{
            //    //fo x, y, justification (0,1,2)
            //    //bx orientation, height, quality, columns, rows
            //    MainLabel.Append("^FO60,50,0 ^BXN,10,200,14,14 ^FN1^FS ");
            //    //a font orientation, character height (dots), width(dots)
            //    //fb width (dots), numlines, add or delete space, justification, hanging indent
            //    MainLabel.Append("^FO30,250,0 ^A0N,45,0 ^FB550,1,0,C ^FN2^FS ");
            //} else
            //{
            label.Append("^FO").Append(left.ToString()).Append(top.ToString()).Append(",0 ^BXN,6,200,16,16 ^FN1^FS ");
            label.Append("^FO").Append(left.ToString()).Append((top + 100).ToString()).Append(",0 ^A0N,30,0 ^FB400,1,0,L ^FN2^FS ");
            //}
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
        public bool PrintIndividualLabels(int left, int top, int darkness, string barcode, bool cut, out string error)
        {
            if (!labelFormatSet)
                if (!SetIndividualLabelFormat(left, top, darkness, out error))
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
                    string sNumWDashes = String.Format("{0,0:0000}-{1,0:000-000-000}", barcode.Substring(0,4), barcode.Substring(4));
                    string sNumOnly = String.Format("{0,0:0000}{1,0:000000000}", barcode.Substring(0, 4), barcode.Substring(4));

                    StringBuilder individualLabel = new StringBuilder();
                    individualLabel.AppendLine("^XA");
                    individualLabel.AppendLine("^XFR:LABEL.ZPL^FS");
                    individualLabel.Append("^FN1^FD").Append(sNumOnly).AppendLine("^FS");
                    individualLabel.Append("^FN2^FD").Append(sNumWDashes).AppendLine("^FS");
                    if (cut)
                        individualLabel.Append("^MMC");
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
        public bool PrintIndividualLabels(string barcode, bool cut, out string error)
        {
           return PrintIndividualLabels(
               printerSettings.IndividualLeft, 
               printerSettings.IndividualTop, 
               printerSettings.IndividualDarkness, 
               barcode, 
               cut, 
               out error);
        }
        public bool PrintTestLabel(out string error)
        {
            return PrintIndividualLabels("1234123456", false, out error);
        }
    }
}
