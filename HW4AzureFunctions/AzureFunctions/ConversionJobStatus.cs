using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using HW4AzureFunctions;

namespace ConversionJobStatus.Function
{
    public class ConversionJobStatus
    {
        const string myRoute = "api/v1/jobs";

        private readonly IConfiguration _configuration;


        [FunctionName("ConversionJobStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = myRoute)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString(ConfigSettings.STORAGE_CONNECTION_STRING_NAME));

            // Create the table client
            var tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object for the "jobs" table
            var table = tableClient.GetTableReference("jobs");


            ArrayList jobEntries = new ArrayList();
            
            foreach (JobEntity entity in await table.ExecuteQuerySegmentedAsync(new TableQuery<JobEntity>(), null))
            {
                jobEntries.Add(entity);
            }

            return new ObjectResult(jobEntries);
        }
    }
}
