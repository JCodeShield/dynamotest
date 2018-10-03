using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using DynamoTest.DB;
using DynamoTest.Models;
using Microsoft.AspNetCore.Mvc;

namespace DynamoTest.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            LambdaLogger.Log($"Request for getting all users");

            var repo = new DynamoRepo();

            var users = repo.Get().Result;
            LambdaLogger.Log($"User list retrieved");

            return users.Select(x => x.name).ToList();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<string> GetAsync(string id)
        {
            LambdaLogger.Log($"Request for getting user: {id}");

            var repo = new DynamoRepo();

            var user = await repo.Get(id);

            if (user == null) {
                LambdaLogger.Log($"User: {id} not found");
                Response.StatusCode = NotFound().StatusCode;
                return null;
            }

            LambdaLogger.Log($"Found User: {id}");
            return user.name;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string name)
        {
            LambdaLogger.Log($"Request for new user: {name}");

            var repo = new DynamoRepo();

            var user = new User
            {
                id = new Random().Next().ToString(),
                name = name
            };

            repo.Save(user).Wait();

            LambdaLogger.Log($"New user saved: {user.id} -> {user.name}");
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
