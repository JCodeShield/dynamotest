using Amazon;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Threading.Tasks;

namespace DynamoTest.Services
{
    public class SecretService
    {

        /*
 *	Use this code snippet in your app.
 *	If you need more information about configurations or implementing the sample code, visit the AWS docs:
 *	https://aws.amazon.com/developers/getting-started/net/
 *	
 *	Make sure to include the following packages in your code.
 *	
 *	using System;
 *	using System.IO;
 *
 *	using Amazon;
 *	using Amazon.SecretsManager;
 *	using Amazon.SecretsManager.Model;
 *
 */

        /*
         * AWSSDK.SecretsManager version="3.3.0" targetFramework="net45"
         */
        public  static async Task<string> GetSecret(string secretName)
        {
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.USWest2);

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.
            //request.SecretId = "arn:aws:secretsmanager:us-west-2:809794767795:secret:dynamo_iam_user-XN2bM3";
            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            try
            {
                response = await client.GetSecretValueAsync(request);
            }
            catch (DecryptionFailureException e)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log("DecryptionFailureException ocurred");
                throw;
            }
            catch (InternalServiceErrorException e)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log("InternalServiceErrorException ocurred");
                throw;
            }
            catch (InvalidParameterException e)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                LambdaLogger.Log("The request had invalid params: " + e.Message);

                throw;
            }
            catch (InvalidRequestException e)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log("The request was invalid due to: " + e.Message);

                throw;
            }
            catch (ResourceNotFoundException e)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log("The requested secret " + secretName + " was not found");

                throw;
            }
            catch (System.AggregateException ae)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                LambdaLogger.Log("AggregateException");
                throw;
            }

            return response?.SecretString;
        }

        private static RegionEndpoint AmazonSecretsManagerConfig(RegionEndpoint uSWest2)
        {
            throw new NotImplementedException();
        }
    }
}
