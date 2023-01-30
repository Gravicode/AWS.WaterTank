using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IDCH.Storage
{
    /// <summary>
    /// Storage type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StorageType
    {
        /// <summary>
        /// Amazon Simple Storage Service.
        /// </summary>
        [EnumMember(Value = "IDCH")]
        IDCH,
       
        /// <summary>
        /// Local filesystem/disk storage.
        /// </summary>
        [EnumMember(Value = "Disk")]
        Disk
      
    }
}
