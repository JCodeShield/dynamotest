using System;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace DynamoTest
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public dynamic FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.Log(request.ToString());

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = { },
                Body = "CC" + new Random().Next().ToString() + request.ToString()
            };
        }
    }
}
