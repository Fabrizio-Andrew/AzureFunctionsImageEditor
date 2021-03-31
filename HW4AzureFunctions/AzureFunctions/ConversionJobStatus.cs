using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
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
        const string myRoute = "v1/jobs";

        private readonly IConfiguration _configuration;


        [FunctionName("ConversionJobStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = myRoute)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get the storage account
            string storageConnectionString = Environment.GetEnvironmentVariable(ConfigSettings.STORAGE_CONNECTION_STRING_NAME);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            
            // Create the table client
            var tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object for the "jobs" table
            var table = tableClient.GetTableReference("jobs");


            ArrayList resultsList = new ArrayList();
            
            foreach (JobEntity entity in await table.ExecuteQuerySegmentedAsync(new TableQuery<JobEntity>(), null))
            {
                // Map relevant JobEntity attributes to JobResult class
                JobResult jobResult = new JobResult();
                jobResult.jobId = entity.RowKey;
                jobResult.imageConversionMode = entity.imageConversionMode;
                jobResult.status = entity.status;
                jobResult.statusDescription = entity.statusDescription;
                jobResult.imageSource = entity.imageSource;
                jobResult.imageResult = entity.imageResult;

                resultsList.Add(jobResult);
            }
            ObjectResult result = new ObjectResult(resultsList);

            // Make some pretty Json
            JsonSerializerOptions options = new JsonSerializerOptions(){ WriteIndented = true };
            var formattedResults = System.Text.Json.JsonSerializer.Serialize(resultsList, options);

            return new ObjectResult(formattedResults);
        }
    }
}
