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
    public class UsersController : Controller
    {
        // GET api/users
        [HttpGet]
        public IEnumerable<string> Get()
        {
            LambdaLogger.Log($"Request for getting all users");

            var repo = new DynamoRepo();

            var users = repo.Get().Result;
            LambdaLogger.Log($"User list retrieved");

            return users.Select(x => x.name).ToList();
        }

        // GET api/users/userid
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

        // POST api/users
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/users
        [HttpPut()]
        public void Put([FromBody]User user)
        {
            var repo = new DynamoRepo();

            repo.Save(user).Wait();

            LambdaLogger.Log($"New user saved: {user.id} -> {user.name}");
        }

        // DELETE api/users/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
