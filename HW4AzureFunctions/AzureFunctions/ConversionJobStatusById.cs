using System.IO;
using System;
using System.Collections;
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

namespace ConversionJobStatusById.Function
{
    public class ConversionJobStatusById
    {
        const string myRoute = "v1/jobs/{id}";

        private readonly IConfiguration _configuration;

        [FunctionName("ConversionJobStatusById")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = myRoute)] HttpRequest req, [FromRoute]string id, ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get the storage account
            string storageConnectionString = Environment.GetEnvironmentVariable(ConfigSettings.STORAGE_CONNECTION_STRING_NAME);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            
            // Create the table client
            var tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object for the "jobs" table
            var table = tableClient.GetTableReference(ConfigSettings.JOBS_TABLENAME);

            // Retrieve the specified entity from Azure Storage Table
            TableOperation retrieveOperation = TableOperation.Retrieve<JobEntity>(ConfigSettings.IMAGEJOBS_PARTITIONKEY, id);
            TableResult retrievedResult = table.ExecuteAsync(retrieveOperation).ConfigureAwait(false).GetAwaiter().GetResult();

            //if (retrievedResult.Result != null)
            //{
                JobEntity entity = retrievedResult.Result as JobEntity;

                // Map relevant JobEntity attributes to JobResult class
                JobResult jobResult = new JobResult();
                jobResult.jobId = entity.RowKey;
                jobResult.imageConversionMode = entity.imageConversionMode;
                jobResult.status = entity.status;
                jobResult.statusDescription = entity.statusDescription;
                jobResult.imageSource = entity.imageSource;
                jobResult.imageResult = entity.imageResult;

                // Make some pretty Json
                JsonSerializerOptions options = new JsonSerializerOptions(){ WriteIndented = true };
                var formattedResult = System.Text.Json.JsonSerializer.Serialize(jobResult, options);

                return new ObjectResult(formattedResult);
            //}

            //return new ObjectResult({ "error": "not found" });
        }
    }
}
