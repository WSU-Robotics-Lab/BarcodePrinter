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

using API_Lib.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private string _Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(1);

        
        #region fields

        private List<Zebra.Sdk.Comm.ConnectionA> _PrinterConnections;
        private List<Printer> APIPrinters;
        private System.Threading.Thread _Monitor;
        private PrinterSettings settings;
        List<Client> clients;
        Client SelectedClient;
        Customer selectedCustomer;
        Repository dbCommands;
        int iStartNum = -1;
        //private Timer _StatusTimer;

        #endregion


        public MainWindow()
        {
            InitializeComponent();
            clients = new List<Client>();
            _PrinterConnections = new List<Zebra.Sdk.Comm.ConnectionA>();
            _Monitor = new System.Threading.Thread(new System.Threading.ThreadStart(Monitor_Thread));
            Title += " Version: " +  _Version;
            settings = new PrinterSettings(PrintJob.PrintOptions.End);

            dbCommands = new Repository();

            //read in customers and add to grid
            APIAccessor.SetAuth(Environment.UserName, "pass");

            //TODO: remove after debugging
            test();
            //TODO: remove after debugging
            try
            {   
                GetClinics();//doesn't work from NIAR
            }
            catch
            {
                GetCustomers();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
           APIPrinters = await APIAccessor.PrintersAccessor.GetAllPrintersAsync();
        }

        private async void test()
        {
            //set user authorization

            //TODO: remove after debugging
            APIAccessor.SetAuth("b333m439", "pass");
            //TODO: remove after debugging

                    
            var p = new List<PrintJob>(); 
        }

        #region printer connections

        private void attempt610Connection()
        {
            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear(); 
            Printer printer = null;

            foreach (Printer p in APIPrinters)
            {
                if (p.ProductName.Contains("610"))
                {
                    printer = p;
                    break;
                }
            }
            try
            {
                txtStatus.Text = "610 - Trying to Connect";
                Cursor = Cursors.Wait;
                txtStatus.Refresh();
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection(printer.HostName, 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "610 - Connection Open";
            }
            catch (Exception)
            {
                _PrinterConnections.Remove(_PrinterConnections.LastOrDefault());
                txtStatus.Text = "610 - Not Connected";
            }
            finally { Cursor = Cursors.Arrow; }
            SetPrinterSettings(printer);

        }

        private void attempt220Connection()
        {
            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear();
            Printer printer = null;
            foreach(Printer p in APIPrinters)
            {
                if (p.ProductName.Contains("220") && p.PrinterName.Contains("2"))
                {
                    printer = p;
                    break;
                }
            }
            
            try
            {
                txtStatus.Text = "220 - Trying to Connect-A";
                txtStatus.Refresh();
                Cursor = Cursors.Wait;
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection(printer.HostName, 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "220 - Printer A Connected";
            }
            catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
            finally { Cursor = Cursors.Arrow; }

            foreach (Printer p in APIPrinters)
            {
                if (p.ProductName.Contains("220") && p.PrinterName.Contains("3"))
                {
                    printer = p;
                    break;
                }
            }

            try
            {
                txtStatus.Text = "220 - Trying to Connect-B";
                txtStatus.Refresh();
                Cursor = Cursors.Wait;
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection(printer.HostName, 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "220 - Printer B Connected";
            }
            catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
            finally { Cursor = Cursors.Arrow; }

            SetPrinterSettings(printer);
            txtStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
        }

        private async void attemptUSBConnection()
        {
            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear();
            Printer printer = null;
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
                    var pj = new PrintJob(_PrinterConnections.LastOrDefault(), settings);
                
                    if (!APIPrinters.Any(x => x.SerialNumber == pj.Identifier))
                    {
                        await APIAccessor.PrintersAccessor.PostPrinterAsync(new API_Lib.Models.Printer(0, pj.Identifier, "USB" + (APIPrinters.Count + 1).ToString(), -10, 150, 60, 1, 16, "MT", 406, 210, null, false, null, pj.Model));
                        APIPrinters = await APIAccessor.PrintersAccessor.GetAllPrintersAsync();//update list
                    }
                    else
                    {
                        foreach(Printer prtr in APIPrinters)
                        {
                            if (prtr.SerialNumber == pj.Identifier)
                            {
                                printer = prtr;
                                SetPrinterSettings(prtr);
                                break;
                            }
                        }
                    }
                }
                catch (Exception) { _PrinterConnections.Remove(_PrinterConnections.LastOrDefault()); }
                finally { Cursor = Cursors.Arrow; }
            }

            var p = new List<PrintJob>();
            foreach (var conn in _PrinterConnections)
                p.Add(new PrintJob(conn, settings));

            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear();
            grdPrinter.ItemsSource = p;
        }

        private void SetPrinterSettings(Printer p)
        {
            settings.IndividualLeft = (int)p.LeftOffset;
            settings.IndividualTop = (int)p.TopOffset;
            settings.IndividualDarkness = (int)p.Density;
            settings.MainDarkness = (int)p.Density;
            settings.MainLeft = (int)p.LeftOffset;
            settings.MainTop = (int)p.TopOffset;
            settings.PrintRate = (int)p.Rate;
            settings.TearOffset = (int)p.TearOffset;
        }
        private void attemptAllConnections()
        {
            foreach (Zebra.Sdk.Comm.ConnectionA Conn in _PrinterConnections) { Conn.Close(); }
            _PrinterConnections.Clear();
            
            attempt610Connection();
            attempt220Connection();
            attemptUSBConnection();
        }

        #endregion

        private void GetClinics()
        {
            //read clients from database
            clients = dbCommands.SelectAllClients();
            grdFoundclients.ItemsSource = clients;
            grdFoundclients.Refresh();
        }
        private async void GetCustomers()
        {
            foreach (Customer c in await APIAccessor.CustomerAccessor.GetAllCustomersAsync())
            {
                clients.Add(new Client("No connection", "C" + c.CustomerNumber));
            }

            if (clients.Count == 0)
            {
                MessageBox.Show("No customers found");
            }

            grdFoundclients.ItemsSource = clients;
            grdFoundclients.Refresh();
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
            
            if (!int.TryParse(tmpBox.Text, out int txtNumber))
            {
                MessageBox.Show("Value must be an integer!");
            }
        }

        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!rdo220A.IsChecked.HasValue && !rdo220B.IsChecked.HasValue && !rdo610.IsChecked.HasValue && !rdoUSB.IsChecked.HasValue)
            {
                MessageBox.Show("Must select a printer.");
                return;
            }
            if (!int.TryParse(txtNumLabels.Text.Trim(), out int iNumLabels))
            {
                MessageBox.Show("Label Quantity must be a number");
                return;
            }

            int iCustNum = 0;
            if (SelectedClient == null)
            {
                MessageBox.Show("Must select a Client");
                return;
            }
            else
            {
                iCustNum = int.Parse(SelectedClient.Code.Substring(1));
            }
            if (!await SetStartNum())
            {
                AddLabel();
            }
            Thread.Sleep(500);

            var start = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));
            iStartNum = int.Parse(start.Substring(4));

            string confirm = string.Format("{0} labels will be printed for {1}. This will start at {2} and go to {3}. Is this correct?", iNumLabels, SelectedClient.Code, iStartNum, iStartNum + iNumLabels - 1);
            var res = MessageBox.Show(confirm, "Confirm Printing", MessageBoxButton.YesNo);
            if (res != MessageBoxResult.Yes) return;

            Queue<PrintJob> jobs = new Queue<PrintJob>();
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
            {
                jobs.Enqueue(new PrintJob(Printer, settings));
            }
                        
            if (jobs.Peek().PrintMainLabel(iCustNum))
            {
                txtStatus.Text = "Main Label Printed"; txtStatus.Refresh();
            }
            
            //TODO: queues need testing on location
            for (int i = 0; i < iNumLabels; i++)
            {
                var p = jobs.Dequeue();

                //printing
                string barcode = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1), true);
                if (p.PrintIndividualLabels(barcode, out string error))
                {
                    txtStatus.Text = "Printing Label: " + barcode.ToString();
                    txtStatus.Refresh();
                }
                else
                {
                    MessageBox.Show(error);
                    break;
                }

                jobs.Enqueue(p);
            }
        }

        private void rdoPrinter_Checked(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "";
            _PrinterConnections.Clear();
            if (rdo610.IsChecked.HasValue && rdo610.IsChecked.Equals(true))
            {
                attempt610Connection();
                
                //enable all checkboxes
                ckCutAtEnd.IsEnabled = true;
                ckCutPerLabel.IsEnabled = true;
            }
            else if ((rdo220A.IsChecked.HasValue && rdo220A.IsChecked.Equals(true)) || (rdo220B.IsChecked.HasValue && rdo220B.IsChecked.Equals(true)))
            {
                attempt220Connection();
                
                //enable all checkboxes
                ckCutAtEnd.IsEnabled = true;
                ckCutPerLabel.IsEnabled = true;
            }
            else if (rdoUSB.IsChecked.HasValue && rdoUSB.IsChecked.Equals(true))
            {
                attemptUSBConnection();
                
                //disable cutter
                ckCutAtEnd.IsEnabled = false;
                ckCutPerLabel.IsEnabled = false;

            }

            txtStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
            


            btnPrint.IsEnabled = _PrinterConnections.Count > 0;
            btnCancel.IsEnabled = btnPrint.IsEnabled;
        }

        private void UxSettings_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txb = sender as TextBox;
            int res;
            if (!Int32.TryParse(txb.Text, out res))
            {
                MessageBox.Show("Value must be an integer");
                txb.Text = "5";
                return;
            }

            if (txb.Name.ToUpper().Contains("LEFT"))
                settings.IndividualLeft = res;
            else if (txb.Name.ToUpper().Contains("TOP"))
                settings.IndividualTop = res;
            else
                settings.IndividualDarkness = res;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
            {

                StringBuilder individualLabel = new StringBuilder();
                individualLabel.AppendLine("^XA");
                individualLabel.AppendLine("~JA");
                individualLabel.AppendLine("^XZ");

                txtStatus.Text = "Sending Cancel" + System.Environment.NewLine + txtStatus.Text;
                txtStatus.Refresh();
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

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            
            Printer printer = null;

            if (btn.Name.Contains("610"))
            {
                foreach (var p in APIPrinters)
                {
                    if (p.ProductName.Contains("610"))
                    {
                        printer = p;
                        break;
                    }
                }
                
            }
            else if (btn.Name.Contains("220A"))
            {
                foreach (var p in APIPrinters)
                {
                    if (p.PrinterName.Contains("174h-2"))
                    {
                        printer = p;
                        break;
                    }
                }
            }
            else if (btn.Name.Contains("220B"))
            {
                foreach (var p in APIPrinters)
                {
                    if (p.PrinterName.Contains("174h-3"))
                    {
                        printer = p;
                        break;
                    }
                }
            }
            else if (btn.Name.Contains("USB"))
            {
                if (grdPrinter.SelectedItem == null)
                {
                    if (grdPrinter.Items.Count == 1)
                    {
                        grdPrinter.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show("no USB selected from grid");
                        return;
                    }
                }

                var sel = (PrintJob)grdPrinter.SelectedItem;
                foreach (var p in APIPrinters)
                {
                    if (p.SerialNumber == sel.Identifier)
                    {
                        printer = p;
                        break;
                    }
                }
            }

            if (printer == null)
            {
                MessageBox.Show("Printer not found in database");
                return;
            }

            popTxbSelectedPrinter.Text = printer.PrinterName;
            popLeft.Text = printer.LeftOffset.ToString();
            popTop.Text = printer.TopOffset.ToString();
            popDarkness.Text = printer.Density.ToString();
            popTear.Text = printer.TearOffset.ToString();
            popRate.Text = printer.Rate.ToString();
            popSettings.IsOpen = !popSettings.IsOpen;//toggle
            
        }

        private void popClose_Click(object sender, RoutedEventArgs e)
        {
            popSettings.IsOpen = false;
        }

        private void UxSettings_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox txb = sender as TextBox;
            txb.Text = "";
        }

        private void txtClientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
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

        private void grdFoundclients_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            SelectedClient = grdFoundclients.SelectedItem as Client;
        }

        private async Task<bool> SetStartNum()
        {
            selectedCustomer = null;
            List<Customer> customers = await APIAccessor.CustomerAccessor.GetAllCustomersAsync();
            foreach (Customer c in customers)//look for customer
            {
                if (int.Parse(c.CustomerNumber) == int.Parse(SelectedClient.Code.Substring(1)))
                {
                    selectedCustomer = c;
                    break;
                }
            }

            if (selectedCustomer == null)//didn't find it
            {
                MessageBox.Show("Customer not listed in database:\nSupply starting barcode and the customer will be added to database.");
                popStart.IsOpen = true;
                return false;
            }
            else
            {
                if (string.IsNullOrEmpty(popTxtStartNum.Text))
                {
                    var s = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));
                    if (s.ToUpper().Contains("STARTNUM") || s.ToUpper().Contains("ALL"))
                    {
                        return false;
                    }
                    
                    iStartNum = int.Parse(s.Substring(4));
                }

                else
                    iStartNum = int.Parse(popTxtStartNum.Text);
                return true;
            }
        }

        private void popBtnTestPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_PrinterConnections.Count == 0 || _PrinterConnections == null) 
            {
                rdoPrinter_Checked(rdo610, new RoutedEventArgs());
                if (_PrinterConnections.Count == 0) { MessageBox.Show("No printers"); return; }
            }
            foreach (Zebra.Sdk.Comm.ConnectionA printer in _PrinterConnections)
            {
                settings.IndividualDarkness = int.Parse(popDarkness.Text);
                settings.IndividualLeft = int.Parse(popLeft.Text);
                settings.IndividualTop = int.Parse(popTop.Text);
                settings.PrintRate = int.Parse(popRate.Text);
                settings.TearOffset = int.Parse(popTear.Text);
                
                PrintJob p = new PrintJob(printer, settings);
                p.PrintTestLabel(out string error);
                if (!string.IsNullOrEmpty(error)) MessageBox.Show("Problem printing to " + printer.SimpleConnectionName + " : " + error);
            }
        }

        private void popBtnStartNum_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(popTxtStartNum.Text, out iStartNum) && iStartNum > -1)
            {
                popStart.IsOpen = false;
                AddLabel();
            }
            else
            {
                MessageBox.Show("Start num must be an integer with value 0 or more");
            }
        }

        private async void AddLabel()
        {
            if (int.TryParse(txtNumLabels.Text, out int numLabels))
            {
                var res = await APIAccessor.LabelAccessor.PostCreateLabel(new API_Lib.Models.ProcedureModels.InputModels.CreateLabelInput(SelectedClient.Code, SelectedClient.Name, iStartNum, numLabels));
                
            }
            else
            {
                MessageBox.Show("Must include label quantity");
            }
        }

        private void ckOptions_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (box.Name.ToUpper().Contains("CUTPERLABEL"))
            {
                settings.options = PrintJob.PrintOptions.Label;
            }
            else if (box.Name.ToUpper().Contains("CUTATEND"))
            {
                settings.options = PrintJob.PrintOptions.End;
            }
            else
            {
                settings.options = PrintJob.PrintOptions.Peel;
            }
        }

        private async void popBtnSave_Click(object sender, RoutedEventArgs e)
        {
            foreach(var p in APIPrinters)
            {
                if (popTxbSelectedPrinter.Text == p.PrinterName)
                {
                    p.Density = int.Parse(popDarkness.Text);
                    p.LeftOffset = int.Parse(popLeft.Text);
                    p.TopOffset = int.Parse(popTop.Text);
                    p.Rate = int.Parse(popRate.Text);
                    p.TearOffset = int.Parse(popTear.Text);
                    if (await APIAccessor.PrintersAccessor.PostPrinterAsync(p))
                    {
                        txtStatus.Text = "Printer settings updated";
                    }
                    else
                    {
                        MessageBox.Show("Problem updating database");
                    }
                }
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