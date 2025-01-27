using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpiceRackAPI.Utilities;

namespace WatermelonDB.Net.Models
{
    public class ChangesCollection<T> where T : new()
    {
        public ChangesCollection()
        {
            timestamp = 1;
            changes = new T();
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public T changes { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public long timestamp { get; set; }
    }
}
