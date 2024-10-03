using Android.Util;
using System.Collections.ObjectModel;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;

namespace MauiApp_Zebra
{
    public partial class MainPage : ContentPage
    {
        public string selectedLabelPrinter { get; set; }
        PermissionStatus status = PermissionStatus.Unknown;
        protected DiscoveredPrinter myPrinter;
        public ObservableCollection<DiscoveredPrinter> printerList = new ObservableCollection<DiscoveredPrinter>();

        public MainPage()
        {
            InitializeComponent();
            CounterBtn.IsEnabled = false;
        }

        public async Task StartZebraPrinterDiscovery()
        {

            new Task(new Action(async () => {

                await StartDiscovery("Bluetooth");
            })).Start();
        }
        private async Task StartDiscovery(string connectionType)
        {

            await RequestBluetooth();
            if (status == PermissionStatus.Granted)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Starting {connectionType.ToString()} label printer discovery...");

                    switch (connectionType)
                    {
                        case "Bluetooth":
                            DependencyService.Register<IPrinterDiscovery, PrinterDiscoveryImplementation>();
                            DependencyService.Get<IPrinterDiscovery>().FindBluetoothPrinters(new DiscoveryHandlerImplementation(this, "Bluetooth"));
                            break;
                    }
                }
                catch (Exception e)
                {
                    string errorMessage = $"Error discovering {connectionType.ToString()} printers: {e.Message}";
                    System.Diagnostics.Debug.WriteLine(errorMessage);
                    await DisplayAlert("Error", errorMessage.ToString(), "Ok");
                }
            }
        }

        public async Task RequestBluetooth()
        {
            if (DeviceInfo.Platform != DevicePlatform.Android)
                return;

            if (DeviceInfo.Version.Major >= 12)
            {
                status = await Permissions.CheckStatusAsync<MyBluetoothPermission>();

                if (status == PermissionStatus.Granted)
                    return;

                if (Permissions.ShouldShowRationale<MyBluetoothPermission>())
                {
                    await DisplayAlert("Needs permissions", "BECAUSE!!!", "OK");
                }
                Dispatcher.Dispatch(async () =>
                {
                    status = await Permissions.RequestAsync<MyBluetoothPermission>();
                });
            }
            else
            {
                status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                    return;

                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    await DisplayAlert("Needs permissions", "BECAUSE!!!", "OK");
                }
                Dispatcher.Dispatch(async () =>
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                });

            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            DependencyService.Register<IPrinterDiscovery, PrinterDiscoveryImplementation>();
            DependencyService.Get<IPrinterDiscovery>().CancelDiscovery();

            new Task(new Action(async () =>
            {
                await PrintLineMode();
            })).Start();
        }

        protected static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            bytes = Encoding.UTF8.GetBytes(str);
            return bytes;
        }
        protected bool CheckPrinterLanguage(Connection connection)
        {
            if (!connection.Connected)
                connection.Open();

            //  Check the current printer language
            byte[] response = connection.SendAndWaitForResponse(GetBytes("! U1 getvar \"device.languages\"\r\n"), 500, 100, "\0");
            string language = Encoding.UTF8.GetString(response, 0, response.Length);
            if (language.Contains("line_print"))
            {
                ShowErrorAlert("Switching printer to ZPL Control Language.");
            }
            // printer is already in zpl mode
            else if (language.Contains("zpl"))
            {
                return true;
            }

            //  Set the printer command languege
            connection.Write(GetBytes("! U1 setvar \"device.languages\" \"zpl\"\r\n"));
            response = connection.SendAndWaitForResponse(GetBytes("! U1 getvar \"device.languages\"\r\n"), 500, 100, "\0");
            language = Encoding.UTF8.GetString(response, 0, response.Length);
            if (!language.Contains("zpl"))
            {
                ShowErrorAlert("Printer language not set. Not a ZPL printer.");
                return false;
            }
            return true;
        }

        protected bool PreCheckPrinterStatus(Zebra.Sdk.Printer.ZebraPrinter printer)
        {
            // Check the printer status
            PrinterStatus status = printer.GetCurrentStatus();
            if (!status.isReadyToPrint)
            {
                ShowErrorAlert("Unable to print. Printer is " + status);
                return false;
            }
            return true;
        }
        public async Task PrintLineMode()
        {
            Connection connection = null;
            try
            {

                connection = this.myPrinter.GetConnection();
                connection.Open();
                Zebra.Sdk.Printer.ZebraPrinter printer = ZebraPrinterFactory.GetInstance(connection);
                if ((!CheckPrinterLanguage(connection)) || (!PreCheckPrinterStatus(printer)))
                {
                    return;
                }
                PrintData(connection);

                if (PostPrintCheckStatus(printer))
                {

                }
                Dispatcher.Dispatch(() =>
                {
                    Navigation.PopAsync();
                });
            }
            catch (Exception ex)
            {
                // Connection Exceptions and issues are caught here
                await DisplayAlert("Error", ex.Message.ToString(), "Ok");
            }
            finally
            {
                if ((connection != null) && (connection.Connected))
                    connection.Close();
            }
        }

        private void PrintData(Connection printerConnection)
        {
            try
            {
                StringBuilder pageData = new StringBuilder();


                string bodyData = string.Format("^XA\r\n\r\n\r\n^FXfield for the element 'Sample Text Element'\r\n^FO200,320,2\r\n^FWN\r\n^A40,40^FDPrinted Successfully^FS\r\n^XZ");

                pageData.Append(bodyData);
                printerConnection.Write(GetBytes(pageData.ToString()));
            }
            catch (Exception)
            {
                //throw 
            }
        }

        protected bool PostPrintCheckStatus(Zebra.Sdk.Printer.ZebraPrinter printer)
        {
            // Check the status again to verify print happened successfully
            try
            {


                PrinterStatus status = printer.GetCurrentStatus();

                // Wait while the printer is printing
                while ((status.numberOfFormatsInReceiveBuffer > 0) && (status.isReadyToPrint))
                {
                    status = printer.GetCurrentStatus();
                }

                // verify the print didn't have errors like running out of paper
                if (!status.isReadyToPrint)
                {
                    ShowErrorAlert("Error during print. Printer is " + status);
                    return false;
                }

                return true;
            }
            catch (ConnectionException ex)
            {
                return false;
            }
        }
        private void ShowErrorAlert(string message)
        {
            Dispatcher.Dispatch(() => {
                DisplayAlert("Error", message, "OK");
            });
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Dispatch(() => {
                statusLbl.Text = message;
            });
        }
        private class DiscoveryHandlerImplementation : DiscoveryHandler
        {

            private MainPage selectPrinterPage { get; set; }
            private string connectionType { get; set; }

            public DiscoveryHandlerImplementation(MainPage selectPrinterPage, string connectionType)
            {
                this.selectPrinterPage = selectPrinterPage;
                this.connectionType = connectionType;
            }

            public async void DiscoveryError(string message)
            {
                selectPrinterPage.ShowErrorAlert(message);

                selectPrinterPage.UpdateStatus("Zebra discovery error..Pull to Refresh ");
                if (selectPrinterPage.printerList != null)
                {
                    selectPrinterPage.UpdateStatus("No printer selected");
                }
                else
                {
                    selectPrinterPage.UpdateStatus("No printers available");
                }

            }

            public void DiscoveryFinished()
            {
                selectPrinterPage.UpdateStatus("Discovery finished..");

            }

            public void FoundPrinter(DiscoveredPrinter discoveredPrinter)
            {
                Device.BeginInvokeOnMainThread(() => {
                    selectPrinterPage.printerLv.BatchBegin();

                    if (!selectPrinterPage.printerList.Contains(discoveredPrinter))
                    {
                        selectPrinterPage.printerList.Add(discoveredPrinter);

                    }
                    //if (selectPrinterPage.printerList != null)
                    //{
                    //    foreach (var printer in selectPrinterPage.printerList)
                    //    {
                    //        selectPrinterPage.myPrinter = printer;
                    //    }
                    //}
                    selectPrinterPage.printerLv.BatchCommit();
                });
            }

        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await StartZebraPrinterDiscovery();
            printerLv.ItemsSource = printerList;
        }


        private async void RefreshView_Refreshing(object sender, EventArgs e)
        {
            DependencyService.Register<IPrinterDiscovery, PrinterDiscoveryImplementation>();
            DependencyService.Get<IPrinterDiscovery>().CancelDiscovery();

            await StartZebraPrinterDiscovery();
        }

        private void printerLv_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            //selectedLabelPrinter = e?.Item.ToString();

            myPrinter = (DiscoveredPrinter)e.Item;
            CounterBtn.IsEnabled = true;
        }
    }

}
