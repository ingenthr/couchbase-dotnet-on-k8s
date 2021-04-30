using System;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Couchbase.Example
{
    class ContainerExample
    {

        static async Task Main(string[] args)
        {

            // .NET 5 logging, added with help from https://docs.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
            // unfortunately, can't log from static without some machinations
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder
                    .AddFilter(level => level >= LogLevel.Trace)
            );
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            Console.WriteLine("Starting version 0.12");

            var config = new ClusterOptions()
            { 
                UserName = "appdemo",
                Password = "letmein",
            }.WithLogging(loggerFactory);

            // SearchByString("cb-appdemo.default.svc");
            // SearchByString("cb-appdemo.default.svc.cluster.local.");


            var cluster = await Cluster.ConnectAsync("couchbase://cb-appdemo.default.svc.cluster.local.", config);

            var bucket = await cluster.BucketAsync("appbucket");
            var collection = bucket.DefaultCollection();


            // var upsertResult = await collection.UpsertAsync("my-document-key", new { Name = "Ted", Age = 31 });
            // var getResult = await collection.GetAsync("my-document-key");
            // Console.WriteLine(getResult.ContentAs<dynamic>());

            // var queryResult = await cluster.QueryAsync<dynamic>("select \"Hello World\" as greeting");
            // await foreach (var row in queryResult) {
            //     Console.WriteLine(row);
            // }


            for (var i = 0; i<1000; i++) {
                    Console.WriteLine('+');
                    await collection.UpsertAsync("foo" + i, new {Name = "bar" + i}).ConfigureAwait(false);  // probably don't need configure await
                }

            while (true) {
                for (var i = 0; i<1000; i++) {
                    try {
                        var result = await collection.GetAsync("foo" +i).ConfigureAwait(false);
                        Console.WriteLine(".");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        Console.WriteLine("Failed during upsert: "  + e.Message);
                    }
                }

            }





        }

        static void SearchByString(String args) {

            Console.WriteLine($"Searching for {args}…");

            // testing dns srv lookup directly
            // DnsClient.Logging.LoggerFactory = loggerFactory;
            ILookupClient lookupClient = new LookupClient();

            var results = lookupClient.Query(args, QueryType.SRV);
            
            foreach (var nameserver in  lookupClient.Settings.NameServers) {
                Console.WriteLine($"DNS client has a nameserver {nameserver}");
            }
            
            foreach (var result in results.Answers) {
                Console.WriteLine($"dns result: {result.ToString()}");
            }

        }

    }
}
