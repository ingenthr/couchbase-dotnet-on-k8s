using System;
using System.Threading.Tasks;
using Couchbase;

namespace repro
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var cluster = await Cluster.ConnectAsync("couchbase://localhost", "username", "password");

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
