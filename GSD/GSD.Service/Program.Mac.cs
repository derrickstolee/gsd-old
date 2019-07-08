using GSD.Common;
using GSD.Common.FileSystem;
using GSD.Common.Tracing;
using GSD.PlatformLoader;
using GSD.Service.Handlers;
using System;
using System.IO;
using System.Linq;

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
                CreateService(tracer, args).Run();
            }
        }

        private static GSDService CreateService(JsonTracer tracer, string[] args)
        {
            string serviceName = args.FirstOrDefault(arg => arg.StartsWith(GSDService.ServiceNameArgPrefix, StringComparison.OrdinalIgnoreCase));
            if (serviceName != null)
            {
                serviceName = serviceName.Substring(GSDService.ServiceNameArgPrefix.Length);
            }
            else
            {
                serviceName = GSDConstants.Service.ServiceName;
            }

            GSDPlatform gvfsPlatform = GSDPlatform.Instance;

            string logFilePath = Path.Combine(
                gvfsPlatform.GetDataRootForGSDComponent(serviceName),
                GSDConstants.Service.LogDirectory);
            Directory.CreateDirectory(logFilePath);

            tracer.AddLogFileEventListener(
                GSDEnlistment.GetNewGSDLogFileName(logFilePath, GSDConstants.LogFileTypes.Service),
                EventLevel.Informational,
                Keywords.Any);

            string serviceDataLocation = gvfsPlatform.GetDataRootForGSDComponent(serviceName);
            RepoRegistry repoRegistry = new RepoRegistry(
                tracer,
                new PhysicalFileSystem(),
                serviceDataLocation,
                new GSDMountProcess(tracer),
                new NotificationHandler(tracer));

            return new GSDService(tracer, serviceName, repoRegistry);
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            using (JsonTracer tracer = new JsonTracer(GSDConstants.Service.ServiceName, GSDConstants.Service.ServiceName))
            {
                tracer.RelatedError($"Unhandled exception in GSD.Service: {e.ExceptionObject.ToString()}");
            }
        }
    }
}
