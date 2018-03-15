﻿using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Streams;
using Utils;

namespace OrleansClient
{
	class Program
	{
		private static string _joinedChannel = "general";

		static void Main(string[] args)
		{
			var clientInstance = InitializeClient(args).GetAwaiter().GetResult();

			PrettyConsole.Line("==== CLIENT: Initialized ====", ConsoleColor.Cyan);
			PrettyConsole.Line("CLIENT: Write commands:", ConsoleColor.Cyan);


			Menu(clientInstance).GetAwaiter().GetResult();

			PrettyConsole.Line("==== CLIENT: Shutting down ====", ConsoleColor.DarkRed);
		}

		static async Task<IClusterClient> InitializeClient(string[] args)
		{
			int initializeCounter = 0;

			var initSucceed = false;
			while (!initSucceed)
			{
				try
				{
				    var siloAddress = IPAddress.Loopback;
				    var gatewayPort = 30000;
                    var client = new ClientBuilder().Configure<ClusterOptions>(options => options.ClusterId = "helloworldcluster")
				        .UseStaticClustering(options => options.Gateways.Add((new IPEndPoint(siloAddress, gatewayPort)).ToGatewayUri()))
				        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ITicker).Assembly).WithReferences())
				        .ConfigureLogging(logging => logging.AddConsole())
                        .AddSimpleMessageStreamProvider(FluentConfig.AltNetStream, options => options.PubSubType = StreamPubSubType.ImplicitOnly)
				        .Build();
                    await client.Connect();
					initSucceed = client.IsInitialized;

					if (initSucceed)
					{
						return client;
					}
				}
				catch (Exception exc)
				{
					PrettyConsole.Line(exc.Message, ConsoleColor.Cyan);
					initSucceed = false;
				}

				if (initializeCounter++ > 10)
				{
					return null;
				}

				PrettyConsole.Line("Client Init Failed. Sleeping 5s...", ConsoleColor.Red);
				Thread.Sleep(TimeSpan.FromSeconds(5));
			}

			return null;
		}

		private static void PrintHints()
		{
			var menuColor = ConsoleColor.Magenta;
			PrettyConsole.Line("Type '/r' to run task", menuColor);
			PrettyConsole.Line("Type '/j <channel>' to join specific channel", menuColor);
			PrettyConsole.Line("Type '/l <channel>' to leave specific channel", menuColor);
			PrettyConsole.Line("Type '<any text>' to send a message", menuColor);
			PrettyConsole.Line("Type '/h' to re-read channel history", menuColor);
			PrettyConsole.Line("Type '/exit' to exit client.", menuColor);
		}

		private static async Task Menu(IClusterClient client)
		{
			string input;
			PrintHints();

			do
			{
				input = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(input)) continue;

				if (input.StartsWith("/j"))
				{
					await JoinChannel(client, input.Replace("/j", "").Trim());
				}
				else if (input.StartsWith("/r"))
				{
					await RunTask(client);
				}
				else if (input.StartsWith("/l"))
				{
					await LeaveChannel(client, input.Replace("/l ", "").Trim());
				}
				else if (input.StartsWith("/h"))
				{
					await ShowCurrentChannelHistory(client);
				}
				else if (!input.StartsWith("/exit"))
				{
					await SendMessage(client, input);
				}
			} while (input != "/exit");
		}

		private static async Task RunTask(IClusterClient client)
		{
			var tick = client.GetGrain<ITicker>(Guid.Empty);

			foreach (var item in Enumerable.Range(1,100))
			{
				try
				{
					var res = await tick.Tick(10);
				}
				catch (Exception e)
				{
					PrettyConsole.Line($"Exception {e.Message}");
				}
				
			}
			
		}

		private static async Task SendMessage(IClusterClient client, string messageText)
		{
			var room = client.GetGrain<IChatRoom>(_joinedChannel);
			await room.Message(new ChatMsg("Alexey", messageText));
		}

		private static async Task JoinChannel(IClusterClient client, string channelName)
		{
			PrettyConsole.Line($"Joining to channel {channelName}");
			_joinedChannel = channelName;
			var room = client.GetGrain<IChatRoom>(_joinedChannel);
			await room.Join("Alexey");


			await ShowCurrentChannelHistory(client);
		}

		private static async Task LeaveChannel(IClusterClient client, string channelName)
		{
			PrettyConsole.Line($"Leaving to channel {channelName}");
			var room = client.GetGrain<IChatRoom>(_joinedChannel);
			await room.Leave("Alexey");
		}

		private static async Task ShowCurrentChannelHistory(IClusterClient client)
		{
			var room = client.GetGrain<IChatRoom>(_joinedChannel);
			var history = await room.ReadHistory(1000);

			PrettyConsole.Line($"====== History for '{_joinedChannel}' ======", ConsoleColor.DarkGreen);
			foreach (var chatMsg in history)
			{
				PrettyConsole.Line($" ({chatMsg.Created:g}) {chatMsg.Author}> {chatMsg.Text}", authorColor(chatMsg.Author));
			}
			PrettyConsole.Line("============", ConsoleColor.DarkGreen);
		}

		private static ConsoleColor authorColor(string chatMsgAuthor)
		{
			switch (chatMsgAuthor.ToLowerInvariant())
			{
				case "alexey": return ConsoleColor.Green;
				case "system": return ConsoleColor.Red;
				default: return ConsoleColor.Yellow;
			}
		}
	}
}