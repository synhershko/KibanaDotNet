using System;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Nancy.Hosting.Self;

namespace KibanaDotNet.KibanaHost
{
    class Program
    {
        private const string kibanaFilename = "kibana-4.0.0-beta3";
        private const string kibanaUri = "https://download.elasticsearch.org/kibana/kibana/{0}.zip";

        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(Bootstrapper.ZipFilePath) || !File.Exists(Bootstrapper.ZipFilePath))
            {
                var url = String.Format(kibanaUri, kibanaFilename);
                var wc = new WebClient();
                var task = wc.DownloadFileTaskAsync(url, kibanaFilename + ".zip.tmp");
                Console.WriteLine("Downloading Kibana from {0}...", url);
                Console.WriteLine();
                wc.DownloadProgressChanged += (sender, eventArgs) =>
                {
                    Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
                    Console.WriteLine("Downloaded {0} / {1} ({2}% Completed)", eventArgs.BytesReceived,
                        eventArgs.TotalBytesToReceive, eventArgs.ProgressPercentage);
                };
                task.Wait();

                // Extract the kibana jar containing all the statics
                using (var fs = new FileStream(kibanaFilename + ".zip.tmp", FileMode.Open, FileAccess.Read))
                using (var zf = new ZipFile(fs))
                {
                    var ze = zf.GetEntry(kibanaFilename + @"/lib/kibana.jar");
                    if (ze == null)
                    {
                        throw new ArgumentException(kibanaFilename + @"\lib\kibana.jar wasn't found");
                    }

                    using (var s = zf.GetInputStream(ze))
                    {
                        using (var streamWriter = File.Create("kibana-statics.zip"))
                        {
                            var buffer = new byte[4096];
                            StreamUtils.Copy(s, streamWriter, buffer);
                        }
                    }
                }

                File.Delete(kibanaFilename + ".zip.tmp");
                
                Console.WriteLine("Kibana downloaded successfully");
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
                new Uri("http://localhost:5602");

            using (
                var host =
                    new NancyHost(
                        new HostConfiguration { AllowChunkedEncoding = false,
                            UrlReservations = new UrlReservations { CreateAutomatically = true } }, uri)
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
