namespace HW4AzureFunctions.DataTransferObjects
{
    /// <summary>
    /// Defines the Job Status data to be provided to the client.
    /// </summary>
    public class JobResult
    {
        public string jobId { get; set; }
        
        public string imageConversionMode { get; set; }

        public int status { get; set; }

        public string statusDescription { get; set; }

        public string imageSource { get; set; }

        public string imageResult { get; set; }
    }
}