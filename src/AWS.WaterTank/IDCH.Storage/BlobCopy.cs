using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDCH.Storage
{
    /// <summary>
    /// BLOB copy.
    /// </summary>
    public class BlobCopy
    {
        #region Public-Members

        /// <summary>
        /// Method to invoke to send log messages.
        /// </summary>
        public Action<string> Logger { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Header = "[BlobCopy] ";
        private object _CopyFrom = null;
        private object _CopyTo = null;
        private StorageType _CopyFromStorageType = StorageType.Disk;
        private StorageType _CopyToStorageType = StorageType.Disk;
        private string _Prefix = null;

        private Blobs _From = null;
        private Blobs _To = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="copyFrom">Settings of the repository from which objects should be copied.</param>
        /// <param name="copyTo">Settings of the repository to which objects should be copied.</param>
        /// <param name="prefix">Prefix of the objects that should be copied.</param>
        public BlobCopy(Blobs copyFrom, Blobs copyTo, string prefix = null)
        {
            if (copyFrom == null) throw new ArgumentNullException(nameof(copyFrom));
            if (copyTo == null) throw new ArgumentNullException(nameof(copyTo));

            _From = copyFrom;
            _To = copyTo;
            _Prefix = prefix;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="copyFrom">Settings of the repository from which objects should be copied.</param>
        /// <param name="copyTo">Settings of the repository to which objects should be copied.</param>
        /// <param name="prefix">Prefix of the objects that should be copied.</param>
        public BlobCopy(BlobSettings copyFrom, BlobSettings copyTo, string prefix = null)
        {
            if (copyFrom == null) throw new ArgumentNullException(nameof(copyFrom));
            if (copyTo == null) throw new ArgumentNullException(nameof(copyTo));

            _CopyFrom = copyFrom;
            _CopyTo = copyTo;

            _CopyFromStorageType = GetStorageType(_CopyFrom);
            _CopyToStorageType = GetStorageType(_CopyTo);

            switch (_CopyFromStorageType)
            {
                case StorageType.IDCH:
                    _From = new Blobs((IDCHSettings)_CopyFrom);
                    break;
               
                case StorageType.Disk:
                    _From = new Blobs((DiskSettings)_CopyFrom);
                    break;
                default:
                    throw new ArgumentException("Unknown storage type in 'copyFrom'.");
            }

            switch (_CopyToStorageType)
            {
                case StorageType.IDCH:
                    _To = new Blobs((IDCHSettings)_CopyTo);
                    break;
               
                case StorageType.Disk:
                    _To = new Blobs((DiskSettings)_CopyTo);
                    break;
                default:
                    throw new ArgumentException("Unknown storage type in 'copyTo'.");
            }

            _Prefix = prefix;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the copy operation.
        /// </summary>
        /// <param name="stopAfter">Stop after this many objects have been copied.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Copy statistics.</returns>
        public async Task<CopyStatistics> Start(int stopAfter = -1, CancellationToken token = default)
        {
            if (stopAfter < -1 || stopAfter == 0) throw new ArgumentException("Value for stopAfter must be -1 or a positive integer.");

            CopyStatistics ret = new CopyStatistics();
            ret.Time.Start = DateTime.Now;

            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested) break;

                    string continuationToken = null;
                    EnumerationResult enumResult = await _From.Enumerate(_Prefix, continuationToken, token);
                    if (enumResult == null)
                    {
                        Log("no enumeration resource from source");
                        break;
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(enumResult.NextContinuationToken)) continuationToken = enumResult.NextContinuationToken;

                        ret.BlobsEnumerated += enumResult.Count;
                        ret.BytesEnumerated += enumResult.Bytes;

                        if (enumResult.Blobs != null && enumResult.Blobs.Count > 0)
                        {
                            bool maxCopiesReached = false;

                            foreach (BlobMetadata blob in enumResult.Blobs)
                            {
                                byte[] blobData = await _From.Get(blob.Key, token);

                                ret.BlobsRead += 1;
                                ret.BytesRead += blobData.Length;

                                await _To.Write(blob.Key, blob.ContentType, blobData, token);

                                ret.BlobsWritten += 1;
                                ret.BytesWritten += blobData.Length;
                                ret.Keys.Add(blob.Key);

                                if (stopAfter != -1)
                                {
                                    if (ret.BlobsWritten >= stopAfter)
                                    {
                                        maxCopiesReached = true;
                                        break;
                                    }
                                }
                            }

                            if (maxCopiesReached)
                            {
                                break;
                            }
                        }

                        if (!enumResult.HasMore) break;
                    }
                }

                ret.Success = true;
            }
            catch (Exception e)
            {
                ret.Success = false;
                ret.Exception = e;
            }
            finally
            {
                ret.Time.End = DateTime.Now;
            }

            return ret;
        }

        #endregion

        #region Private-Methods

        private bool IsSettings(object val)
        {
            if (val is IDCHSettings
                || val is DiskSettings
               )
            {
                return true;
            }

            return false;
        }

        private StorageType GetStorageType(object val)
        {
            if (val is IDCHSettings) return StorageType.IDCH;
            else if (val is DiskSettings) return StorageType.Disk;
            else throw new ArgumentException("Unknown storage type.");
        }

        private void Log(string msg)
        {
            if (String.IsNullOrEmpty(msg)) return;
            Logger?.Invoke(_Header + msg);
        }

        #endregion
    }
}
