using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace IDCH.Storage
{
    /// <summary>
    /// Settings when using AWS S3 for storage.
    /// </summary>
    public class IDCHSettings : BlobSettings
    {
        #region Public-Members

        /// <summary>
        /// Override the AWS S3 endpoint (if using non-Amazon storage), otherwise leave null.
        /// Use the form http://localhost:8000/
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// Enable or disable SSL (only if using non-Amazon storage).
        /// </summary>
        public bool Ssl { get; set; } = false;

        /// <summary>
        /// AWS S3 access key.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// AWS S3 secret key.
        /// </summary>
        public string SecretKey { get; set; } = null;

        /// <summary>
        /// AWS S3 region.
        /// </summary>
        public IDCHRegion Region { get; set; } = IDCHRegion.USWest1;

        /// <summary>
        /// AWS S3 bucket.
        /// </summary>
        public string Bucket { get; set; } = null;

        /// <summary>
        /// Base URL to use for objects, i.e. https://[bucketname].s3.[regionname].amazonaws.com/.
        /// For non-S3 endpoints, use {bucket} and {key} to indicate where these values should be inserted, i.e. http://{bucket}.[hostname]:[port]/{key} or https://[hostname]:[port]/{bucket}/key.
        /// </summary>
        public string BaseUrl { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initialize the object.
        /// </summary>
        public IDCHSettings()
        {

        }

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="accessKey">Access key with which to access AWS S3.</param>
        /// <param name="secretKey">Secret key with which to access AWS S3.</param>
        /// <param name="region">AWS region.</param>
        /// <param name="bucket">Bucket in which to store BLOBs.</param>
        public IDCHSettings(string accessKey, string secretKey, string bucket)
        {
            IDCHRegion region = IDCHRegion.USWest1;
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket)); 

            Endpoint = null;
            Ssl = true;
            AccessKey = accessKey;
            SecretKey = secretKey;
            Region = region;
            Bucket = bucket; 
        }

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="accessKey">Access key with which to access AWS S3.</param>
        /// <param name="secretKey">Secret key with which to access AWS S3.</param>
        /// <param name="region">AWS region.</param>
        /// <param name="bucket">Bucket in which to store BLOBs.</param>
        public IDCHSettings(string accessKey, string secretKey, string region, string bucket)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (String.IsNullOrEmpty(region)) throw new ArgumentNullException(nameof(region));
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));

            AccessKey = accessKey;
            SecretKey = secretKey;
            Bucket = bucket;

            if (!ValidateRegion(region)) throw new ArgumentException("Unable to validate region: " + region);
            Region = GetRegionFromString(region);
        }

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="accessKey">Access key with which to access AWS S3.</param>
        /// <param name="secretKey">Secret key with which to access AWS S3.</param>
        /// <param name="region">AWS region.</param>
        /// <param name="bucket">Bucket in which to store BLOBs.</param>
        /// <param name="ssl">Enable or disable SSL.</param>
        public IDCHSettings(string accessKey, string secretKey, IDCHRegion region, string bucket, bool ssl)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket)); 

            Endpoint = null;
            Ssl = true;
            AccessKey = accessKey;
            SecretKey = secretKey;
            Region = region;
            Bucket = bucket;
            Ssl = ssl; 
        }

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="accessKey">Access key with which to access AWS S3.</param>
        /// <param name="secretKey">Secret key with which to access AWS S3.</param>
        /// <param name="region">AWS region.</param>
        /// <param name="bucket">Bucket in which to store BLOBs.</param>
        /// <param name="ssl">Enable or disable SSL.</param> 
        public IDCHSettings(string accessKey, string secretKey, string region, string bucket, bool ssl)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (String.IsNullOrEmpty(region)) throw new ArgumentNullException(nameof(region));
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket)); 
            
            AccessKey = accessKey;
            SecretKey = secretKey;
            Bucket = bucket;
            Ssl = ssl;

            if (!ValidateRegion(region)) throw new ArgumentException("Unable to validate region: " + region);
            Region = GetRegionFromString(region);
        }

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="endpoint">Override the AWS S3 endpoint (if using non-Amazon storage).  Use the form http://localhost:8000/.</param>
        /// <param name="ssl">Enable or disable SSL.</param>
        /// <param name="accessKey">Access key with which to access AWS S3.</param>
        /// <param name="secretKey">Secret key with which to access AWS S3.</param>
        /// <param name="region">AWS region.</param>
        /// <param name="bucket">Bucket in which to store BLOBs.</param>
        /// <param name="baseUrl">Base URL to use for objects, i.e. https://[bucketname].s3.[regionname].amazonaws.com/.  For non-S3 endpoints, use {bucket} and {key} to indicate where these values should be inserted, i.e. http://{bucket}.[hostname]:[port]/{key} or https://[hostname]:[port]/{bucket}/key.</param>
        public IDCHSettings(string endpoint, bool ssl, string accessKey, string secretKey, IDCHRegion region, string bucket, string baseUrl)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));
            
            Endpoint = endpoint;
            Ssl = ssl;
            AccessKey = accessKey;
            SecretKey = secretKey;
            Region = region;
            Bucket = bucket;
            BaseUrl = baseUrl;

            if (!BaseUrl.EndsWith("/")) BaseUrl += "/"; 
        }

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="endpoint">Override the AWS S3 endpoint (if using non-Amazon storage).  Use the form http://localhost:8000/.</param>
        /// <param name="ssl">Enable or disable SSL.</param>
        /// <param name="accessKey">Access key with which to access AWS S3.</param>
        /// <param name="secretKey">Secret key with which to access AWS S3.</param>
        /// <param name="region">AWS region.</param>
        /// <param name="bucket">Bucket in which to store BLOBs.</param>
        /// <param name="baseUrl">Base URL to use for objects, i.e. https://[bucketname].s3.[regionname].amazonaws.com/.  For non-S3 endpoints, use {bucket} and {key} to indicate where these values should be inserted, i.e. http://{bucket}.[hostname]:[port]/{key} or https://[hostname]:[port]/{bucket}/key.</param>
        public IDCHSettings(string endpoint, bool ssl, string accessKey, string secretKey, string region, string bucket, string baseUrl)
        {
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));

            Endpoint = endpoint;
            Ssl = ssl;
            AccessKey = accessKey;
            SecretKey = secretKey;
            Bucket = bucket;
            BaseUrl = baseUrl;

            if (!BaseUrl.EndsWith("/")) BaseUrl += "/";

            if (!ValidateRegion(region)) throw new ArgumentException("Unable to validate region: " + region);
            Region = GetRegionFromString(region);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve AWS region endpoint.
        /// </summary>
        /// <returns>AWS region endpoint.</returns>
        public Amazon.RegionEndpoint GetAwsRegionEndpoint()
        { 
            switch (Region)
            {
                case IDCHRegion.APNortheast1:
                    return Amazon.RegionEndpoint.APNortheast1;
                case IDCHRegion.APNortheast2:
                    return Amazon.RegionEndpoint.APNortheast2;
                case IDCHRegion.APNortheast3:
                    return Amazon.RegionEndpoint.APNortheast3;
                case IDCHRegion.APSouth1:
                    return Amazon.RegionEndpoint.APSouth1;
                case IDCHRegion.APSoutheast1:
                    return Amazon.RegionEndpoint.APSoutheast1;
                case IDCHRegion.APSoutheast2:
                    return Amazon.RegionEndpoint.APSoutheast2;
                case IDCHRegion.CACentral1:
                    return Amazon.RegionEndpoint.CACentral1;
                case IDCHRegion.CNNorth1:
                    return Amazon.RegionEndpoint.CNNorth1;
                case IDCHRegion.EUCentral1:
                    return Amazon.RegionEndpoint.EUCentral1;
                case IDCHRegion.EUNorth1:
                    return Amazon.RegionEndpoint.EUNorth1;
                case IDCHRegion.EUWest1:
                    return Amazon.RegionEndpoint.EUWest1;
                case IDCHRegion.SAEast1:
                    return Amazon.RegionEndpoint.SAEast1;
                case IDCHRegion.USEast1:
                    return Amazon.RegionEndpoint.USEast1;
                case IDCHRegion.USEast2:
                    return Amazon.RegionEndpoint.USEast2;
                case IDCHRegion.USGovCloudEast1:
                    return Amazon.RegionEndpoint.USGovCloudEast1;
                case IDCHRegion.USGovCloudWest1:
                    return Amazon.RegionEndpoint.USGovCloudWest1;
                case IDCHRegion.USWest1:
                    return Amazon.RegionEndpoint.USWest1;
                case IDCHRegion.USWest2:
                    return Amazon.RegionEndpoint.USWest2;
            }

            throw new ArgumentException("Unknown region: " + Region.ToString());
        }

        /// <summary>
        /// Validate a region string.
        /// </summary>
        /// <param name="region">Region.</param>
        /// <returns>True if valid.</returns>
        public bool ValidateRegion(string region)
        {
            return ValidRegions.Contains(region);
        }

        #endregion

        #region Private-Methods

        private List<string> ValidRegions = new List<string>
        {
            "APNortheast1",
            "APNortheast2",
            "APNortheast3",
            "APSoutheast1",
            "APSoutheast2",
            "APSouth1",
            "CACentral1",
            "CNNorth1",
            "EUCentral1",
            "EUNorth1",
            "EUWest1",
            "EUWest2",
            "EUWest3",
            "SAEast1",
            "USEast1",
            "USEast2",
            "USGovCloudEast1",
            "USGovCloudWest1",
            "USWest1",
            "USWest2",
        };

        private IDCHRegion GetRegionFromString(string region)
        {
            switch (region)
            {
                case "APNortheast1":
                    return IDCHRegion.APNortheast1;
                case "APSouth1":
                    return IDCHRegion.APSouth1;
                case "APSoutheast1":
                    return IDCHRegion.APSoutheast1;
                case "APSoutheast2":
                    return IDCHRegion.APSoutheast2;
                case "CACentral1":
                    return IDCHRegion.CACentral1;
                case "CNNorth1":
                    return IDCHRegion.CNNorth1;
                case "EUCentral1":
                    return IDCHRegion.EUCentral1;
                case "EUNorth1":
                    return IDCHRegion.EUNorth1;
                case "EUWest1":
                    return IDCHRegion.EUWest1;
                case "EUWest2":
                    return IDCHRegion.EUWest2;
                case "EUWest3":
                    return IDCHRegion.EUWest3;
                case "SAEast1":
                    return IDCHRegion.SAEast1;
                case "USEast1":
                    return IDCHRegion.USEast1;
                case "USEast2":
                    return IDCHRegion.USEast2;
                case "USGovCloudEast1":
                    return IDCHRegion.USGovCloudEast1;
                case "USGovCloudWest1":
                    return IDCHRegion.USGovCloudWest1;
                case "USWest1":
                    return IDCHRegion.USWest1;
                case "USWest2":
                    return IDCHRegion.USWest2;
                default:
                    throw new ArgumentException("Unknown region: " + region);
            }
        }

        #endregion
    }
}
