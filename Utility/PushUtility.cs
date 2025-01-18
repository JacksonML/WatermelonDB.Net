using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatermelonDB.Net.DbModels;
using WatermelonDB.Net.Models;

namespace WatermelonDB.Net.Utility
{
    public static class PushUtility
    {
        public static void Push<T>(DbSet<T> dbSet, ChangedTable<T>? changes) where T : class, IWatermelonDBModel<T>
        {
            if (changes == null) return;

            dbSet.AddRange(changes.Created);

            foreach (var updated in changes.Updated)
            {
                var existing = dbSet.Find(updated.Id);
                if (existing == null) continue; // should this be an error?
                if (existing.LastModifiedAt > updated.LastModifiedAt) throw new Exception(); // todo, better bubble up

                existing.Update(updated);
                existing.LastModifiedAt = DateTime.UtcNow;
            }

            foreach (var deleted in changes.Deleted)
            {
                var existing = dbSet.Find(deleted.Id);
                if (existing == null) continue; // should this be an error?

                if (existing.IsDeleted == false)
                {
                    existing.DeletedAt = DateTime.UtcNow;
                    existing.IsDeleted = true;
                }
            }
        }
    }
}
