using System;
using System.Net;
using System.Runtime.CompilerServices;
using GrainImplementation;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Streams;
using Utils;

namespace OrleansServer
{
	class Program
	{
		static void Main(string[] args)
		{
		    var siloPort = 11111;
		    int gatewayPort = 30000;
		    var siloAddress = IPAddress.Loopback;
		    var builder = new SiloHostBuilder()
		        .Configure<ClusterOptions>(options => options.ClusterId = "helloworldcluster")
		        .UseDevelopmentClustering(options => options.PrimarySiloEndpoint = new IPEndPoint(siloAddress, siloPort))
		        .ConfigureEndpoints(siloAddress, siloPort, gatewayPort)
		        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(TickerGrain).Assembly).WithReferences())
		        .ConfigureLogging(logging => logging.AddConsole())
                .AddMemoryGrainStorage("PubSubStore")
		        .AddSimpleMessageStreamProvider(FluentConfig.AltNetStream, options=>options.PubSubType = StreamPubSubType.ImplicitOnly); ;


		    var silo = builder.Build();
		    silo.StartAsync().Wait();

			Console.WriteLine("Press Enter to close.");
			Console.ReadLine();

			// shut the silo down after we are done.
		    silo.StopAsync().Wait();
		}
	}
}
