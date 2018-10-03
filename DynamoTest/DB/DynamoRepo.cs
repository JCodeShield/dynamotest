using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using DynamoTest.Services;


namespace DynamoTest.DB
{

    [DataContract]
    class DynamoDbCredential {
        [DataMember] public string accessKey { get; set; }
        [DataMember] public string secretKey { get; set; }
        [DataMember] public string serviceUrl { get; set; }
    }

    public class User
    {
        public string id;
        public string name;
    }

    public class DynamoRepo
    {
        private DynamoDbCredential _dynamoDbCredential;

        private const string dynamo_iam_user_secretName = "dynamo_iam_user";

        public DynamoRepo() {
            log($"Init dynamoRepo");
        }

        public void LoadSecrets()
        {
            log($"Get secret: {dynamo_iam_user_secretName}");
            var secretJson = SecretService.GetSecret(dynamo_iam_user_secretName).Result;
            log($"Got secret: {secretJson}");

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(secretJson));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DynamoDbCredential));
            _dynamoDbCredential = ser.ReadObject(ms) as DynamoDbCredential;

            log($"---> _accessKey  {_dynamoDbCredential.accessKey}");
            log($"---> _secretKey  {_dynamoDbCredential.secretKey}");
            log($"---> _serviceUrl {_dynamoDbCredential.serviceUrl}");
        }

        private void log(string msg)
        {
            LambdaLogger.Log(msg);
        }

        public async Task GetStuffFromDynamoAsync()
        {
            const string tableName = "testTable";

            log("creating client...");

            var client = new AmazonDynamoDBClient(
                _dynamoDbCredential.accessKey, 
                _dynamoDbCredential.secretKey);

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
