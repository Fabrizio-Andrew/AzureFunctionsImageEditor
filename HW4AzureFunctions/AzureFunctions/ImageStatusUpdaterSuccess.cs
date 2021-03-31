using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using HW4AzureFunctions;

namespace ImageStatusUpdaterSuccess.Function
{
    public static class ImageStatusUpdaterSuccess
    {
        /// <summary>
        /// Stores successfully converted blobs in the convertedimages container.
        /// 
        /// Updates the job status table for the successful job.
        /// </summary>
        /// <param name="convertedImage"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ImageStatusUpdaterSuccess")]
        public static async Task Run([BlobTrigger("convertedimages/{name}", Connection = ConfigSettings.STORAGE_CONNECTION_STRING_NAME)]CloudBlockBlob convertedImage, ILogger log)
        {
            const int jobStatus = 3;
            const string jobMessage = "Image converted successfully.";

            // Retrieve attributes (jobId) from blob
            await convertedImage.FetchAttributesAsync();

            if (convertedImage.Metadata.ContainsKey(ConfigSettings.JOBID_METADATA_NAME))
            {
                string jobId = convertedImage.Metadata[ConfigSettings.JOBID_METADATA_NAME];
                string imageResult = convertedImage.Uri.ToString();

                log.LogInformation($"C# Blob trigger function Processed blob\n Name:{convertedImage.Name} \n JobId: [{jobId}]");

                // Set the job status
                JobTable jobTable = new JobTable(log, ConfigSettings.IMAGEJOBS_PARTITIONKEY);
                await jobTable.UpdateJobEntityStatus(jobId, jobStatus, jobMessage, imageResult);
            }
            else 
            {
                log.LogError($"The blob {convertedImage.Name} is missing its {ConfigSettings.JOBID_METADATA_NAME} metadata can't update the job");
            }
        }
    }
}
