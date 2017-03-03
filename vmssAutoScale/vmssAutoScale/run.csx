#r "System.Net.Http"	 
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Data"
#r "vmssAutoScale.BL.dll"
#r "vmssAutoScale.SqlLoadWatcher.dll"
#r "vmssAutoScale.ServiceBusWatcher.dll"
#r "vmssAutoScale.Interfaces.dll"

using System;
using System.Net;
using System.Diagnostics;
using vmssAutoScale.BL;
using vmssAutoScale.SqlLoadWatcher;
using vmssAutoScale.ServiceBusWatcher;

public static TraceWriter Log;
public static async void Run(TimerInfo myTimer, TraceWriter log)
{
    Log = log;
    log.Info("running");
    //SqlLoadWatcher sqlLoadWatcher = new SqlLoadWatcher();
    ServiceBusWatcher SBWatcher = new ServiceBusWatcher();
    AutoScaler autoScaler = new AutoScaler(SBWatcher);
    autoScaler.TraceEvent += AutoScaler_TraceEvent;

    await autoScaler.AutoScale();
}

private static void AutoScaler_TraceEvent(object sender, string message)
{
    Log.Info(message);
}