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
        const string myRoute = "api/v1/jobs";

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


            ArrayList jobEntries = new ArrayList();
            
            foreach (JobEntity entity in await table.ExecuteQuerySegmentedAsync(new TableQuery<JobEntity>(), null))
            {
                jobEntries.Add(entity);
            }
            ObjectResult result = new ObjectResult(jobEntries);
            return new ObjectResult(BeautifyJson(jobEntries));
        }

        /// <summary>
        /// Returns a formatted/indented Json String.
        /// </summary>
        /// <param name="jobEntries"></param>
        /// <returns></returns>
        public static string BeautifyJson(ArrayList jobEntries)
        {
            JsonSerializerOptions options = new JsonSerializerOptions(){ WriteIndented = true };
            return System.Text.Json.JsonSerializer.Serialize(jobEntries, options);
        }
    }
}
