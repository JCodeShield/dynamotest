using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

namespace DynamoTest.DB
{

    public class User
    {
        public string id;
        public string name;
    }

    public class DynamoRepo
    {
        private string _accessKey;
        private string _secretKey;
        private string _serviceUrl;

        public DynamoRepo() {
            _accessKey = Environment.GetEnvironmentVariable("AccessKey");
            _secretKey = Environment.GetEnvironmentVariable("SecretKey");
            _serviceUrl = Environment.GetEnvironmentVariable("ServiceURL");

            log($"---> _accessKey {_accessKey}");
            log($"---> _secretKey {_secretKey}");
            log($"---> _serviceUrl {_serviceUrl}");
        }

        private void log(string msg)
        {
            LambdaLogger.Log(msg);
        }

        public async Task GetStuffFromDynamoAsync()
        {
            const string tableName = "testTable";

            log("creating client...");

            var client = new AmazonDynamoDBClient(_accessKey, _secretKey);

            log("created client");

            log("ensuring table exists...");

            await EnsureTableExists(client, tableName);

            log("ensured table exists");

            await WaitForTableToBeReady(client, tableName);

            var dbContext = new DynamoDBContext(client);

            var someTestDocument = new User()
            {
                id = "someAwesomeUser",
                name = "Tom"
            };

            await dbContext.SaveAsync(someTestDocument);

            //LambdaLogger.Log
            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition("id", ScanOperator.Equal, someTestDocument.id));
            var allDocs = await dbContext.ScanAsync<User>(conditions).GetRemainingAsync();
            var savedState = allDocs.FirstOrDefault();

            LambdaLogger.Log($"retrieved record has name: {savedState.name}");
        }

        public async Task EnsureTableExists(AmazonDynamoDBClient client, String tableName)
        {
            var tableResponse = await client.ListTablesAsync();
            if (!tableResponse.TableNames.Contains(tableName))
            {
                await client.CreateTableAsync(
                    new CreateTableRequest
                    {
                        TableName = "testTable",
                        KeySchema = new List<KeySchemaElement> { new KeySchemaElement("id", KeyType.HASH) },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 3,
                            WriteCapacityUnits = 1
                        }
                    });


            }
        }

        public async Task WaitForTableToBeReady(AmazonDynamoDBClient client, String tableName)
        {

            bool isTableAvailable = false;
            while (!isTableAvailable)
            {
                Thread.Sleep(5000);
                var tableStatus = await client.DescribeTableAsync(tableName);
                isTableAvailable = tableStatus.Table.TableStatus == "ACTIVE";
            }
        }
    }
}
