using System.Runtime.Serialization;

namespace DynamoTest.DB
{
    [DataContract]
    public class DynamoDbCredential
    {
        [DataMember] public string accessKey { get; set; }
        [DataMember] public string secretKey { get; set; }
        [DataMember] public string serviceUrl { get; set; }
    }
}
