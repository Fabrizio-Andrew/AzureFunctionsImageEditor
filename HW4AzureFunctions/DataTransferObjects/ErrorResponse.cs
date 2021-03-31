namespace HW4AzureFunctions.DataTransferObjects
{
    /// <summary>
    /// Defines the standard error format to be returned to the client.
    /// </summary>
    public class ErrorResponse
    {
        public int? errorNumber { get; set; }
        public string parameterName { get; set; }
        public string parameterValue { get; set; }
        public string errorDescription { get; set; }

        /// <summary>
        /// Converts an error number inside an encoded error description, to the standard error response
        /// </summary>
        /// <param name="errorNumber">The error number</param>
        /// <param name="errorMessage">Any error message that returned by the Azure storage service.</param>
        /// <param name="parameterName">The name of the offending parameter</param>
        /// <param name="parameterValue">The offening parameter value</param>
        /// <returns>An ErrorResponse Object</returns>
        public static ErrorResponse GenerateErrorResponse(int? errorNumber, string? errorMessage, string parameterName, string parameterValue)
        {

            switch (errorNumber)
            {
                case 1:
                    {
                        ErrorResponse errorResponse = new ErrorResponse {
                            errorNumber = errorNumber,
                            parameterName = parameterName,
                            parameterValue = parameterValue,
                            errorDescription = "The entity already exists."
                        };
                        return errorResponse;
                    }
                case 2:
                    {
                        ErrorResponse errorResponse = new ErrorResponse {
                            errorNumber = errorNumber,
                            parameterName = parameterName,
                            parameterValue = parameterValue,
                            errorDescription = "The parameter is required."
                        };
                        return errorResponse;
                    }
                case 3:
                    {
                        ErrorResponse errorResponse = new ErrorResponse {
                            errorNumber = errorNumber,
                            parameterName = parameterName,
                            parameterValue = parameterValue,
                            errorDescription = "The entity could not be found."
                        };
                        return errorResponse;
                    }
                case 4:
                    {
                        ErrorResponse errorResponse = new ErrorResponse {
                            errorNumber = errorNumber,
                            parameterName = parameterName,
                            parameterValue = parameterValue,
                            errorDescription = "The parameter cannot be null."
                        };
                        return errorResponse;
                    }
                default:
                    {
                        ErrorResponse errorResponse = new ErrorResponse {
                            errorNumber = errorNumber,
                            parameterName = parameterName,
                            parameterValue = parameterValue,
                            errorDescription = errorMessage
                        };
                        return errorResponse;
                    }
            }
        }
    }
}