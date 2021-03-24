namespace HW4AzureFunctions
{
    public class ConfigSettings
    {
        public const string STORAGE_CONNECTION_STRING_NAME = "AzureWebJobsStorage";

        public const string GREYSCALEIMAGES_CONTAINERNAME = "converttogreyscale";

        public const string CONVERTED_IMAGES_CONTAINERNAME = "convertedimages";

        public const string FAILED_IMAGES_CONTAINERNAME = "failedimages";

        public const string JOBS_TABLENAME = "jobs";

        public const string IMAGEJOBS_PARTITIONKEY = "ImageJobs";

        public const string JOBID_METADATA_NAME = "JobId";
    }
}