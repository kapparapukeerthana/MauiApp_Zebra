using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer.Discovery;

namespace MauiApp_Zebra
{
    public class PrinterDiscoveryImplementation : IPrinterDiscovery
    {
        public void CancelDiscovery()
        {
        }
        public string BuildBluetoothConnectionChannelsString(string macAddress)
        {
            BluetoothConnection connection = new BluetoothConnection(macAddress);
            connection.Open(); // Check connection

            try
            {
                ServiceDiscoveryHandlerImplementation serviceDiscoveryHandler = new ServiceDiscoveryHandlerImplementation();
                BluetoothDiscoverer.FindServices(Platform.AppContext, macAddress, serviceDiscoveryHandler);

                while (!serviceDiscoveryHandler.Finished)
                {
                    Task.Delay(100);
                }

                StringBuilder sb = new StringBuilder();
                foreach (ConnectionChannel connectionChannel in serviceDiscoveryHandler.ConnectionChannels)
                {
                    sb.AppendLine(connectionChannel.ToString());
                }
                return sb.ToString();
            }
            finally
            {
                try
                {
                    connection?.Close();
                }
                catch (ConnectionException) { }
            }
        }

        public void FindBluetoothPrinters(DiscoveryHandler handler)
        {
            BluetoothDiscoverer.FindPrinters(Platform.AppContext, handler);
        }

        public Connection GetBluetoothConnection(string macAddress)
        {
            return new BluetoothConnection(macAddress);
        }

        public StatusConnection GetBluetoothStatusConnection(string macAddress)
        {
            return new BluetoothStatusConnection(macAddress);
        }

        public MultichannelConnection GetMultichannelBluetoothConnection(string macAddress)
        {
            return new MultichannelBluetoothConnection(macAddress);
        }

        public Connection GetUsbConnection(string symbolicName)
        {
            throw new NotImplementedException();
        }

        public void GetZebraUsbDirectPrinters(DiscoveryHandler discoveryHandler)
        {
            throw new NotImplementedException();
        }

        public List<DiscoveredPrinter> GetZebraUsbDriverPrinters()
        {
            throw new NotImplementedException();
        }

        private class ServiceDiscoveryHandlerImplementation : ServiceDiscoveryHandler
        {

            public List<ConnectionChannel> ConnectionChannels { get; private set; }

            public bool Finished { get; private set; }

            public ServiceDiscoveryHandlerImplementation()
            {
                ConnectionChannels = new List<ConnectionChannel>();
            }

            public void DiscoveryFinished()
            {
                Finished = true;
            }

            public void FoundService(ConnectionChannel channel)
            {
                ConnectionChannels.Add(channel);
            }
        }

    }
}
