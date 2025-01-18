using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatermelonDB.Net.Models
{
    public class ChangesCollection<T>
    {
        public ChangesCollection()
        {
            Timesstamp = DateTime.UtcNow;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public T Changes { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public DateTime Timesstamp { get; set; }
    }
}
