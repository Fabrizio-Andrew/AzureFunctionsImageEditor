using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HW4AzureFunctions
{
    public class ImageCleanup
    {
        /// <summary>
        /// Batch job that deletes all original images for jobs that have been successfully completed.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ImageCleanup")]
        public async Task Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Image Cleanup function executed at: {DateTime.Now}");

            // Get the storage account
            string storageConnectionString = Environment.GetEnvironmentVariable(ConfigSettings.STORAGE_CONNECTION_STRING_NAME);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the table client
            var tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object for the "jobs" table
            var table = tableClient.GetTableReference("jobs");

            // Query operation for all jobs that have been successfully completed
            TableQuery<JobEntity> completedJobsQuery = new TableQuery<JobEntity>().Where(TableQuery.GenerateFilterConditionForInt("status", QueryComparisons.Equal, 3));

            // Sort blob names into greyscale and sepia lists
            ArrayList greyscaleList = new ArrayList();
            ArrayList sepiaList = new ArrayList();

            foreach (JobEntity entity in await table.ExecuteQuerySegmentedAsync(completedJobsQuery, null))
            {
                // Retrieve the container and blob names from the imageSource url string
                string[] urlSplit = entity.imageSource.Split('/');
                string containerName = urlSplit[3];
                string blobName = urlSplit[4];

                if (containerName == ConfigSettings.GREYSCALEIMAGES_CONTAINERNAME)
                {
                    greyscaleList.Add(blobName);
                }
                else if (containerName == ConfigSettings.SEPIAIMAGES_CONTAINERNAME)
                {
                    sepiaList.Add(blobName);
                }
            }

            // Create blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get greyscale and sepia container references
            CloudBlobContainer greyscaleContainer = blobClient.GetContainerReference(ConfigSettings.GREYSCALEIMAGES_CONTAINERNAME);
            CloudBlobContainer sepiaContainer = blobClient.GetContainerReference(ConfigSettings.SEPIAIMAGES_CONTAINERNAME);

            // Delete blobs from greyscaleList
            foreach (string name in greyscaleList)
            {
                var blob = greyscaleContainer.GetBlockBlobReference(name);
                await blob.DeleteIfExistsAsync();
            }

            // Delete blobs from sepiaList
            foreach (string name in sepiaList)
            {
                var blob = sepiaContainer.GetBlockBlobReference(name);
                await blob.DeleteIfExistsAsync();
            }
        }
    }
}
