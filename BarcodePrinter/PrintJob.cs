using System;
using System.Linq;
using System.Text;

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

        public string Model { get; set; }//610, 220, 420
        public string Identifier { get; set; }//Serial number for USB printers, otherwise PrinterName

        Zebra.Sdk.Comm.ConnectionA connection;
        private PrinterSettings printerSettings;//settings for this job
        bool labelFormatSet = false;
        
        public PrintJob(Zebra.Sdk.Comm.ConnectionA conn, PrinterSettings settings)
        {
            printerSettings = settings;
            connection = conn;
            QueryPrinter();

            if (conn.SimpleConnectionName.Contains("usb"))//set Identifier to SerialNumber
            {
                var c = (Zebra.Sdk.Comm.UsbConnection)conn;
                Identifier = c.SerialNumber;
            }
            else//set Identifier to Printer name
            {
                Identifier = conn.SimpleConnectionName.Split('.')[0];
            }
        }
        
        ~PrintJob()
        {
            if (connection.Connected) 
                connection.Close(); 
        }

        /// <summary>
        /// Close connection to printer
        /// </summary>
        public void Close()
        {
            if (connection.Connected)
                connection.Close();
        }

        /// <summary>
        /// Get Printer information 
        /// </summary>
        private void QueryPrinter()
        {
            if (!connection.Connected) { connection.Open(); }//open connection
            
            //send query command to printer
            StringBuilder QueryPrinter = new StringBuilder();
            QueryPrinter.AppendLine("^XA");
            QueryPrinter.AppendLine("~HI");
            QueryPrinter.AppendLine("^XZ");

            //set model
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

        /// <summary>
        /// Print the barcode label for the Order
        /// _________________________
        /// |                       |
        /// |   Barcode    CUST     |
        /// |              1234     |
        /// |_______________________|
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="darkness"></param>
        /// <param name="rate"></param>
        /// <param name="tear"></param>
        /// <param name="iCustNum"></param>
        /// <param name="opt"></param>
        /// <returns></returns>
        private bool PrintMainLabel(int left, int top, int darkness, int rate, int tear, int iCustNum, PrintOptions opt)
        {
            StringBuilder MainLabel = new StringBuilder();
            MainLabel.Append("^XA");
            MainLabel.Append("^LH0,0");
            MainLabel.Append("^LT0");
            MainLabel.Append("^LS0");

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
            else if (opt == PrintOptions.Tear)
                MainLabel.Append("^MMT");
            
            //fo x, y, justification (0,1,2)
            //bx orientation, height, quality, columns, rows
            //a font orientation, character height (dots), width(dots)
            //fb width (dots), numlines, add or delete space, justification, hanging indent
            MainLabel.Append("^FO").Append(left.ToString()).Append(",").Append(top.ToString()).Append(",0 ^BXN,6,200,18,18 ^FD" + iCustNum.ToString() + " ^FS");//barcode
            MainLabel.Append("^FO").Append(left.ToString()).Append(",").Append(top.ToString()).Append(",0 ^A0N,60,0 ^FB400,1,0,C ^FD CUST ^FS");//CUST
            MainLabel.Append("^FO").Append(left.ToString()).Append(",").Append((top + 50).ToString()).Append(",0 ^A0N,60,0 ^FB400,1,0,C ^FD" + iCustNum.ToString() + " ^FS");//####
            MainLabel.Append("^XZ");

            try//try to send the label
            {
                if (!connection.Connected) { connection.Open(); }
                //connection.Write(Encoding.ASCII.GetBytes(MainLabel.ToString()));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// calls other method using stored settings
        /// </summary>
        /// <param name="iCustNum">customer number to be printed on the label</param>
        /// <returns>success or failure</returns>
        public bool PrintMainLabel(int iCustNum)
        {
            return PrintMainLabel(printerSettings.MainLeft, printerSettings.MainTop, printerSettings.MainDarkness, printerSettings.PrintRate, printerSettings.TearOffset, iCustNum, printerSettings.options);
        }

        /// <summary>
        /// sets the format of the label to be printed
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="darkness"></param>
        /// <param name="rate"></param>
        /// <param name="tear"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public bool SetIndividualLabelFormat(int left, int top, int darkness, int rate, int tear, out string error)
        {
            StringBuilder label = new StringBuilder();
            label.Append("^XA");
            label.Append("^DFR:LABEL.ZPL^FS");//download format for later use
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

            try//attempt connection
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

        /// <summary>
        /// Print label using format
        /// _________________________
        /// |                       |
        /// |   Barcode             |
        /// |                       |
        /// |   1234-000-12345678   |
        /// |_______________________|
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="darkness"></param>
        /// <param name="rate"></param>
        /// <param name="tear"></param>
        /// <param name="barcode"></param>
        /// <param name="error"></param>
        /// <param name="opt"></param>
        /// <param name="lastLabel"></param>
        /// <returns></returns>
        private bool PrintIndividualLabels(int left, int top, int darkness, int rate, int tear, string barcode, out string error, PrintOptions opt, bool lastLabel = false)
        {
            if (!labelFormatSet)//if not set
                if (!SetIndividualLabelFormat(left, top, darkness, rate, tear, out error))//try to set the format
                    return false;
                else
                    error = "";
            else
                error = "";
            
            //build error checking command
            StringBuilder ErrorCheck = new StringBuilder();
            ErrorCheck.AppendLine("^XA");
            ErrorCheck.AppendLine("~HQES");
            ErrorCheck.AppendLine("^XZ");

            int attempts = 0;
            while (attempts < 10)//try to print 10 times
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
                foreach (string value in ErrorValues)//check for any errors from printer
                {
                    if (value.Contains("ERROR") | value.Contains("WARNING"))
                    {
                        string[] values = value.Split(':').Select(respv => respv.Trim()).ToArray();
                        errValue = errValue & values[1].StartsWith("1");
                    }
                }
                if (!errValue)//if no errors, send data to print
                {
                    string sNumWDashes = String.Format("{0,0:0000}-000-{1,0:000-000-000}", barcode.Substring(0,4), barcode.Substring(4));//for readable number
                    string sNumOnly = String.Format("{0,0:0000}000{1,0:000000000}", barcode.Substring(0, 4), barcode.Substring(4));//value for barcode image

                    //build printer command
                    StringBuilder individualLabel = new StringBuilder();
                    individualLabel.AppendLine("^XA");
                    individualLabel.AppendLine("^XFR:LABEL.ZPL^FS");
                    individualLabel.Append("^FN1^FD").Append(sNumOnly).AppendLine("^FS");//barcode value
                    individualLabel.Append("^FN2^FD").Append(sNumWDashes).AppendLine("^FS");//readable value
                    
                    // print mode, T=tear, P=peel, C=cutter
                    if (opt == PrintOptions.Label)
                        individualLabel.Append("^MMC");
                    else if (opt == PrintOptions.End && lastLabel)
                        individualLabel.Append("^MMC");
                    else if (opt == PrintOptions.Peel)
                        individualLabel.Append("^MMP");
                    else if (opt == PrintOptions.Tear)
                        individualLabel.Append("^MMT");

                    individualLabel.AppendLine("^XZ");
                    
                    //send command, if successful, return true
                    try { 
                        //connection.Write(Encoding.ASCII.GetBytes(individualLabel.ToString())); 
                        return true; 
                    }
                    catch (Exception e)
                    {
                        if (attempts == 10)//stop trying
                        {
                            error = e.Message;
                            return false;
                        }
                    }
                } 
            }
            return false;
        }

        /// <summary>
        /// call other method with stored settings
        /// </summary>
        /// <param name="barcode">string to be printed</param>
        /// <param name="error">Output. If no errors, will be empty</param>
        /// <param name="lastBarcode">if cut at end is set, then cut after this label</param>
        /// <returns></returns>
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

        /// <summary>
        /// print a dummy test label
        /// </summary>
        /// <param name="error">will be empty if no error</param>
        /// <returns>success or failure</returns>
        public bool PrintTestLabel(out string error)
        {
            return PrintIndividualLabels("1234123456", out error);
        }
    }
}
