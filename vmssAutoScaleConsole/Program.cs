using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vmssAutoScale.BL;
using vmssAutoScale.SqlLoadWatcher;
using vmssAutoScale.ServiceBusWatcher;

namespace vmssAutoScaleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.WriteLine("****************************");
            Trace.WriteLine("Welcom To VMSS Auto Scaleset");
            Trace.WriteLine("****************************");
            Trace.WriteLine("\n\n");

            while (!Console.KeyAvailable)
            {
                /*
                Console.WriteLine("Please select the Trriger interfacet:");
                Console.WriteLine("1. SQL");
                Console.WriteLine("2. Service Bus");

                string strOption = Console.ReadLine();

                switch (strOption)
                {
                    case "1":
                        {
                            SQLLoader();
                        }
                        break;
                    case "2":
                        {*/
                            ServiceBusLoader();
                       /* }
                        break;
                    default:
                        {

                        }
                        break;
                }              */  
            }
        }

        private static void SQLLoader()
        {
            Console.WriteLine("SQLLoader");

            SqlLoadWatcher sqlLoadWatcher = new SqlLoadWatcher();
            AutoScaler autoScaler = new AutoScaler(sqlLoadWatcher);

            autoScaler.TraceEvent += AutoScaler_TraceEvent;
            Task t = autoScaler.AutoScale();

            t.Wait();
            Trace.WriteLine("Pausing for one minute");
            Task.Delay(60000).Wait();
        }

        private static void ServiceBusLoader()
        {
            Console.WriteLine("ServiceBusLoader");

            ServiceBusWatcher MyServiceBusWatcher = new ServiceBusWatcher();          
            AutoScaler autoScaler = new AutoScaler(MyServiceBusWatcher);
            
            autoScaler.TraceEvent += AutoScaler_TraceEvent;
            Task t = autoScaler.AutoScale();

            t.Wait();
            Trace.WriteLine("Pausing for one minute");
            Task.Delay(60000).Wait();
        }
        

        private static void AutoScaler_TraceEvent(object sender, string message)
        {
            Console.WriteLine(message);
        }
    }
}
