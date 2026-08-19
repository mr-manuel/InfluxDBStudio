using System.Net;

namespace CymaticLabs.InfluxDB.Data
{
    /// <summary>
    /// Represents a response from an InfluxDB API request.
    /// </summary>
    public class InfluxDbApiResponse
    {
        #region Fields

        /// <summary>
        /// The body of the response.
        /// </summary>
        public readonly string Body;

        /// <summary>
        /// The HTTP status code of the API response.
        /// </summary>
        public readonly HttpStatusCode StatusCode;

        /// <summary>
        /// Whether or not the request was successful.
        /// </summary>
        public readonly bool Success;

        #endregion Fields

        #region Constructors

        public InfluxDbApiResponse(string body, HttpStatusCode statusCode, bool success)
        {
            // InfluxDB's /write endpoint returns 204 No Content (an empty body) on success,
            // so an empty body here is not necessarily an error condition.
            Body = body ?? string.Empty;
            StatusCode = statusCode;
            Success = success;
        }

        #endregion Constructors

        #region Methods

        #endregion Methods
    }
}
