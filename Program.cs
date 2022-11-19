using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading;

namespace HttpPipelineSendCancel
{
    internal class Program
    {
        // Config
        private const int MinDelayMilliseconds = 1;
        private const int MaxDelayMilliseconds = 20;
        private const int ResponseSize = 10_000_000;

        private static readonly string _response = new string('a', ResponseSize);
        private static readonly HttpPipeline _pipeline = HttpPipelineBuilder.Build(new TestClientOptions());

        static async Task Main()
        {
            Console.WriteLine("Starting web server...");
            await new WebHostBuilder()
                .UseKestrel()
                .Configure(app => app.Run(async context =>
                {
                    await context.Response.WriteAsync(_response);
                }))
                .Build()
                .StartAsync();

            var random = new Random();
            while (true)
            {
                var delay = random.Next(MinDelayMilliseconds, MaxDelayMilliseconds);
                using (var cts = new CancellationTokenSource(millisecondsDelay: delay))
                {
                    try
                    {
                        var message = CreateMessage();
                        _pipeline.Send(message, cts.Token);
                        Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] completed");
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] cancelled");
                    }
                }
            }
        }

        private static HttpMessage CreateMessage()
        {
            var message = _pipeline.CreateMessage();
            message.Request.Uri.Reset(new Uri("https://localhost:5001"));
            return message;
        }

        private class TestClientOptions : ClientOptions { }
    }
}