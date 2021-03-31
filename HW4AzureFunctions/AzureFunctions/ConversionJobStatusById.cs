using System;
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
using HW4AzureFunctions;
using HW4AzureFunctions.DataTransferObjects;

namespace ConversionJobStatusById.Function
{
    public class ConversionJobStatusById
    {
        const string myRoute = "v1/jobs/{id}";

        private readonly IConfiguration _configuration;

        /// <summary>
        /// Returns a JSON object representing a specific job requested via id.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="id"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ConversionJobStatusById")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = myRoute)] HttpRequest req, [FromRoute]string id, ILogger log)
        {

            log.LogInformation($"ConversionJobStatusById function processed a request for job: {id}.");

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

            if (retrievedResult.Result != null)
            {
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
            }

            // Create error response
            ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(3, null, "id", id);

            // Format error response
            JsonSerializerOptions errorOptions = new JsonSerializerOptions() { WriteIndented = true };
            var formattedError = System.Text.Json.JsonSerializer.Serialize(errorResponse, errorOptions);

            return new ObjectResult(formattedError);
        }
    }
}
