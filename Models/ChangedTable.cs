using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatermelonDB.Net.Models
{
    public class ChangedTable<T>
    {
        public List<T> Created { get; set; }
        public List<T> Updated { get; set; }
        public List<T> Deleted { get; set; }

        public ChangedTable(List<T> created, List<T> updated, List<T> deleted)
        {
            Created = created;
            Updated = updated;
            Deleted = deleted;
        }
    }
}
