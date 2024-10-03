using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer.Discovery;

namespace MauiApp_Zebra
{
    public interface IPrinterDiscovery
    {
        string BuildBluetoothConnectionChannelsString(string macAddress);

        void FindBluetoothPrinters(DiscoveryHandler handler);

        Connection GetBluetoothConnection(string macAddress);

        StatusConnection GetBluetoothStatusConnection(string macAddress);

        MultichannelConnection GetMultichannelBluetoothConnection(string macAddress);

        Connection GetUsbConnection(string symbolicName);

        void GetZebraUsbDirectPrinters(DiscoveryHandler discoveryHandler);

        List<DiscoveredPrinter> GetZebraUsbDriverPrinters();

        void CancelDiscovery();

    }
}
