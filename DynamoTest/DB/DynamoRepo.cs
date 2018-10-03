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
            log("Initializing DynamoRepo");

            var secretJson = SecretService.GetSecret(dynamo_iam_user_secretName).Result;
            log("Retrieved Dynamo DB credentials");

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(secretJson));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DynamoDbCredential));
            _dynamoDbCredential = ser.ReadObject(ms) as DynamoDbCredential;
        }

        private void log(string msg)
        {
            LambdaLogger.Log(msg);
        }

        public async Task GetStuffFromDynamoAsync()
        {
            const string tableName = "testTable";

            log("Creating client...");
            var client = new AmazonDynamoDBClient(
                _dynamoDbCredential.accessKey, 
                _dynamoDbCredential.secretKey);
            log("Finished creating client");

            log("Ensuring table exists...");
            await EnsureTableExists(client, tableName);
            log("Finished ensuring table exists");

            await WaitForTableToBeReady(client, tableName);

            log("Creating DBcontext...");
            var dbContext = new DynamoDBContext(client);
            log("Finished creating DBcontext...");

            var someTestDocument = new User()
            {
                id = "someAwesomeUser",
                name = "Tom"
            };

            log("Saving document...");
            await dbContext.SaveAsync(someTestDocument);
            log("Finishes saving document...");

            log("Retrieving document...");
            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition("id", ScanOperator.Equal, someTestDocument.id));
            var allDocs = await dbContext.ScanAsync<User>(conditions).GetRemainingAsync();
            var savedState = allDocs.FirstOrDefault();
            log("Finished retrieving document...");

            LambdaLogger.Log($"retrieved record has name: {savedState.name}");
        }

        public async Task EnsureTableExists(AmazonDynamoDBClient client, String tableName)
        {
            var tableResponse = client.ListTablesAsync().Result;

            if (!tableResponse.TableNames.Contains(tableName))
            {
                log($"List of tables did not contains {tableName}... going to create it");

                await client.CreateTableAsync(
                    new CreateTableRequest
                    {
                        TableName = "testTable",
                        KeySchema = new List<KeySchemaElement> {
                            new KeySchemaElement("id", KeyType.HASH) },
                        AttributeDefinitions = new List<AttributeDefinition> {
                            new AttributeDefinition("id", ScalarAttributeType.S)},
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
            log("Waiting for table to be ready.");

            bool isTableAvailable = false;
            do
            {
                var tableStatus = await client.DescribeTableAsync(tableName);
                isTableAvailable = tableStatus.Table.TableStatus == "ACTIVE";
                if (!isTableAvailable) { log("Table not yet ready."); Thread.Sleep(5000); }
            } while (!isTableAvailable);

            log("Table is ready.");
        }
    }
}
