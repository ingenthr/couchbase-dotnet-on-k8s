using System;
using System.Threading.Tasks;
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

            var config = new ClusterOptions()
            { 
                UserName = "username",
                Password = "password",
            }.WithLogging(loggerFactory);

            var cluster = await Cluster.ConnectAsync("couchbase://localhost", config);

            var bucket = await cluster.BucketAsync("travel-sample");
            var collection = bucket.DefaultCollection();


            // var upsertResult = await collection.UpsertAsync("my-document-key", new { Name = "Ted", Age = 31 });
            // var getResult = await collection.GetAsync("my-document-key");
            // Console.WriteLine(getResult.ContentAs<dynamic>());

            var queryResult = await cluster.QueryAsync<dynamic>("select \"Hello World\" as greeting");
            await foreach (var row in queryResult) {
                Console.WriteLine(row);
            }
        }
    }
}
