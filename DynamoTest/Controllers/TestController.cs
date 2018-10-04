using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using DynamoTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamoTest.Controllers
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        private static readonly HttpClient client = new HttpClient();

        // GET api/test
        [HttpGet]
        public async Task<string> Get()
        {
            LambdaLogger.Log($"Get Test invoked");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var stringTask = client.GetStringAsync("https://api.github.com/orgs/dotnet/repos");

            var msg = await stringTask;

            LambdaLogger.Log($"Retrieved string from Github");

            LambdaLogger.Log($"Now try to get the secret...");
            var secretJson = SecretService.GetSecret("dynamo_iam_user").Result;
            LambdaLogger.Log($"Secret received");

            return msg;
        }

        // PUT api/test
        [HttpPut]
        public string Put()
        {
            LambdaLogger.Log($"Credential-only Test invoked");
            
            var secretJson = SecretService.GetSecret("dynamo_iam_user").Result;
            LambdaLogger.Log($"Secret received");

            return secretJson;
        }
    }
}