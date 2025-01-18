using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatermelonDB.Net.Models;

namespace WatermelonDB.Net.Utility
{
    public static class PullUtility
    {
        public static ChangedTable<T> Pull<T>(DbSet<T> dbSet, DateTime lastPulledAt) where T : class, DbModels.IWatermelonDBModel<T>
        {
            var created = dbSet.Where(o => o.CreatedAt > lastPulledAt).ToList();
            var updated = dbSet.Where(o => o.LastModifiedAt > lastPulledAt && o.CreatedAt <= lastPulledAt).ToList();
            var deleted = dbSet.Where(o => o.IsDeleted == true && o.DeletedAt > lastPulledAt).ToList();

            var changes = new ChangedTable<T>(created, updated, deleted);

            return changes;
        }
    }
}
