using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatermelonDB.Net.DbModels
{
    public interface IWatermelonDBModel<T>
    {
        public Guid Id { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Update(T newData);
    }
}
