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
        public static ChangedTable<T> Pull<T>(DbSet<T> dbSet, long lastPulledAt, Func<T, bool>? precheck) where T : class, DbModels.IWatermelonDBModel<T>
        {
            precheck ??= (o) => true;
            
            var created = dbSet.Where(precheck).Where(o => o.CreatedAt > lastPulledAt && o.IsDeleted == false).ToList();
            var updated = dbSet.Where(precheck).Where(o => o.LastModifiedAt > lastPulledAt && o.CreatedAt <= lastPulledAt && o.IsDeleted == false).ToList();
            var deleted = dbSet.Where(precheck).Where(o => o.IsDeleted == true && o.DeletedAt > lastPulledAt).Select(o => o.Id.ToString()).ToList();

            var changes = new ChangedTable<T>(created, updated, deleted);

            return changes;
        }
    }
}
