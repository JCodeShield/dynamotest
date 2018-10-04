using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using DynamoTest.Models;

namespace DynamoTest.DB
{
    public interface IDynamoRepo
    {
        Task<List<User>> Get();
        Task<User> Get(string id);
        Task Save(User user);
    }
}