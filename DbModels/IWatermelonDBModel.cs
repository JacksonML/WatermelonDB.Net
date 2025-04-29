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
        [JsonIgnore]
        public long LastModifiedAt { get; set; }
        [JsonIgnore]
        public long CreatedAt { get; set; }
        [JsonIgnore]
        public bool IsDeleted { get; set; }
        [JsonIgnore]
        public long? DeletedAt { get; set; }

        public void Update(T newData);
    }
}
