using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WatermelonDB.Net.DbModels
{
    public interface IWatermelonDBModel<in T> where T : class
    {
        public Guid Id { get; set; }
        [JsonPropertyName("last_modified")]
        public long LastModifiedAt { get; set; }
        public long CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public long? DeletedAt { get; set; }

        public void Update(T newData);
    }
}
