// VERSION HISTORY
// 0.6 - 1/4/2021 - Brian Brown
//      Fixed the large / extra large barcode on the 610.
//      Fixed the CUSTomer label for the 610.
// 0.7 - 1/11/2021 - Brian Brown
//      Shrinking barcode further
// 0.8 - 1/13/2021 - Brian Brown
//      Swapping for 203 dpi 610 printer head.
// 0.9 - 2/4/2021 - Matthew Drummond
//      adding database integration

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using API_Lib.Routes;
using API_Lib.Models;
using API_Lib.Models.ProcedureModels;

namespace BarcodePrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(2);

        #region fields
        private List<Zebra.Sdk.Comm.ConnectionA> _PrinterConnections;
        private System.Threading.Thread _Monitor;

        private PrinterSettings settings;
        int selectedPrinter = -1;
        List<Client> clients;
        Client SelectedClient;
        Repository dbCommands;
        bool BothPeelsPrinting;
        
        //private Timer _StatusTimer;
        #endregion

        public MainWindow()
        {
            clients = new List<Client>();
            InitializeComponent();
            _PrinterConnections = new List<Zebra.Sdk.Comm.ConnectionA>();
            _Monitor = new System.Threading.Thread(new System.Threading.ThreadStart(Monitor_Thread));
            Title += " Version: " +  _Version;
            settings = new PrinterSettings(false);
            //read in customers and add to combobox
            
            dbCommands = new Repository();

            //GetClinics();//doesn't work from NIAR
            test();
        }

        private async void test()
        {
            //APIAccessor.SetAuth("b333m43", "pass");
            var labels = await APIAccessor.LabelAccessor.GetAllLabelsAsync();


            
        }

        #region printer connections

        private void attempt610Connection()
        {
            try
            {
                txtStatus.Text = "610 - Trying to Connect";
                Cursor = Cursors.Wait;
                txtStatus.Refresh();
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection("lbl-cv-174h-1.dyn.wichita.edu", 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "610 - Connection Open";
            }
            catch (Exception)
            {
                _PrinterConnections.Remove(_PrinterConnections.LastOrDefault());
                txtStatus.Text = "610 - Not Connected";
            }
            finally { Cursor = Cursors.Arrow; }
        }

        private void attempt220Connection()
        {
            try
            {
                txtStatus.Text = "220 - Trying to Connect-A";
                txtStatus.Refresh();
                Cursor = Cursors.Wait;
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection("lbl-cv-174h-2.dyn.wichita.edu", 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "220 - Printer A Connected";
            }
            catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
            finally { Cursor = Cursors.Arrow; }

            try
            {
                txtStatus.Text = "220 - Trying to Connect-B";
                txtStatus.Refresh();
                Cursor = Cursors.Wait;
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection("lbl-cv-174h-3.dyn.wichita.edu", 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "220 - Printer B Connected";
            }
            catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
            finally { Cursor = Cursors.Arrow; }

            txtStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
        }

        private void attemptUSBConnection()
        {
            foreach (Zebra.Sdk.Printer.Discovery.DiscoveredUsbPrinter Printer in Zebra.Sdk.Printer.Discovery.UsbDiscoverer.GetZebraUsbPrinters())
            {
                try
                {
                    txtStatus.Text = "Trying to Connect";
                    txtStatus.Refresh();
                    Cursor = Cursors.Wait;

                    _PrinterConnections.Add(new Zebra.Sdk.Comm.UsbConnection(Printer.Address));
                    _PrinterConnections.LastOrDefault().Open();
                    txtStatus.Text = "USB Connected";
                    txtStatus.Refresh();
                }
                catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
                finally { Cursor = Cursors.Arrow; }
            }
        }

        private void attemptAllConnections()
        {
            foreach (Zebra.Sdk.Comm.ConnectionA Conn in _PrinterConnections) { Conn.Close(); }
            _PrinterConnections.Clear();
            
            attempt610Connection();
            attempt220Connection();
        }

        #endregion

        private void GetClinics()
        {
            //for testing
            //clients.Add(new Client("tim", "tom"));
            //clients.Add(new Client("Jim", "tom"));
            //clients.Add(new Client("Kevin", "tom"));
            //clients.Add(new Client("Beuler", "tom"));
            //clients.Add(new Client("Gina", "tom"));
            
            //read clients from database
            clients = dbCommands.SelectAllClients();
            clients.ForEach(c => cbxClients.Items.Add(string.Format("{0} - {1}", c.Code, c.Name)));
            grdFoundclients.ItemsSource = clients;
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

        
        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            int iNumLabels = int.Parse(txtNumLabels.Text.Trim());
            //get iStartNum from api
            int iStartNum = int.Parse(txtStartNum.Text.Trim());
            //iStartNum = APIAccessor.GetLastBarcode();

            //TODO: see if the customer exists
            int iCustNum = int.Parse((cbxClients.SelectedItem as Client).Code.Substring(1));
            //if (await APIAccessor.CustomerAccessor.GetCustomerExistsAsync(SelectedClient.Code.Substring(1)))
            //    iStartNum = (await APIAccessor.BarcodeAccessor.GetLastBarcodeAsync(SelectedClient.Code)).LastNum;
            //else//get start number

            //    //    var _ = Int32.Parse(SelectedClient.Code.Substring(1));
            //    //if customer doesn't exist, ask for a start number, and send to db
            //    //otherwise get the last num from db


            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
            {
                PrintJob p = new PrintJob(Printer, settings);
                if (p.PrintMainLabel(iCustNum, out string test))//printed labels
                    txbStatus.Text = "Main Label Printed"; txbStatus.Refresh();

                
                int numLabels = Convert.ToInt32(txtNumLabels.Text);
                for(var i = 0; i < numLabels; i++)
                {
                    int barcode = iStartNum + i;
                    if (p.PrintIndividualLabels(iCustNum, barcode, (bool)ckCut.IsChecked, out string error))
                    {
                        txbStatus.Text = "Printing Label: " + barcode.ToString(); 
                        txbStatus.Refresh();
                    }
                    else
                    {
                        MessageBox.Show(error);
                        break;
                    }
                }
            }
        }

        private void rdoPrinter_Checked(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "";
            if (rdo610.IsChecked.HasValue && rdo610.IsChecked.Equals(true))
                attempt610Connection();
            else if (rdo220.IsChecked.HasValue && rdo220.IsChecked.Equals(true))
                attempt220Connection();
            //removing USB printers for now
            //else if (rdoUSB.IsChecked.HasValue && rdoUSB.IsChecked.Equals(true))
            //{
            //       attemptUSBConnection();
            //}

            txtStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
            
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

        private void UxSettings_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txb = sender as TextBox;
            int res;
            if (!Int32.TryParse(txb.Text, out res))
            {
                MessageBox.Show("Value must be an integer"); 
                return;
            }

            if (txb.Name.ToUpper().Contains("LEFT"))
                settings.IndividualLeft = res;
            else if (txb.Name.ToUpper().Contains("TOP"))
                settings.IndividualTop = res;
            else
                settings.IndividualDarkness = res;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            popSettings.IsOpen = true;
        }

        private void popClose_Click(object sender, RoutedEventArgs e)
        {
            popSettings.IsOpen = false;
        }

        private void UxSettings_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox txb = sender as TextBox;
            if (txb.Name.ToUpper().Contains("LEFT"))
                popLeft.SelectAll();
            else if (txb.Name.ToUpper().Contains("TOP"))
                popTop.SelectAll();
            else
                popDarkness.SelectAll();
        }

        private void cbxSubCustomers_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ("Customer Search...".Contains(txtClientSearch.Text)) return;


            grdFoundclients.ItemsSource = null;
            grdFoundclients.Items.Clear();
            string txt = (sender as TextBox).Text.ToUpper();
            List<Client> found = new List<Client>();
            foreach (Client c in clients)
            {
                if (c.Name.ToUpper().Contains(txt) || c.Code.ToUpper().Contains(txt))
                    found.Add(c);
            }
            grdFoundclients.ItemsSource = found;
        }

        private void PeelPrinter_Checked(object sender, RoutedEventArgs e)
        {
            BothPeelsPrinting = (bool)ckPrinterA.IsChecked && (bool)ckPrinterB.IsChecked;
        }

        private void txtClientSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            txtClientSearch.Text = "";
        }

        private void txtClientSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            txtClientSearch.Text = "Customer Search...";
        }

        private void cbxClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedClient = clients[cbxClients.SelectedIndex];
        }

        private void grdFoundclients_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            SelectedClient = grdFoundclients.SelectedItem as Client;
        }

        private void popBtnTestPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_PrinterConnections.Count == 0 || _PrinterConnections == null) 
            {
                attemptAllConnections();
                if (_PrinterConnections.Count == 0) { MessageBox.Show("No printers"); return; }
            }
            foreach (Zebra.Sdk.Comm.ConnectionA printer in _PrinterConnections)
            {
                PrintJob p = new PrintJob(printer, settings);

                p.PrintTestLabel(out string error);
                if (!string.IsNullOrEmpty(error)) MessageBox.Show("Problem printing to " + printer.SimpleConnectionName + " : " + error);
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