using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using HW4AzureFunctions;

namespace ImageStatusUpdater.Function
{
    public static class ImageStatusUpdaterFailed
    {
        [FunctionName("ImageStatusUpdaterFailed")]
        public static async Task Run([BlobTrigger("failedimages/{name}", Connection = ConfigSettings.STORAGE_CONNECTION_STRING_NAME)]CloudBlockBlob failedImage, string name, ILogger log)
         {
            const int jobStatus = 4;
            const string jobMessage = "Image conversion failed.";

            // Retrieve attributes (jobId) from blob
            await failedImage.FetchAttributesAsync();

            if (failedImage.Metadata.ContainsKey(ConfigSettings.JOBID_METADATA_NAME))
            {
                string jobId = failedImage.Metadata[ConfigSettings.JOBID_METADATA_NAME];

                log.LogInformation($"C# Blob trigger function Processed blob\n Name:{failedImage.Name} \n JobId: [{jobId}]");

                // Set the job status
                JobTable jobTable = new JobTable(log, ConfigSettings.IMAGEJOBS_PARTITIONKEY);
                await jobTable.UpdateJobEntityStatus(jobId, jobStatus, jobMessage);
            }
            else 
            {
                log.LogError($"The blob {failedImage.Name} is missing its {ConfigSettings.JOBID_METADATA_NAME} metadata can't update the job");
            }
        }
    }
}