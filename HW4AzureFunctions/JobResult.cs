namespace HW4AzureFunctions
{
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