using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using HW4AzureFunctions;

namespace ImageStatusUpdaterSuccess.Function
{
    public static class ImageStatusUpdaterSuccess
    {
        [FunctionName("ImageStatusUpdaterSuccess")]
        public static async Task Run([BlobTrigger("convertedimages/{name}", Connection = ConfigSettings.STORAGE_CONNECTION_STRING_NAME)]CloudBlockBlob convertedImage, string name, ILogger log)
        {
            string jobStatus = "Success!";
            string jobMessage = "Image converted successfully.";

            // Retrieve attributes (jobId) from blob
            await convertedImage.FetchAttributesAsync();

            if (convertedImage.Metadata.ContainsKey(ConfigSettings.JOBID_METADATA_NAME))
            {
                string jobId = convertedImage.Metadata[ConfigSettings.JOBID_METADATA_NAME];

                log.LogInformation($"C# Blob trigger function Processed blob\n Name:{convertedImage.Name} \n JobId: [{jobId}]");

                // Set the job status
                JobTable jobTable = new JobTable(log, ConfigSettings.IMAGEJOBS_PARTITIONKEY);
                await jobTable.UpdateJobEntityStatus(jobId, jobStatus, jobMessage);
            }
            else 
            {
                log.LogError($"The blob {convertedImage.Name} is missing its {ConfigSettings.JOBID_METADATA_NAME} metadata can't update the job");
            }
        }
    }
}