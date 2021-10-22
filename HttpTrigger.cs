using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Digitransit.Function
{
    public static class HttpTrigger
    {
        [FunctionName("HttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log, ClaimsPrincipal principal)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            bool isAuthorized = false;
            if (null != principal)  
                {  
                foreach (Claim claim in principal.Claims)  
                {
                    if (claim.Type.ToString().Equals("roles") && claim.Value.ToString().Equals("Task.Read")) {
                        isAuthorized = true;
                    }
                }  
            }

            if (!isAuthorized) {
                log.LogInformation("Unauthorized user");
                var result = new ObjectResult("User lacks role permission");
                result.StatusCode = StatusCodes.Status401Unauthorized;
                return result;
            }

            var id = "integration";
            var key = Environment.GetEnvironmentVariable("KEY");
            var expiry = DateTime.UtcNow.AddHours(1);
            string encodedToken;
            using (var encoder = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                var dataToSign = id + "\n" + expiry.ToString("O", CultureInfo.InvariantCulture);
                var hash = encoder.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
                var signature = Convert.ToBase64String(hash);
                encodedToken = string.Format("SharedAccessSignature uid={0}&ex={1:o}&sn={2}", id, expiry, signature);
            }

            string responseMessage = encodedToken;

            return new OkObjectResult(responseMessage);
        }
    }
}
