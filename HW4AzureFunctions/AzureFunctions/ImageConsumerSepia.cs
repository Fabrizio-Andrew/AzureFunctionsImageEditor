using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using HW4AzureFunctions;

namespace ImageConsumerSepia.Function
{
    public static class ImageConsumerSepia
        {

        const string imageConversionMode = "Sepia";
        const string ImagesToConvertRoute = "converttosepia/{name}";

        /// <summary>
        /// Converts images uploaded into the "converttosepia" container into sepia format.
        /// If success, the convertedimages container contains the result image.
        /// If fail, the failedimages container contains a copy of the original image uploaded into the
        /// "converttosepia" continer
        /// 
        /// An initial job record is added to the jobs table upon receipt of the job.The job status is updated
        /// when the image is about to be converted.        /// </summary>
        /// <param name="cloudBlockBlob">The BLOB.</param>
        /// <param name="name">The name.</param>
        /// <param name="log">The log.</param>
        [FunctionName("ImageConsumerSepia")]
        public static async Task Run([BlobTrigger(ImagesToConvertRoute, Connection = ConfigSettings.STORAGE_CONNECTION_STRING_NAME)]CloudBlockBlob cloudBlockBlob, string name, ILogger log)
        {
            log.LogInformation($"ImageConsumerSepia function Processed blob\n Name:{name} \n ContentType: {cloudBlockBlob.Properties.ContentType} Bytes");

            // Assign a GUID to the job
            string jobId = Guid.NewGuid().ToString();

            // Retrieve the public uri for the uploaded blob
            await cloudBlockBlob.FetchAttributesAsync();
            string uri = cloudBlockBlob.Uri.ToString();

            // Create the table entity
            await UpdateJobTableWithStatus(log, jobId, status: 1, message: "Blob received.", imageSource: uri);

            using (Stream blobStream = await cloudBlockBlob.OpenReadAsync())
            {
                // Get the storage account
                string storageConnectionString = Environment.GetEnvironmentVariable(ConfigSettings.STORAGE_CONNECTION_STRING_NAME);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Create a blob client
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Create or retrieve a reference to the converted images container
                CloudBlobContainer convertedImagesContainer = blobClient.GetContainerReference(ConfigSettings.CONVERTED_IMAGES_CONTAINERNAME);
                bool created = await convertedImagesContainer.CreateIfNotExistsAsync();
                log.LogInformation($"[{ConfigSettings.CONVERTED_IMAGES_CONTAINERNAME}] Container needed to be created: {created}");

                CloudBlobContainer failedImagesContainer = blobClient.GetContainerReference(ConfigSettings.FAILED_IMAGES_CONTAINERNAME);
                created = await failedImagesContainer.CreateIfNotExistsAsync();
                log.LogInformation($"[{ConfigSettings.FAILED_IMAGES_CONTAINERNAME}] Container needed to be created: {created}");

                await ConvertAndStoreImage(log, blobStream, convertedImagesContainer, name, failedImagesContainer, jobId, uri);
            }
        }

        /// <summary>
        /// Converts and stores the image.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="uploadedImage"></param>
        /// <param name="convertedImagesContainer"></param>
        /// <param name="blobName"></param>
        /// <param name="failedImagesContainer"></param>
        /// <param name="jobId"></param>
        /// <param name="imageSource"></param>
        private static async Task ConvertAndStoreImage(ILogger log,
                                                 Stream uploadedImage,
                                                 CloudBlobContainer convertedImagesContainer,
                                                 string blobName,
                                                 CloudBlobContainer failedImagesContainer,
                                                 string jobId,
                                                 string imageSource)
        {
            string convertedBlobName = $"{Guid.NewGuid()}-{blobName}";

            try
            {
                // Update Job Status - about to convert image
                await UpdateJobTableWithStatus(log, jobId, status: 2, message: "Processing blob.", imageSource: imageSource);

                uploadedImage.Seek(0, SeekOrigin.Begin);

                using (MemoryStream convertedMemoryStream = new MemoryStream())
                using (Image<Rgba32> image = (Image<Rgba32>)Image.Load(uploadedImage))
                {
                    log.LogInformation($"[+] Starting conversion of image {blobName}");

                    // Convert the image
                    image.Mutate(x => x.Sepia());
                    image.SaveAsJpeg(convertedMemoryStream);

                    convertedMemoryStream.Seek(0, SeekOrigin.Begin);
                    log.LogInformation($"[-] Completed conversion of image {blobName}");

                    log.LogInformation($"[+] Storing converted image {blobName} into {ConfigSettings.CONVERTED_IMAGES_CONTAINERNAME} container");

                    CloudBlockBlob convertedBlockBlob = convertedImagesContainer.GetBlockBlobReference(convertedBlobName);

                    convertedBlockBlob.Metadata.Add(ConfigSettings.JOBID_METADATA_NAME, jobId);

                    convertedBlockBlob.Properties.ContentType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                    await convertedBlockBlob.UploadFromStreamAsync(convertedMemoryStream);

                    log.LogInformation($"[-] Stored converted image {convertedBlobName} into {ConfigSettings.CONVERTED_IMAGES_CONTAINERNAME} container");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to convert blob {blobName} Exception ex {ex.Message}");
                await StoreFailedImage(log, uploadedImage, blobName, failedImagesContainer, convertedBlobName: convertedBlobName, jobId: jobId);
            }
        }
        /// <summary>
        /// Updates the job table with status.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="message">The message.</param>
        /// <param name="imageSource">The original image URL.</param>
        private static async Task UpdateJobTableWithStatus(ILogger log, string jobId, int status, string message, string imageSource)
        {
            JobTable jobTable = new JobTable(log, ConfigSettings.IMAGEJOBS_PARTITIONKEY);
            await jobTable.InsertOrReplaceJobEntity(jobId, status: status, message: message, imageSource: imageSource, imageConversionMode);
        }

        /// <summary>
        /// Stores the failed image.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="uploadedImage">The uploaded image.</param>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <param name="failedImagesContainer">The failed images container.</param>
        /// <param name="convertedBlobName">Name of the converted BLOB.</param>
        /// <param name="jobId">The job identifier.</param>
        private static async Task StoreFailedImage(ILogger log, Stream uploadedImage, string blobName, CloudBlobContainer failedImagesContainer, string convertedBlobName, string jobId)
        {
            try
            {
                log.LogInformation($"[+] Storing failed image {blobName} into {ConfigSettings.FAILED_IMAGES_CONTAINERNAME} container as blob name: {convertedBlobName}");

                CloudBlockBlob failedBlockBlob = failedImagesContainer.GetBlockBlobReference(convertedBlobName);
                failedBlockBlob.Metadata.Add(ConfigSettings.JOBID_METADATA_NAME, jobId);

                uploadedImage.Seek(0, SeekOrigin.Begin);
                await failedBlockBlob.UploadFromStreamAsync(uploadedImage);

                log.LogInformation($"[+] Stored failed image {blobName} into {ConfigSettings.FAILED_IMAGES_CONTAINERNAME} container as blob name: {convertedBlobName}");
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to store a blob called {blobName} that failed conversion into {ConfigSettings.FAILED_IMAGES_CONTAINERNAME}. Exception ex {ex.Message}");
            }
        }
    }
}
