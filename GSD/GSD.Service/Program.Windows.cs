using GSD.Common;
using GSD.Common.Tracing;
using GSD.PlatformLoader;
using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace GSD.Service
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            GSDPlatformLoader.Initialize();

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            using (JsonTracer tracer = new JsonTracer(GSDConstants.Service.ServiceName, GSDConstants.Service.ServiceName))
            {
                using (GSDService service = new GSDService(tracer))
                {
                    // This will fail with a popup from a command prompt. To install as a service, run:
                    // %windir%\Microsoft.NET\Framework64\v4.0.30319\installutil GSD.Service.exe
                    ServiceBase.Run(service);
                }
            }
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";
                eventLog.WriteEntry(
                    "Unhandled exception in GSD.Service: " + e.ExceptionObject.ToString(),
                    EventLogEntryType.Error);
            }
        }
    }
}