using System;
using System.IO;
using System.Net;
using Nancy.Hosting.Self;

namespace KibanaDotNet.KibanaHost
{
    class Program
    {
        private static readonly Uri kibanaUri = new Uri("https://download.elasticsearch.org/kibana/kibana/kibana-3.1.2.zip");

        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(Bootstrapper.ZipFilePath) || !File.Exists(Bootstrapper.ZipFilePath))
            {
                var wc = new WebClient();
                var task = wc.DownloadFileTaskAsync(kibanaUri, "kibana-3.1.2.zip.tmp");
                Console.WriteLine("Downloading Kibana from {0}...", kibanaUri);
                Console.WriteLine();
                wc.DownloadProgressChanged += (sender, eventArgs) =>
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("Downloaded {0} / {1} ({2}% Completed)", eventArgs.BytesReceived,
                        eventArgs.TotalBytesToReceive, eventArgs.ProgressPercentage);
                };
                task.Wait();
                File.Move("kibana-3.1.2.zip.tmp", "kibana-3.1.2.zip");
                Console.WriteLine();
            }
            StartKibanaHost();
        }

        private static void StartKibanaHost()
        {
            if (string.IsNullOrWhiteSpace(Bootstrapper.ZipFilePath) || !File.Exists(Bootstrapper.ZipFilePath))
            {
                Console.WriteLine("Unable to find Kibana, quitting");
                return;
            }

            var uri =
                new Uri("http://localhost:3579");

            using (
                var host =
                    new NancyHost(
                        new HostConfiguration { UrlReservations = new UrlReservations { CreateAutomatically = true } }, uri)
                )
            {
                host.Start();

                Console.WriteLine("Kibana is now available on " + uri);
                Console.WriteLine("Press any [Enter] to close the host.");
                Console.ReadLine();
            }
        }
    }
}
