// VERSION HISTORY
// 0.6 - 1/4/2021 - Brian Brown
//      Fixed the large / extra large barcode on the 610.
//      Fixed the CUSTomer label for the 610.
// 0.7 - 1/11/2021 - Brian Brown
//      Shrinking barcode further
// 0.8 - 1/13/2021 - Brian Brown
//      Swapping for 203 dpi 610 printer head.




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BarcodePrinter
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const  string _Version = "0.8";

        private List<Zebra.Sdk.Comm.ConnectionA> _PrinterConnections;
        private System.Threading.Thread _Monitor;

        

        //private Timer _StatusTimer;
        public MainWindow()
        {
            InitializeComponent();
            _PrinterConnections = new List<Zebra.Sdk.Comm.ConnectionA>();
            _Monitor = new System.Threading.Thread(new System.Threading.ThreadStart(Monitor_Thread));
            Title += " Version: " +  _Version;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            _Monitor.Abort(); 
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections) Printer.Close();
            Application.Current.Shutdown();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tmpBox = sender as TextBox;
            int txtNumber;
            if (!int.TryParse(tmpBox.Text, out txtNumber))
            {
                MessageBox.Show("Value must be an integer!");
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (txtCustNumber.Text.Length < 1) { txtCustNumber.Text = "0"; };
            if (txtSubCustNumber.Text.Length < 1) { txtSubCustNumber.Text = "0"; };
            int iNumLabels = int.Parse(txtNumLabels.Text.Trim());
            int iStartNum = int.Parse(txtStartNum.Text.Trim());
            int iCustNum = int.Parse(txtCustNumber.Text.Trim());
            int iSubCustNum = int.Parse(txtSubCustNumber.Text.Trim());

            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
            {
                if (!Printer.Connected) { Printer.Open(); }
                StringBuilder QueryPrinter = new StringBuilder();
                QueryPrinter.AppendLine("^XA");
                QueryPrinter.AppendLine("~HI");
                QueryPrinter.AppendLine("^XZ");
                txbStatus.Text = txbStatus.Text + "Checking Printer Information for Printer" + System.Environment.NewLine;
                txbStatus.Refresh();
                string PrinterInformation = Encoding.ASCII.GetString(Printer.SendAndWaitForResponse(Encoding.ASCII.GetBytes(QueryPrinter.ToString()), 1000, 1000, ""));

                StringBuilder MainLabel = new StringBuilder();

                MainLabel.Append("^XA");
                MainLabel.Append("^LH0,0 ");
                MainLabel.Append("^LT0");
                MainLabel.Append("^LS0");
                if (PrinterInformation.Contains("610"))
                {
                    // Print rate 
                    MainLabel.Append("^PR6");
                    // Darkness
                    MainLabel.Append("~SD10");
                    // Tear / Cut offset
                    MainLabel.Append("~TA-010");
                    // Thermal Transfer
                    MainLabel.Append("^MTD");
                    // print mode, T=tear, P=peel, C=cutter
                    if (ckCut.IsChecked.HasValue && ckCut.IsChecked.Value.Equals(true))
                        MainLabel.Append("^MMC");
                    else
                        MainLabel.Append("^MMT");

                }
                else if (PrinterInformation.Contains("220"))
                {
                    MainLabel.Append("^PR1");
                    MainLabel.Append("~SD12");
                    MainLabel.Append("~TA-010");
                    MainLabel.Append("^MTD");
                    MainLabel.Append("^MMP");
                }
                else if (PrinterInformation.Contains("420"))
                {
                    MainLabel.Append("^PR1");
                    MainLabel.Append("~SD10");
                    MainLabel.Append("~TA020");
                    MainLabel.Append("^MTD");
                    MainLabel.Append("^MMP");
                }
                else if (PrinterInformation.Contains("410"))
                {
                    MainLabel.Append("^PR1");
                    MainLabel.Append("~SD10");
                    MainLabel.Append("~TA020");
                    MainLabel.Append("^MTD");
                    MainLabel.Append("^MMT");
                }
                //if (PrinterInformation.Contains("610"))
                //{
                //    //fo x, y, justification (0,1,2)
                //    //bx orientation, height, quality, columns, rows
                //    MainLabel.Append("^FO60,50,0 ^BXN,12,200,14,14 ^FD" + iCustNum.ToString() + " ^FS ");
                //    //a font orientation, character height (dots), width(dots)
                //    //fb width (dots), numlines, add or delete space, justification, hanging indent
                //    MainLabel.Append("^FO200,80,0 ^A0N,60,0 ^FB400,1,0,C ^FD CUST ^FS");
                //    MainLabel.Append("^FO100,150,0 ^A0N,60,0 ^FB550,1,0,C ^FD" + iCustNum.ToString() + "^FS ");
                //}
                //else
                //{
                    MainLabel.Append("^FO60,50,0 ^BXN,6,200,18,18 ^FD" + iCustNum.ToString() + " ^FS");
                    MainLabel.Append("^FO60,50,0 ^A0N,60,0 ^FB400,1,0,C ^FD CUST ^FS");
                    MainLabel.Append("^FO60,100,0 ^A0N,60,0 ^FB400,1,0,C ^FD" + iCustNum.ToString() + " ^FS");
                //}
                MainLabel.Append("^XZ");
                Printer.Write(Encoding.ASCII.GetBytes(MainLabel.ToString()));
                MainLabel = new StringBuilder();


                MainLabel.Append("^XA");
                MainLabel.Append("^DFR:LABEL.ZPL^FS");
                MainLabel.Append("^LH0,0 ");
                MainLabel.Append("^LT0");
                MainLabel.Append("^LS0");


                if (PrinterInformation.Contains("610"))
                {
                    // Print rate 
                    MainLabel.Append("^PR6");
                    // Darkness
                    MainLabel.Append("~SD10");
                    // Tear / Cut offset
                    MainLabel.Append("~TA-010");
                    // Thermal Transfer
                    MainLabel.Append("^MTD");
                    // print mode, T=tear, P=peel, C=cutter
                    if (ckCut.IsChecked.HasValue && ckCut.IsChecked.Value.Equals(true))
                        MainLabel.Append("^MMC");
                    else
                        MainLabel.Append("^MMT");
                }
                else if (PrinterInformation.Contains("220"))
                {
                    MainLabel.Append("^PR1");
                    MainLabel.Append("~SD12");
                    MainLabel.Append("~TA-010");
                    MainLabel.Append("^MTD");
                    MainLabel.Append("^MMP");
                }
                else if (PrinterInformation.Contains("420"))
                {
                    MainLabel.Append("^PR1");
                    MainLabel.Append("~SD10");
                    MainLabel.Append("~TA020");
                    MainLabel.Append("^MTD");
                    MainLabel.Append("^MMP");
                }
                else if (PrinterInformation.Contains("410"))
                {
                    MainLabel.Append("^PR1");
                    MainLabel.Append("~SD10");
                    MainLabel.Append("~TA020");
                    MainLabel.Append("^MTD");
                    MainLabel.Append("^MMT");
                }
                //if (PrinterInformation.Contains("610"))
                //{
                //    //fo x, y, justification (0,1,2)
                //    //bx orientation, height, quality, columns, rows
                //    MainLabel.Append("^FO60,50,0 ^BXN,10,200,14,14 ^FN1^FS ");
                //    //a font orientation, character height (dots), width(dots)
                //    //fb width (dots), numlines, add or delete space, justification, hanging indent
                //    MainLabel.Append("^FO30,250,0 ^A0N,45,0 ^FB550,1,0,C ^FN2^FS ");
                //} else
                //{
                    MainLabel.Append("^FO60,50,0 ^BXN,6,200,14,14 ^FN1^FS ");
                    MainLabel.Append("^FO30,150,0 ^A0N,30,0 ^FB400,1,0,C ^FN2^FS ");
                //}
                MainLabel.Append("^XZ");

                txbStatus.Text =  "Sending Main Label Information" + System.Environment.NewLine + txbStatus.Text;
                txbStatus.Refresh();

                Printer.Write(Encoding.ASCII.GetBytes(MainLabel.ToString()));
            }

            int PrintNum = 0;
            int i = 0;
            int attempts = 0;
            StringBuilder ErrorCheck = new StringBuilder();
            ErrorCheck.AppendLine("^XA");
            ErrorCheck.AppendLine("~HQES");
            ErrorCheck.AppendLine("^XZ");

            while (i < iNumLabels & attempts <= iNumLabels * 10)
            {
                attempts++;
                string Errors = Encoding.ASCII.GetString(_PrinterConnections[PrintNum].SendAndWaitForResponse(Encoding.ASCII.GetBytes(ErrorCheck.ToString()), 1000, 1000, ""));
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
                    string sNumWDashes = String.Format("{0,0:0000}-{1,0:000}-{2,0:000-000-000}", iCustNum, iSubCustNum, iStartNum + i);
                    string sNumOnly = String.Format("{0,0:0000}{1,0:000}{2,0:000000000}", iCustNum, iSubCustNum, iStartNum + i);

                    StringBuilder individualLabel = new StringBuilder();
                    individualLabel.AppendLine("^XA");
                    individualLabel.AppendLine("^XFR:LABEL.ZPL^FS");
                    individualLabel.Append("^FN1^FD").Append(sNumOnly).AppendLine("^FS");
                    individualLabel.Append("^FN2^FD").Append(sNumWDashes).AppendLine("^FS");
                    if (rdo610.IsChecked.Equals(true) && i == iNumLabels-1)
                        individualLabel.Append("^MMC");
                    individualLabel.AppendLine("^XZ");

                    txbStatus.Text = sNumWDashes + " to printer " + PrintNum.ToString() + System.Environment.NewLine + txbStatus.Text;
                    txbStatus.Refresh();
                    _PrinterConnections[PrintNum].Write(Encoding.ASCII.GetBytes(individualLabel.ToString()));
                    PrintNum++;
                    if (PrintNum >= _PrinterConnections.Count) PrintNum = 0;
                    i++;

                }
            }
        }

        private void rdoPrinter_Checked(object sender, RoutedEventArgs e)
        {
            txt610Status.Text = "";
            txt220Status.Text = "";
            txtUSBStatus.Text = "";
            foreach (Zebra.Sdk.Comm.ConnectionA Conn in _PrinterConnections) { Conn.Close(); }
            _PrinterConnections.Clear(); 
            if (rdo610.IsChecked.HasValue && rdo610.IsChecked.Equals(true))
            {
                try
                {
                    txt610Status.Text = "Trying to Connect";
                    Cursor = Cursors.Wait;
                    txt610Status.Refresh();
                    _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection("lbl-cv-174h-1.dyn.wichita.edu", 9100));
                    _PrinterConnections.LastOrDefault().Open();
                    txt610Status.Text = "Connection Open";
                }
                catch (Exception) {
                    _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); 
                    txt610Status.Text = "Not Connected";
                }
                finally {Cursor = Cursors.Arrow;}
            }
            else if (rdo220.IsChecked.HasValue && rdo220.IsChecked.Equals(true))
            {
                try
                {
                    txt220Status.Text = "Trying to Connect-A";
                    txt220Status.Refresh();
                    Cursor = Cursors.Wait;
                    _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection("lbl-cv-174h-2.dyn.wichita.edu", 9100));
                    _PrinterConnections.LastOrDefault().Open();
                    txt220Status.Text = "Printer A Connected";
                }
                catch (Exception){_PrinterConnections.Remove(_PrinterConnections.LastOrDefault());}
                finally { Cursor = Cursors.Arrow; }

                try
                {
                    txt220Status.Text = "Trying to Connect-B";
                    txt220Status.Refresh();
                    Cursor = Cursors.Wait;
                    _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection("lbl-cv-174h-3.dyn.wichita.edu", 9100));
                    _PrinterConnections.LastOrDefault().Open();
                    txt220Status.Text = "Printer B Connected";
                }
                catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
                finally { Cursor = Cursors.Arrow; }

                txt220Status.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
            }
            else if (rdoUSB.IsChecked.HasValue && rdoUSB.IsChecked.Equals(true))
            {
                foreach (Zebra.Sdk.Printer.Discovery.DiscoveredUsbPrinter Printer in Zebra.Sdk.Printer.Discovery.UsbDiscoverer.GetZebraUsbPrinters())
                {
                    try
                    {
                        txtUSBStatus.Text = "Trying to Connect";
                        txtUSBStatus.Refresh();
                        Cursor = Cursors.Wait;

                        _PrinterConnections.Add(new Zebra.Sdk.Comm.UsbConnection(Printer.Address));
                        _PrinterConnections.LastOrDefault().Open();
                        txtUSBStatus.Text = "USB Connected";
                        txtUSBStatus.Refresh();
                    }
                    catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
                    finally { Cursor = Cursors.Arrow; }
                }

                txtUSBStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
            }
            btnPrint.IsEnabled = _PrinterConnections.Count > 0;
            btnCancel.IsEnabled = btnPrint.IsEnabled;
            //_Monitor.Start(); 
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
            {

                StringBuilder individualLabel = new StringBuilder();
                individualLabel.AppendLine("^XA");
                individualLabel.AppendLine("~JA");
                individualLabel.AppendLine("^XZ");

                txbStatus.Text = "Sending Cancel" + System.Environment.NewLine + txbStatus.Text;
                txbStatus.Refresh();
                Printer.Write(Encoding.ASCII.GetBytes(individualLabel.ToString()));
            }
        }

        private void Monitor_Thread()
        {
            while (true)
            {
                int count = 0;
                foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
                {
                    if (Printer.Connected)
                    {
                        try
                        {
                            Zebra.Sdk.Printer.PrinterStatus zPrinter = Zebra.Sdk.Printer.ZebraPrinterFactory.GetInstance(Printer).GetCurrentStatus();
                            count += zPrinter.numberOfFormatsInReceiveBuffer / 2;
                        }
                        catch { }
                    }
                }
                Dispatcher.Invoke(new Action(() => this.txbQueue.Text = "Current Printer Queue: " + count.ToString()));
                Dispatcher.Invoke(new Action(() => this.txbQueue.Refresh()));
                System.Threading.Thread.Sleep(500); 
            }   
        }
    }
}
public static class ExtensionMethods
{
    private static readonly Action EmptyDelegate = delegate { };
    public static void Refresh(this UIElement uiElement)
    {
        uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
    }
}