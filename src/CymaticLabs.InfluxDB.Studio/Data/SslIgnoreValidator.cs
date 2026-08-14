using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace CymaticLabs.InfluxDB.Data
{
    /// <summary>
    /// Utility class used to configure SSL/TLS certificate validation for InfluxDB
    /// connections when untrusted SSL is allowed by the user.
    /// </summary>
    /// <remarks>
    /// On .NET the <see cref="System.Net.ServicePointManager"/> settings no longer
    /// affect <see cref="HttpClient"/>, so the certificate validation callback must be
    /// supplied on the <see cref="HttpClientHandler"/> used by each client instead.
    /// </remarks>
    public static class SslIgnoreValidator
    {
        // Whether or not to allow untrusted SSL/TLS certificates.
        private static bool allowUntrusted = false;

        /// <summary>
        /// Gets or sets whether or not to allow untrusted SSL/TLS certificates.
        /// </summary>
        public static bool AllowUntrusted
        {
            get { return allowUntrusted; }
            set { allowUntrusted = value; }
        }

        /// <summary>
        /// Creates an <see cref="HttpClient"/> whose handler bypasses certificate
        /// validation errors when <see cref="AllowUntrusted"/> is enabled.
        /// </summary>
        /// <returns>An <see cref="HttpClient"/> configured for the current setting.</returns>
        public static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler();

            if (allowUntrusted)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage message, X509Certificate2 certificate,
                     X509Chain chain, System.Net.Security.SslPolicyErrors errors) => true;
            }

            return new HttpClient(handler);
        }
    }
}
