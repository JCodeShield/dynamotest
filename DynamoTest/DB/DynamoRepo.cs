using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using DynamoTest.Models;
using DynamoTest.Services;


namespace DynamoTest.DB
{
    public class DynamoRepo
    {
        private DynamoDbCredential _dynamoDbCredential;
        private AmazonDynamoDBClient _client;
        private DynamoDBContext _dbContext;

        private const string dynamo_iam_user_secretName = "dynamo_iam_user";
        private const string userTable = "User";

        public DynamoRepo() {
            log("Initializing DynamoRepo");

            LoadCredentials();

            CreateClient();

            CreateDbContext();

            EnsureTableExists(userTable).Wait();
        }

        private void LoadCredentials() {
            var secretJson = SecretService.GetSecret(dynamo_iam_user_secretName).Result;

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(secretJson));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DynamoDbCredential));
            _dynamoDbCredential = ser.ReadObject(ms) as DynamoDbCredential;

            log("Retrieved Dynamo DB credentials");
        }

        private void CreateClient() {
            log("Creating client...");
            _client = new AmazonDynamoDBClient(
                _dynamoDbCredential.accessKey,
                _dynamoDbCredential.secretKey);
            log("Finished creating client");
        }

        private void CreateDbContext() {
            _dbContext = new DynamoDBContext(_client);
            log("DbContext created");
        }

        private async Task EnsureTableExists(string tableName) {
            log("Ensuring table exists...");
            await EnsureTableExists(_client, tableName);
            log("Finished ensuring table exists");

            await WaitForTableToBeReady(_client, tableName);
        }

        private void log(string msg)
        {
            LambdaLogger.Log(msg);
        }



        public async Task Save(User user) {
            await _dbContext.SaveAsync(user);
        }


        public async Task<List<User>> Get()
        {
            return await _dbContext.ScanAsync<User>(null).GetRemainingAsync();
        }


        public async Task<User> Get(string id)
        {
            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition("id", ScanOperator.Equal, id));
            var allDocs = await _dbContext.ScanAsync<User>(conditions).GetRemainingAsync();
            return allDocs.FirstOrDefault();
        }

        //public async Task GetStuffFromDynamoAsync()
        //{
        //    var someTestDocument = new User()
        //    {
        //        id = "someAwesomeUser",
        //        name = "Tom2"
        //    };

        //    log("Saving document...");
        //    await dbContext.SaveAsync(someTestDocument);
        //    log("Finishes saving document...");

        //    log("Retrieving document...");
        //    List<ScanCondition> conditions = new List<ScanCondition>();
        //    conditions.Add(new ScanCondition("id", ScanOperator.Equal, someTestDocument.id));
        //    var allDocs = await dbContext.ScanAsync<User>(conditions).GetRemainingAsync();
        //    var savedState = allDocs.FirstOrDefault();
        //    log("Finished retrieving document...");

        //    LambdaLogger.Log($"retrieved record has name: {savedState.name}");
        //}

        public async Task EnsureTableExists(AmazonDynamoDBClient client, string tableName)
        {
            var tableResponse = client.ListTablesAsync().Result;

            if (!tableResponse.TableNames.Contains(tableName))
            {
                log($"List of tables did not contains {tableName}... going to create it");

                await client.CreateTableAsync(
                    new CreateTableRequest
                    {
                        TableName = tableName,
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
