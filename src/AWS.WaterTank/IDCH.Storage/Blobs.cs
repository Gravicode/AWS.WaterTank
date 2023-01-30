using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace IDCH.Storage
{
    /// <summary>
    /// BLOB storage client.
    /// </summary>
    public class Blobs
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private StorageType _StorageType = StorageType.Disk;
        private IDCHSettings _AwsSettings = null;
        private DiskSettings _DiskSettings = null;

        private AmazonS3Config _S3Config = null;
        private IAmazonS3 _S3Client = null;
        private Amazon.Runtime.BasicAWSCredentials _S3Credentials = null;
        private Amazon.RegionEndpoint _S3Region = null;

        #endregion

        #region Constructors-and-Factories

        private Blobs()
        {
            throw new NotImplementedException("Use a StorageType specific constructor.");
        }


        /// <summary>
        /// Instantiate the object for AWS S3 strorage.
        /// </summary>
        /// <param name="config">Storage configuration.</param>
        public Blobs(IDCHSettings config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _AwsSettings = config;
            _StorageType = StorageType.IDCH;
            InitializeClients();
        }

        /// <summary>
        /// Instantiate the object for disk strorage.
        /// </summary>
        /// <param name="config">Storage configuration.</param>
        public Blobs(DiskSettings config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _DiskSettings = config;
            _StorageType = StorageType.Disk;
            InitializeClients();
        }



        #endregion

        #region Public-Methods

        /// <summary>
        /// Delete a BLOB by its key.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param>
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns>True if successful.</returns>
        public Task Delete(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3Delete(key, token);
                    case StorageType.Disk:
                        return DiskDelete(key, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve a BLOB.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param> 
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns>Byte data of the BLOB.</returns>
        public Task<byte[]> Get(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3Get(key, token);
                    case StorageType.Disk:
                        return DiskGet(key, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve a BLOB.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param> 
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns>BLOB data.</returns>
        public Task<BlobData> GetStream(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3GetStream(key, token);
                    case StorageType.Disk:
                        return DiskGetStream(key, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Write a BLOB using a string.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param>
        /// <param name="contentType">Content-type of the object.</param>
        /// <param name="data">BLOB data.</param>
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns></returns>
        public Task Write(string key, string contentType, string data, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            return Write(key, contentType, Encoding.UTF8.GetBytes(data), token);
        }

        /// <summary>
        /// Write a BLOB using a byte array.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param>
        /// <param name="contentType">Content-type of the object.</param>
        /// <param name="data">BLOB data.</param> 
        /// <param name="token">Cancellation token to cancel the request.</param>
        public Task Write(
            string key,
            string contentType,
            byte[] data,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3Write(key, contentType, data, token);
                    case StorageType.Disk:
                        return DiskWrite(key, data, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Write a BLOB using a stream.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param>
        /// <param name="contentType">Content type.</param>
        /// <param name="contentLength">Content length.</param>
        /// <param name="stream">Stream containing the data.</param> 
        /// <param name="token">Cancellation token to cancel the request.</param>
        public Task Write(
            string key,
            string contentType,
            long contentLength,
            Stream stream,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new IOException("Cannot read from supplied stream.");

            if (stream.CanSeek && stream.Length == stream.Position) stream.Seek(0, SeekOrigin.Begin);

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3Write(key, contentType, contentLength, stream, token);
                    case StorageType.Disk:
                        return DiskWrite(key, contentLength, stream, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Check if a BLOB exists.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param>
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns>True if exists.</returns>
        public Task<bool> Exists(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3Exists(key, token);
                    case StorageType.Disk:
                        return DiskExists(key, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return Task.FromResult(false);
            }
            catch (OperationCanceledException)
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Generate a URL for a given object key.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns>URL.</returns>
        public string GenerateUrl(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            switch (_StorageType)
            {
                case StorageType.IDCH:
                    return S3GenerateUrl(_AwsSettings.Bucket, key);
                case StorageType.Disk:
                    return DiskGenerateUrl(key);
                default:
                    throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
            }
        }

        /// <summary>
        /// Retrieve BLOB metadata.
        /// </summary>
        /// <param name="key">Key of the BLOB.</param> 
        /// <param name="token">Cancellation token to cancel the request.</param>
        /// <returns>BLOB metadata.</returns>
        public Task<BlobMetadata> GetMetadata(string key, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3GetMetadata(key, token);
                    case StorageType.Disk:
                        return DiskGetMetadata(key, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Enumerate BLOBs.
        /// </summary>
        /// <param name="prefix">Key prefix that must match.</param>
        /// <param name="continuationToken">Continuation token to use if issuing a subsequent enumeration request.</param>
        /// <param name="token">Cancellation token to cancel the request.</param> 
        /// <returns>Enumeration result.</returns>
        public Task<EnumerationResult> Enumerate(string prefix = null, string continuationToken = null, CancellationToken token = default)
        {
            try
            {
                switch (_StorageType)
                {
                    case StorageType.IDCH:
                        return S3Enumerate(prefix, continuationToken, token);
                    case StorageType.Disk:
                        return DiskEnumerate(prefix, continuationToken, token);
                    default:
                        throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        #endregion

        #region Private-Methods

        private void InitializeClients()
        {
            switch (_StorageType)
            {
                case StorageType.IDCH:
                    _S3Credentials = new Amazon.Runtime.BasicAWSCredentials(_AwsSettings.AccessKey, _AwsSettings.SecretKey);

                    if (String.IsNullOrEmpty(_AwsSettings.Endpoint))
                    {
                        _S3Region = _AwsSettings.GetAwsRegionEndpoint();
                        _S3Config = new AmazonS3Config
                        {
                            RegionEndpoint = _S3Region,
                            UseHttp = !_AwsSettings.Ssl,
                        };

                        // _S3Client = new AmazonS3Client(_S3Credentials, _S3Region);
                        _S3Client = new AmazonS3Client(_S3Credentials, _S3Config);
                    }
                    else
                    {
                        _S3Config = new AmazonS3Config
                        {
                            ServiceURL = _AwsSettings.Endpoint,
                            ForcePathStyle = true,
                            UseHttp = !_AwsSettings.Ssl
                        };

                        _S3Client = new AmazonS3Client(_S3Credentials, _S3Config);
                    }
                    break;
                case StorageType.Disk:
                    if (!Directory.Exists(_DiskSettings.Directory)) Directory.CreateDirectory(_DiskSettings.Directory);
                    break;
                default:
                    throw new ArgumentException("Unknown storage type: " + _StorageType.ToString());
            }
        }

        #region Delete


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task DiskDelete(string key, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string filename = DiskGenerateUrl(key);
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            else if (Directory.Exists(filename))
            {
                Directory.Delete(filename);
            }
            else
            {
                throw new FileNotFoundException("Could not find file '" + key + "'.");
            }
        }

        private async Task S3Delete(string key, CancellationToken token)
        {
            DeleteObjectRequest request = new DeleteObjectRequest
            {
                BucketName = _AwsSettings.Bucket,
                Key = key
            };

            DeleteObjectResponse response = await _S3Client.DeleteObjectAsync(request, token).ConfigureAwait(false);
        }


        #endregion

        #region Get


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<byte[]> DiskGet(string key, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string filename = DiskGenerateUrl(key);
            if (Directory.Exists(filename))
            {
                return new byte[0];
            }
            else if (File.Exists(filename))
            {
                return File.ReadAllBytes(filename);
            }
            else
            {
                throw new FileNotFoundException("Could not find file '" + key + "'.");
            }
        }

        private async Task<byte[]> S3Get(string key, CancellationToken token)
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = _AwsSettings.Bucket,
                Key = key,
            };

            using (GetObjectResponse response = await _S3Client.GetObjectAsync(request, token).ConfigureAwait(false))
            using (Stream responseStream = response.ResponseStream)
            using (StreamReader reader = new StreamReader(responseStream))
            {
                if (response.ContentLength > 0)
                {
                    // first copy the stream
                    byte[] data = new byte[response.ContentLength];

                    Stream bodyStream = response.ResponseStream;
                    data = Common.StreamToBytes(bodyStream);

                    int statusCode = (int)response.HttpStatusCode;
                    return data;
                }
                else
                {
                    throw new IOException("Unable to read object.");
                }
            }
        }



        #endregion

        #region Get-Stream



#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<BlobData> DiskGetStream(string key, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string filename = DiskGenerateUrl(key);
            if (File.Exists(filename))
            {
                long contentLength = new FileInfo(filename).Length;
                FileStream stream = new FileStream(filename, FileMode.Open);
                return new BlobData(contentLength, stream);
            }
            else if (Directory.Exists(filename))
            {
                return new BlobData(0, new MemoryStream());
            }
            else
            {
                throw new FileNotFoundException("Could not find file '" + key + "'.");
            }
        }

        private async Task<BlobData> S3GetStream(string key, CancellationToken token)
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = _AwsSettings.Bucket,
                Key = key,
            };

            GetObjectResponse response = await _S3Client.GetObjectAsync(request, token).ConfigureAwait(false);
            BlobData ret = new BlobData();

            if (response.ContentLength > 0)
            {
                ret.ContentLength = response.ContentLength;
                ret.Data = response.ResponseStream;
            }
            else
            {
                ret.ContentLength = 0;
                ret.Data = new MemoryStream(new byte[0]);
            }

            return ret;
        }



        #endregion

        #region Exists



#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<bool> DiskExists(string key, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string filename = DiskGenerateUrl(key);
            if (File.Exists(filename))
            {
                return true;
            }
            else if (Directory.Exists(filename))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> S3Exists(string key, CancellationToken token)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest
            {
                BucketName = _AwsSettings.Bucket,
                Key = key
            };

            try
            {
                GetObjectMetadataResponse response = await _S3Client.GetObjectMetadataAsync(request, token).ConfigureAwait(false);
                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                //status wasn't not found, so throw the exception
                throw;
            }
        }



        #endregion

        #region Write



        private async Task DiskWrite(string key, byte[] data, CancellationToken token)
        {
            long contentLength = 0;
            MemoryStream stream = new MemoryStream(new byte[0]);

            if (data != null && data.Length > 0)
            {
                contentLength = data.Length;
                stream = new MemoryStream(data);
                stream.Seek(0, SeekOrigin.Begin);
            }

            await DiskWrite(key, contentLength, stream, token);
        }

        private async Task DiskWrite(string key, long contentLength, Stream stream, CancellationToken token)
        {
            string filename = DiskGenerateUrl(key);

            if (
                (key.EndsWith("\\") || key.EndsWith("/"))
                &&
                contentLength == 0
               )
            {
                Directory.CreateDirectory(filename);
            }
            else
            {
                string dirName = Path.GetDirectoryName(filename);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                int bytesRead = 0;
                long bytesRemaining = contentLength;
                byte[] buffer = new byte[65536];

                using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    while (bytesRemaining > 0)
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                        if (bytesRead > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                            bytesRemaining -= bytesRead;
                        }
                    }
                }
            }
        }

        private async Task S3Write(string key, string contentType, byte[] data, CancellationToken token)
        {
            long contentLength = 0;
            MemoryStream stream = new MemoryStream(new byte[0]);

            if (data != null && data.Length > 0)
            {
                contentLength = data.Length;
                stream = new MemoryStream(data);
                stream.Seek(0, SeekOrigin.Begin);
            }

            await S3Write(key, contentType, contentLength, stream, token).ConfigureAwait(false);
        }

        private async Task S3Write(string key, string contentType, long contentLength, Stream stream, CancellationToken token)
        {
            PutObjectRequest request = new PutObjectRequest();

            if (stream == null || contentLength < 1)
            {
                request.BucketName = _AwsSettings.Bucket;
                request.Key = key;
                request.ContentType = contentType;
                request.UseChunkEncoding = false;
                request.InputStream = new MemoryStream(new byte[0]);
            }
            else
            {
                request.BucketName = _AwsSettings.Bucket;
                request.Key = key;
                request.ContentType = contentType;
                request.UseChunkEncoding = false;
                request.InputStream = stream;
            }

            PutObjectResponse response = await _S3Client.PutObjectAsync(request, token).ConfigureAwait(false);
        }



        #endregion

        #region Get-Metadata



#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<BlobMetadata> DiskGetMetadata(string key, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string filename = DiskGenerateUrl(key);

            if (File.Exists(filename))
            {
                FileInfo fi = new FileInfo(filename);
                BlobMetadata md = new BlobMetadata();
                md.Key = key;
                md.ContentLength = fi.Length;
                md.CreatedUtc = fi.CreationTimeUtc;
                md.LastAccessUtc = fi.LastAccessTimeUtc;
                md.LastUpdateUtc = fi.LastWriteTimeUtc;
                return md;
            }
            else if (Directory.Exists(filename))
            {
                DirectoryInfo di = new DirectoryInfo(filename);
                BlobMetadata md = new BlobMetadata();
                md.Key = key;
                md.ContentLength = 0;
                md.CreatedUtc = di.CreationTimeUtc;
                md.LastAccessUtc = di.LastAccessTimeUtc;
                md.LastUpdateUtc = di.LastWriteTimeUtc;
                return md;
            }
            else
            {
                throw new FileNotFoundException("Could not find file '" + key + "'.");
            }
        }

        private async Task<BlobMetadata> S3GetMetadata(string key, CancellationToken token)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest();
            request.BucketName = _AwsSettings.Bucket;
            request.Key = key;

            GetObjectMetadataResponse response = await _S3Client.GetObjectMetadataAsync(request);

            if (response.ContentLength > 0)
            {
                BlobMetadata md = new BlobMetadata();
                md.Key = key;
                md.ContentLength = response.ContentLength;
                md.ContentType = response.Headers.ContentType;
                md.ETag = response.ETag;
                md.CreatedUtc = response.LastModified;

                if (!String.IsNullOrEmpty(md.ETag))
                {
                    while (md.ETag.Contains("\"")) md.ETag = md.ETag.Replace("\"", "");
                }

                return md;
            }
            else
            {
                throw new KeyNotFoundException("The requested object was not found.");
            }
        }



        #endregion

        #region Enumeration



#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<EnumerationResult> DiskEnumerate(string prefix, string continuationToken, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            int startIndex = 0;
            int count = 1000;

            if (!String.IsNullOrEmpty(continuationToken))
            {
                if (!DiskParseContinuationToken(continuationToken, out startIndex, out count))
                {
                    throw new ArgumentException("Unable to parse continuation token.");
                }
            }

            long maxIndex = startIndex + count;

            long currCount = 0;
            IEnumerable<string> files = null;

            if (!String.IsNullOrEmpty(prefix))
            {
                if (Directory.Exists(_DiskSettings.Directory + prefix))
                {
                    string tempPrefix = prefix;
                    tempPrefix = tempPrefix.Replace("\\", "/");
                    if (!tempPrefix.EndsWith("/")) tempPrefix += "/";
                    files = Directory.EnumerateFiles(_DiskSettings.Directory, tempPrefix + "*", SearchOption.AllDirectories);
                }
                else
                {
                    files = Directory.EnumerateFiles(_DiskSettings.Directory, prefix + "*", SearchOption.AllDirectories);
                }
            }
            else
            {
                files = Directory.EnumerateFiles(_DiskSettings.Directory, "*", SearchOption.AllDirectories);
            }

            files = files.Skip(startIndex).Take(count);

            EnumerationResult ret = new EnumerationResult();
            if (files.Count() < 1) return ret;

            ret.NextContinuationToken = DiskBuildContinuationToken(startIndex + count, count);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);

                string filename = file;
                if (filename.StartsWith(_DiskSettings.Directory)) filename = file.Substring(_DiskSettings.Directory.Length);
                if (!String.IsNullOrEmpty(filename)) filename = filename.Replace("\\", "/");

                BlobMetadata md = new BlobMetadata();
                md.Key = filename;

                md.ContentLength = fi.Length;
                md.CreatedUtc = fi.CreationTimeUtc;
                ret.Blobs.Add(md);

                currCount++;
                continue;
            }

            return ret;
        }

        private async Task<EnumerationResult> S3Enumerate(string prefix, string continuationToken, CancellationToken token)
        {
            ListObjectsRequest req = new ListObjectsRequest();
            req.BucketName = _AwsSettings.Bucket;
            if (!String.IsNullOrEmpty(prefix)) req.Prefix = prefix;

            if (!String.IsNullOrEmpty(continuationToken)) req.Marker = continuationToken;

            ListObjectsResponse resp = await _S3Client.ListObjectsAsync(req, token).ConfigureAwait(false);
            EnumerationResult ret = new EnumerationResult();

            if (resp.S3Objects != null && resp.S3Objects.Count > 0)
            {
                foreach (S3Object curr in resp.S3Objects)
                {
                    BlobMetadata md = new BlobMetadata();
                    md.Key = curr.Key;
                    md.ContentLength = curr.Size;
                    md.ETag = curr.ETag;
                    md.CreatedUtc = curr.LastModified;

                    if (!String.IsNullOrEmpty(md.ETag))
                    {
                        while (md.ETag.Contains("\"")) md.ETag = md.ETag.Replace("\"", "");
                    }

                    ret.Blobs.Add(md);
                }
            }

            if (!String.IsNullOrEmpty(resp.NextMarker)) ret.NextContinuationToken = resp.NextMarker;

            return ret;
        }



        #endregion

        #region Continuation-Tokens


        private bool DiskParseContinuationToken(string continuationToken, out int start, out int count)
        {
            start = -1;
            count = -1;
            if (String.IsNullOrEmpty(continuationToken)) return false;
            byte[] encoded = Convert.FromBase64String(continuationToken);
            string encodedStr = Encoding.UTF8.GetString(encoded);
            string[] parts = encodedStr.Split(' ');
            if (parts.Length != 2) return false;

            if (!Int32.TryParse(parts[0], out start)) return false;
            if (!Int32.TryParse(parts[1], out count)) return false;
            return true;
        }

        private string DiskBuildContinuationToken(int start, int count)
        {
            if (start >= count) return null;
            string ret = start.ToString() + " " + count.ToString();
            byte[] retBytes = Encoding.UTF8.GetBytes(ret);
            return Convert.ToBase64String(retBytes);
        }


        #endregion

        #region URL


        private string DiskGenerateUrl(string key)
        {
            string dir = _DiskSettings.Directory;
            dir = dir.Replace("\\", "/");
            dir = dir.Replace("//", "/");
            while (dir.EndsWith("/")) dir = dir.Substring(0, dir.Length - 1);
            return dir + "/" + key;
        }

        private string S3GenerateUrl(string bucket, string key)
        {
            if (!String.IsNullOrEmpty(_AwsSettings.BaseUrl))
            {
                string url = _AwsSettings.BaseUrl;
                url = url.Replace("{bucket}", bucket);
                url = url.Replace("{key}", key);
                return url;
            }
            else
            {
                string ret = "";

                // https://[bucketname].s3.[regionname].amazonaws.com/
                if (_AwsSettings.Ssl) ret = "https://";
                else ret = "http://";

                ret += bucket + ".s3." + S3RegionToString(_AwsSettings.Region) + ".amazonaws.com/" + key;

                return ret;
            }
        }



        private string S3RegionToString(IDCHRegion region)
        {
            switch (region)
            {
                case IDCHRegion.APNortheast1:
                    return "ap-northeast-1";
                case IDCHRegion.APNortheast2:
                    return "ap-northeast-2";
                case IDCHRegion.APNortheast3:
                    return "ap-northeast-3";
                case IDCHRegion.APSouth1:
                    return "ap-south-1";
                case IDCHRegion.APSoutheast1:
                    return "ap-southeast-1";
                case IDCHRegion.APSoutheast2:
                    return "ap-southeast-2";
                case IDCHRegion.CACentral1:
                    return "ca-central-1";
                case IDCHRegion.CNNorth1:
                    return "cn-north-1";
                case IDCHRegion.CNNorthwest1:
                    return "cn-northwest-1";
                case IDCHRegion.EUCentral1:
                    return "eu-central-1";
                case IDCHRegion.EUNorth1:
                    return "eu-north-1";
                case IDCHRegion.EUWest1:
                    return "eu-west-1";
                case IDCHRegion.EUWest2:
                    return "eu-west-2";
                case IDCHRegion.EUWest3:
                    return "eu-west-3";
                case IDCHRegion.SAEast1:
                    return "sa-east-1";
                case IDCHRegion.USEast1:
                    return "us-east-1";
                case IDCHRegion.USEast2:
                    return "us-east-2";
                case IDCHRegion.USGovCloudEast1:
                    return "us-gov-east-1";
                case IDCHRegion.USGovCloudWest1:
                    return "us-gov-west-1";
                case IDCHRegion.USWest1:
                    return "us-west-1";
                case IDCHRegion.USWest2:
                    return "us-west-2";
                default:
                    throw new ArgumentException("Unknown region: " + region.ToString());
            }
        }


        #endregion

        #endregion
    }
}
