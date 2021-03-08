﻿// VERSION HISTORY
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        #region fields

        private string _Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private List<Zebra.Sdk.Comm.ConnectionA> _PrinterConnections;
        private List<Printer> APIPrinters;//list of printers pulled from API
        private System.Threading.Thread _Monitor;
        private PrinterSettings settings;//left, top, tear, options etc,
        List<Client> clients;//list of clients pulled from Oracle
        Client SelectedClient;//the client selected by user
        Customer selectedCustomer;//customer is the selectedClient, but stored in MDL db
        Repository dbCommands;
        int iStartNum = -1;
        //private Timer _StatusTimer;

        #endregion


        public MainWindow()
        {
            //initialize everything
            InitializeComponent();
            clients = new List<Client>();
            _PrinterConnections = new List<Zebra.Sdk.Comm.ConnectionA>();
            _Monitor = new System.Threading.Thread(new System.Threading.ThreadStart(Monitor_Thread));
            Title += " Version: " +  _Version;
            settings = new PrinterSettings("DT", PrintJob.PrintOptions.End);
            ckTear.IsChecked = true;
            dbCommands = new Repository();
            
        }

       
        /// <summary>
        /// set authentication to logged in user
        /// fill up client grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //read in customers and add to grid
            //todo:remove in prod
            APIAccessor.SetAuth("b333m439", "");
            //APIAccessor.SetAuth(Environment.UserName, "pass");
            APIPrinters = await APIAccessor.PrintersAccessor.GetAllPrintersAsync();

            Cursor = Cursors.Wait;
            //fill the grid with customers
            try
            {//from oracle
                GetClinics();//doesn't work from NIAR
            }
            catch
            {//from MDL db
                MessageBox.Show("Unable to retrieve customers from Oracle database.\nShowing previous Customers");
                GetCustomers();
            }
            Cursor = Cursors.Arrow;
        }

        #region Printers

        /// <summary>
        /// handle printer radio buttons
        /// attempt printer connections
        /// update ui
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdoPrinter_Checked(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "";
            _PrinterConnections.Clear();
            if (rdo610.IsChecked.HasValue && rdo610.IsChecked.Equals(true))
            {
                attempt610Connection();//connect to 610
                
                //enable all checkboxes
                ckCutAtEnd.IsEnabled = true;
                ckCutPerLabel.IsEnabled = true;
            }
            else if ((rdo220A.IsChecked.HasValue && rdo220A.IsChecked.Equals(true)) || (rdo220B.IsChecked.HasValue && rdo220B.IsChecked.Equals(true)))
            {
                attempt220Connection();//connect to 220
                
                //220 doesn't have cutter
                ckCutAtEnd.IsEnabled = false;
                ckCutPerLabel.IsEnabled = false;
            }
            else if (rdoUSB.IsChecked.HasValue && rdoUSB.IsChecked.Equals(true))
            {
                attemptUSBConnection();//connect to usb printers
                
                //disable cutter
                ckCutAtEnd.IsEnabled = false;
                ckCutPerLabel.IsEnabled = false;
                if ((bool)ckCutAtEnd.IsChecked || (bool)ckCutPerLabel.IsChecked)
                {
                    ckTear.IsChecked = true;
                }
            }

            //update ui
            txtStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
            btnPrint.IsEnabled = _PrinterConnections.Count > 0;
            btnCancel.IsEnabled = btnPrint.IsEnabled;
        }

        /// <summary>
        /// clear list and attempt connection to all printers
        /// </summary>
        private void attemptAllConnections()
        {
            //close connection and empty printers list
            foreach (Zebra.Sdk.Comm.ConnectionA Conn in _PrinterConnections) { Conn.Close(); }
            _PrinterConnections.Clear();
            
            //attempt connection to all printers
            attempt610Connection();
            attempt220Connection();
            attemptUSBConnection();
        }

        /// <summary>
        /// find 610 printer from MDL db
        /// use found hostname to connect to printer
        /// update printer settings
        /// </summary>
        private void attempt610Connection()
        {
            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear(); 
            Printer printer = null;

            foreach (Printer p in APIPrinters)//find 610 in printers list
            {
                if (p.ProductName.Contains("610"))
                {
                    printer = p;
                    break;
                }
            }
            try
            {//attempt connections to printer using found hostname
                txtStatus.Text = "610 - Trying to Connect";
                Cursor = Cursors.Wait;
                txtStatus.Refresh();
                _PrinterConnections.Add(new Zebra.Sdk.Comm.TcpConnection(printer.HostName, 9100));
                _PrinterConnections.LastOrDefault().Open();
                txtStatus.Text = "610 - Connection Open";
            }
            catch (Exception)
            {//remove connection from list
                _PrinterConnections.Remove(_PrinterConnections.LastOrDefault());
                txtStatus.Text = "610 - Not Connected";
            }
            finally { Cursor = Cursors.Arrow; }
            SetPrinterSettings(printer);
        }

        /// <summary>
        /// get printer information from MDL db
        /// use hostname to connect to printer
        /// update printer settings
        /// </summary>
        private void attempt220Connection()
        {
            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear();
            Printer printer = null;
            foreach(Printer p in APIPrinters)//find the one we're looking for
            {
                if (p.ProductName.Contains("220") && p.PrinterName.Contains("2"))//printer A
                {
                    printer = p;
                    break;
                }
            }
            
            try
            {//attempt printer connection
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
                if (p.ProductName.Contains("220") && p.PrinterName.Contains("3"))//printer B
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

            SetPrinterSettings(printer);//update printer settings
            txtStatus.Text = "Number Connected: " + _PrinterConnections.Count.ToString();
        }

        /// <summary>
        /// connect to all connected USB printers
        /// if it's not in the MDL db, then add it with default values
        /// otherwise update the printer settings
        /// add usb printers to grid using serial number as identifier
        /// </summary>
        private async void attemptUSBConnection()
        {
            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear();
            Printer printer = null;
            foreach (Zebra.Sdk.Printer.Discovery.DiscoveredUsbPrinter Printer in Zebra.Sdk.Printer.Discovery.UsbDiscoverer.GetZebraUsbPrinters())
            {//loop through all found usb printers
                try
                {//attempt connection
                    txtStatus.Text = "Trying to Connect";
                    txtStatus.Refresh();
                    Cursor = Cursors.Wait;
                    
                    _PrinterConnections.Add(new Zebra.Sdk.Comm.UsbConnection(Printer.Address));
                    _PrinterConnections.LastOrDefault().Open();
                    txtStatus.Text = "USB Connected";
                    txtStatus.Refresh();
                    var pj = new PrintJob(_PrinterConnections.LastOrDefault(), settings);//put in printjob, to filter out the serial number
                
                    if (!APIPrinters.Any(x => x.SerialNumber == pj.Identifier))//see if we found this printer in the db
                    {
                        //if not, add printer to db
                        await APIAccessor.PrintersAccessor.PostPrinterAsync(new Printer(0, pj.Identifier, "USB" + (APIPrinters.Count + 1).ToString(), -10, 150, 60, 1, 16, "MT", 406, 210, null, false, null, pj.Model, false));
                        APIPrinters = await APIAccessor.PrintersAccessor.GetAllPrintersAsync();//update list
                    }
                    else
                    {//otherwise
                        foreach(Printer prtr in APIPrinters)//find the printer in the db list
                        {
                            if (prtr.SerialNumber == pj.Identifier)//set printer settings
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

            //show all usb printers in a grid
            var p = new List<PrintJob>();
            foreach (var conn in _PrinterConnections)
                p.Add(new PrintJob(conn, settings));

            grdPrinter.ItemsSource = null;
            grdPrinter.Items.Clear();
            grdPrinter.ItemsSource = p;
        }
        
        #endregion

        #region Buttons

        /// <summary>
        /// when a settings button is pressed, show the settings for that printer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            
            Printer printer = null;
            APIPrinters = await APIAccessor.PrintersAccessor.GetAllPrintersAsync();

            if (btn.Name.Contains("610"))//show 610 info
            {
                foreach (var p in APIPrinters)//find 610 printer in db list
                {
                    if (p.ProductName.Contains("610"))//set the printer
                    {
                        printer = p;
                        break;
                    }
                }
                
            }
            else if (btn.Name.Contains("220A"))//show 220A setting values
            {
                foreach (var p in APIPrinters)//find 220A based on name
                {
                    if (p.PrinterName.Contains("174h-2"))
                    {
                        printer = p;
                        break;
                    }
                }
            }
            else if (btn.Name.Contains("220B"))//show 220B setting values
            {
                foreach (var p in APIPrinters)//find 22B base on name
                {
                    if (p.PrinterName.Contains("174h-3"))
                    {
                        printer = p;
                        break;
                    }
                }
            }
            else if (btn.Name.Contains("USB"))//find usb printer in db
            {
                if (!(bool)rdoUSB.IsChecked)
                {
                    rdoUSB.IsChecked = true;
                    rdoPrinter_Checked(rdoUSB, new RoutedEventArgs());
                }
                if (grdPrinter.SelectedItem == null)//if a printer isn't selected
                {
                    if (grdPrinter.Items.Count == 1)//if there's only one, then select if
                    {
                        grdPrinter.SelectedIndex = 0;
                    }
                    else//otherwise tell user to pick one
                    {
                        MessageBox.Show("no USB selected from grid");
                        return;
                    }
                }

                var sel = (PrintJob)grdPrinter.SelectedItem;
                foreach (var p in APIPrinters)
                {
                    if (p.SerialNumber == sel.Identifier)//find serial number in db printer list
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

            //show all settings
            
            popCkRotate.IsChecked = printer.Rotate90;
            popTxbSelectedPrinter.Text = printer.PrinterName;

            FlipTopAndLeft();

            popLeft.Text = printer.LeftOffset.ToString();
            popTop.Text = printer.TopOffset.ToString();
            popDarkness.Text = printer.Density.ToString();
            popTear.Text = printer.TearOffset.ToString();
            popRate.Text = printer.Rate.ToString();
            if (printer.Mode == "TT")
                rdoModeTransfer.IsChecked = true;
            else if (printer.Mode == "DT")
                rdoModeDirect.IsChecked = true;

            popSettings.IsOpen = !popSettings.IsOpen;//toggle settings popup
        }

        /// <summary>
        /// close settings when the red x is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void popClose_Click(object sender, RoutedEventArgs e)
        {
            popSettings.IsOpen = false;
        }

        /// <summary>
        /// print a test label using the current settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void popBtnTestPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_PrinterConnections.Count == 0 || _PrinterConnections == null) //if there aren't any printers
            {
                rdoPrinter_Checked(rdo610, new RoutedEventArgs());//try to connect the selected printer
                if (_PrinterConnections.Count == 0) { MessageBox.Show("No printers"); return; }//if still none, inform user
            }
            foreach (Zebra.Sdk.Comm.ConnectionA printer in _PrinterConnections)//search printers
            {
                //update settings
                settings.IndividualDarkness = int.Parse(popDarkness.Text);
                settings.IndividualLeft = int.Parse(popLeft.Text);
                settings.IndividualTop = int.Parse(popTop.Text);
                settings.PrintRate = int.Parse(popRate.Text);
                settings.TearOffset = int.Parse(popTear.Text);
                settings.Rotate = (bool)popCkRotate.IsChecked;

                if ((bool)rdoModeTransfer.IsChecked)
                    settings.Mode = "TT";
                else if ((bool)rdoModeDirect.IsChecked)
                    settings.Mode = "DT";
                
                //print test label 1234-000-00123456
                PrintJob p = new PrintJob(printer, settings);
                p.PrintTestLabel(out string error);

                //inform user of any errors
                if (!string.IsNullOrEmpty(error)) MessageBox.Show("Problem printing to " + printer.SimpleConnectionName + " : " + error);
            }
        }

        /// <summary>
        /// update settings on MDL db with new values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void popBtnSave_Click(object sender, RoutedEventArgs e)
        {
            foreach(var p in APIPrinters)//find printer in list
            {
                if (popTxbSelectedPrinter.Text == p.PrinterName)//
                {
                    //update settings
                    p.Density = int.Parse(popDarkness.Text);
                    p.LeftOffset = int.Parse(popLeft.Text);
                    p.TopOffset = int.Parse(popTop.Text);
                    p.Rate = int.Parse(popRate.Text);
                    p.TearOffset = int.Parse(popTear.Text);
                    p.Rotate90 = (bool)popCkRotate.IsChecked;

                    if ((bool)rdoModeDirect.IsChecked)
                        p.Mode = "DT";
                    else if ((bool)rdoModeTransfer.IsChecked)
                        p.Mode = "TT";

                    //update db values
                    if (await APIAccessor.PrintersAccessor.PostPrinterAsync(p))
                    {
                        txtStatus.Text = "Printer settings updated";
                    }
                    else
                    {
                        MessageBox.Show("Problem updating printer settings");
                    }
                }
            }
        }
        
        /// <summary>
        /// print labels, updating MDL db
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!rdo220A.IsChecked.HasValue && !rdo220B.IsChecked.HasValue && !rdo610.IsChecked.HasValue && !rdoUSB.IsChecked.HasValue)//make sure a printer was selected
            {
                MessageBox.Show("Must select a printer.");
                return;
            }
            if (!int.TryParse(txtNumLabels.Text.Trim(), out int iNumLabels))//make sure number of labels was valid
            {
                MessageBox.Show("Label Quantity must be a number");
                return;
            }

            int iCustNum = 0;
            if (SelectedClient == null)//make sure a client was connected
            {
                MessageBox.Show("Must select a Client");
                return;
            }
            else
            {
                iCustNum = int.Parse(SelectedClient.Code.Substring(1));//get client code without 'C'
            }

            if (!int.TryParse(txtStartingNum.Text, out iStartNum))
            {
                MessageBox.Show("Starting number is not valid");
                return;
            }
            await AddLabel();
            //if (iStartNum == -1)//couldn't find a start num
            //{
            //    return;
            //}

            //get user confirmation
            string confirm = string.Format("{0} labels will be printed for {1}. This will start at {2} and go through {3}. Is this correct?", iNumLabels, SelectedClient.Code, iStartNum, iStartNum + iNumLabels - 1);
            var res = MessageBox.Show(confirm, "Confirm Printing", MessageBoxButton.YesNo);
            if (res != MessageBoxResult.Yes) return;//if no, then cancel

            //for printing on multiple printers
            Queue<PrintJob> jobs = new Queue<PrintJob>();
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)//loop through connections
            {
                jobs.Enqueue(new PrintJob(Printer, settings));//add to queue
            }
            
            if (jobs.Peek().PrintMainLabel(iCustNum, SelectedClient.Name))//try to print main label
            {
                txtStatus.Text = "Main Label Printed"; txtStatus.Refresh();
            }
            else//inform of errors
            {
                MessageBox.Show("Problem printing main label");
                return;
            }

            rdo220A.IsEnabled = false;
            rdo220B.IsEnabled = false;
            rdo610.IsEnabled = false;
            rdoUSB.IsEnabled = false;
            btnSettings220A.IsEnabled = false;
            btnSettings220B.IsEnabled = false;
            btnSettings610.IsEnabled = false;
            btnSettingsUSB.IsEnabled = false;
            btnExit.IsEnabled = false;

            //TODO: queues need testing on location
            for (int i = 0; i < iNumLabels; i++)//loop through all labels
            {
                var p = jobs.Dequeue();//get the front job

                //printing
                string barcode = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));
                
                if (barcode.ToUpper().Contains("ALL") && i < iNumLabels)
                {//we need to add the rest of the barcodes, to make up for an error
                    var r = await APIAccessor.LabelAccessor.PostCreateLabel(new API_Lib.Models.ProcedureModels.InputModels.CreateLabelInput(SelectedClient.Code.Substring(1), SelectedClient.Name, iStartNum + i, iNumLabels - i));
                    barcode = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));//update barcode
                }

                bool printed = false;
                string error = "";
                if (!IsLastNum(barcode))//if this is NOT the last barcode
                {
                    printed = p.PrintIndividualLabels(barcode, out error);
                }
                else//it is the last barcode, tell the printer
                {
                    printed = p.PrintIndividualLabels(barcode, out error, true);
                }

                if (printed)//if printed successfully, tell user what we printed
                {
                    txtStatus.Text = "Printing Label: " + string.Format("{0:0000}-000-{1:000-000-000}", int.Parse(barcode.Substring(0, 4)), int.Parse(barcode.Substring(4)));
                    txtStatus.Refresh();
                    barcode = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1), true);
                }
                else//otherwise show the error
                {
                    MessageBox.Show(error + "\n Number of Labels updated. Press Print to reattempt");
                    txtNumLabels.Text = (iNumLabels - i).ToString();
                    break;
                }

                jobs.Enqueue(p);//put the job on the back
            }

            iStartNum = -1;//reset the start number
            SetStartNum();

            rdo220A.IsEnabled = true;
            rdo220B.IsEnabled = true;
            rdo610.IsEnabled = true;
            rdoUSB.IsEnabled = true;
            btnSettings220A.IsEnabled = true;
            btnSettings220B.IsEnabled = true;
            btnSettings610.IsEnabled = true;
            btnSettingsUSB.IsEnabled = true;
            btnExit.IsEnabled = true;
        }

        /// <summary>
        /// cancel printing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections)
            {
                StringBuilder individualLabel = new StringBuilder();
                individualLabel.AppendLine("^XA");
                individualLabel.AppendLine("~JA");
                individualLabel.AppendLine("^XZ");

                txtStatus.Text = "Sending Cancel";
                txtStatus.Refresh();
                Printer.Write(Encoding.ASCII.GetBytes(individualLabel.ToString()));
            }
        }

        /// <summary>
        /// close the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            _Monitor.Abort(); 
            foreach (Zebra.Sdk.Comm.ConnectionA Printer in _PrinterConnections) Printer.Close();
            Application.Current.Shutdown();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// get clinic from Oracle
        /// </summary>
        private void GetClinics()
        {
            //read clients from database
            clients = dbCommands.SelectAllClients();
            grdFoundclients.ItemsSource = clients;
            grdFoundclients.Refresh();
        }

        /// <summary>
        /// get customers from MDL db
        /// </summary>
        private async void GetCustomers()
        {
            foreach (Customer c in await APIAccessor.CustomerAccessor.GetAllCustomersAsync())
            {
                clients.Add(new Client(c.CustomerName, "C" + c.CustomerNumber));
            }

            if (clients.Count == 0)
            {
                MessageBox.Show("No customers found");
            }

            grdFoundclients.ItemsSource = clients;
            grdFoundclients.Refresh();
        }

        /// <summary>
        /// set printer settings
        /// </summary>
        /// <param name="p"></param>
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
            settings.Rotate = p.Rotate90;
        }

        /// <summary>
        /// change labels depending on rotate 90
        /// </summary>
        /// <param name="printer"></param>
        private void FlipTopAndLeft()
        {
            if ((bool)popCkRotate.IsChecked)//flip top and left labels
            {
                popLeftLbl.Content = "Top Offset:";
                popTopLbl.Content = "Left Offset:";
            }
            else
            {
                popLeftLbl.Content = "Left Offset:";
                popTopLbl.Content = "Top Offset:";
            }
        }

        /// <summary>
        /// see if the passed barcode is the last one
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        private bool IsLastNum(string barcode)
        {
            if (!int.TryParse(barcode.Substring(4), out int num))//parse the int
            {
                return true;//we're at the end of the printing
            }

            return num == (iStartNum + int.Parse(txtNumLabels.Text) - 1);//compare to startnum + quantity
        }
        
        /// <summary>
        /// make sure the printers in the list are still connected
        /// </summary>
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

        /// <summary>
        /// set the first barcode
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SetStartNum()
        {
            selectedCustomer = null;
            List<Customer> customers = await APIAccessor.CustomerAccessor.GetAllCustomersAsync();//get customers
            foreach (Customer c in customers)//look for customer
            {
                if (int.Parse(c.CustomerNumber) == int.Parse(SelectedClient.Code.Substring(1)))//look for customer number
                {
                    selectedCustomer = c;
                    break;
                }
            }

            if (selectedCustomer == null)//didn't find it
            {//inform user, prompt to enter starting barcode
                MessageBox.Show("Customer not listed in database:\nStarting barcode is needed be added to database.");
                txtStartingNum.Text = "0";
                txtStartingNum.Focus();
                txtStartingNum.SelectAll();
                return false;
            }
            else//we found the customer
            {
                var s = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));//check the string
                
                if (s.ToUpper().Contains("STARTNUM") || s.ToUpper().Contains("ALL"))//need to add labels to db
                {
                    int num = await APIAccessor.BarcodeAccessor.GetLastBarcodeAsync(selectedCustomer.CustomerID);
                    txtStartingNum.Text = num.ToString();
                }
                else
                {
                    int num = int.Parse(s.Substring(4));
                    txtStartingNum.Text = num.ToString();
                }

                return true;

                //if (!HaveStartNum)//need to get possible starting number from db
                //{
                //    var s = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));//check the string

                //    //s will be either "Supply Startnum" or "All Printed" when we need to create new labels
                //    if (s.ToUpper().Contains("STARTNUM") || s.ToUpper().Contains("ALL"))
                //    {
                //        if (iStartNum == -1)//if istartnum is -1
                //        {//update iStartNum to last barcode group that was set
                //            iStartNum = await APIAccessor.BarcodeAccessor.GetLastBarcodeAsync(selectedCustomer.CustomerID);
                //        }
                //        await AddLabel();//add new label
                //        s = await APIAccessor.LabelAccessor.GetPrintLabelAsync(SelectedClient.Code.Substring(1));//make sure start is valid
                //    }

                //    iStartNum = int.Parse(s.Substring(4));//parse out the barcode number
                //    txtStartingNum.Text = s.Substring(4);
                //}
                //else//user has entered a starting num
                //{
                //    iStartNum = int.Parse(txtStartingNum.Text);//use that start num
                //    txtStartingNum.Text = "";
                //}
                //return true;
            }
        }

        /// <summary>
        /// add barcode group to MDL db
        /// </summary>
        /// <returns></returns>
        private async Task AddLabel()
        {
            if (int.TryParse(txtNumLabels.Text, out int numLabels))//make sure entered quantity is valid
            {//push client code, name, starting number, and number of labels to db
                if (selectedCustomer != null)
                {
                    int lastNum = await APIAccessor.BarcodeAccessor.GetLastBarcodeAsync(selectedCustomer.CustomerID);
                    if (lastNum != iStartNum)
                    {
                        numLabels -= (lastNum - iStartNum);
                    }
                }

                var res = await APIAccessor.LabelAccessor.PostCreateLabel(new API_Lib.Models.ProcedureModels.InputModels.CreateLabelInput(SelectedClient.Code, SelectedClient.Name, iStartNum, numLabels));
            }
            else//inform user of invalid value in numlabels
            {
                MessageBox.Show("Must include label quantity");
            }
        }

        #endregion

        #region Misc Events

        /// <summary>
        /// make sure values are numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tmpBox = sender as TextBox;
            
            if (!int.TryParse(tmpBox.Text, out int txtNumber))
            {
                MessageBox.Show("Value must be an integer!");
            }
        }

        /// <summary>
        /// for settings, make sure values are numbers and copy values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UxSettings_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txb = sender as TextBox;
            int res;
            if (!Int32.TryParse(txb.Text, out res))
            {
                MessageBox.Show("Value must be an integer");
                txb.Text = "0";
                return;
            }

            //copy value to correct setting
            if (txb.Name.ToUpper().Contains("LEFT"))
            {
                if (res < 0 || res > 406)
                {
                    MessageBox.Show("Left offset must be between 0 and 406");

                    return;
                }
                settings.IndividualLeft = res;
            }
            else if (txb.Name.ToUpper().Contains("TOP"))
            {
                if (res < 0 || res > 210)
                {
                    MessageBox.Show("Top offset must be between 0 and 203");
                    return;
                }
                settings.IndividualTop = res;
            }
            else if (txb.Name.ToUpper().Contains("TEAR"))
            {
                if (res < -120 || res > 120)
                {
                    MessageBox.Show("Tear offset must be between -120 and 120");
                    return;
                }
                settings.TearOffset = res;
            }
            else if (txb.Name.ToUpper().Contains("RATE"))
            {
                if (res < 1 || res > 14)
                {
                    MessageBox.Show("Print Rate must be between 1 and 14");
                    return;
                }
                settings.PrintRate = res;
            }
            else if (txb.Name.ToUpper().Contains("DARKNESS"))
            {
                if (res < 0 || res > 30)
                {
                    MessageBox.Show("Darkness must be between 0 and 30");
                    return;
                }
                settings.IndividualDarkness = res;
            }
            
        }

        /// <summary>
        /// empty textbox when selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UxSettings_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox txb = sender as TextBox;
            txb.Text = "";
        }

        /// <summary>
        /// when searching for a particular customer, fill grid with all client with that name/code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// updated the selected client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void grdFoundclients_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            SelectedClient = grdFoundclients.SelectedItem as Client;
            //set start num
            await SetStartNum();
        }
        
        /// <summary>
        /// set the appropriate print setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ckOptions_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as RadioButton;
            if (box.Name.ToUpper().Contains("CUTPERLABEL"))
            {
                settings.options = PrintJob.PrintOptions.Label;
            }
            else if (box.Name.ToUpper().Contains("CUTATEND"))
            {
                settings.options = PrintJob.PrintOptions.End;
            }
            else if (box.Name.ToUpper().Contains("PEEL"))
            {
                settings.options = PrintJob.PrintOptions.Peel;
            }
            else
            {
                settings.options = PrintJob.PrintOptions.Tear;
            }
        }

        /// <summary>
        /// set rotation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void popCkRotate_Checked(object sender, RoutedEventArgs e)
        {
            settings.Rotate = (bool)popCkRotate.IsChecked;
            FlipTopAndLeft();
        }

        #endregion

        private void popRdoMode_Checked(object sender, RoutedEventArgs e)
        {
            var btn = sender as RadioButton;
            if (btn.Name.ToUpper().Contains("TRANSFER"))
                settings.Mode = "TT";
            else if (btn.Name.ToUpper().Contains("DIRECT"))
                settings.Mode = "DT";
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