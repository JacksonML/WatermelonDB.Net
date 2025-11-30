using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpiceRackAPI.Utilities;
using WatermelonDB.Net.DbModels;
using WatermelonDB.Net.Models;

namespace WatermelonDB.Net.Utility
{
    public static class PushUtility
    {

        public static void Push<T>(DbSet<T> dbSet, 
            ChangedTable<T>? changes, 
            Action<T>? onCreate = null, 
            Func<T, bool>? updatePrecheck = null, 
            Func<T, bool>? deletePrecheck = null)
            where T : class, IWatermelonDBModel<T>, new()
        {
            if (changes == null) return;

            updatePrecheck ??= o => true;
            deletePrecheck ??= o => true;

            foreach (var created in changes.Created)
            {
                // Double check for existing
                var existing = dbSet.Where(updatePrecheck).FirstOrDefault(o => o.Id == created.Id);
                if (existing != null)
                {
                    dbSet.Remove(existing);
                }

                created.CreatedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                created.LastModifiedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                onCreate?.Invoke(created);
            }
            dbSet.AddRange(changes.Created);

            foreach (var updated in changes.Updated)
            {
                var existing = dbSet.Where(updatePrecheck).FirstOrDefault(o => o.Id == updated.Id);
                if (existing == null) continue; // should this be an error?
                // todo, need to convert LastModifiedAt to epoch seconds
                // if (existing.LastModifiedAt > updated.LastModifiedAt) throw new Exception(); // todo, better bubble up

                existing.Update(updated);
                existing.LastModifiedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
            }

            foreach (var deleted in changes.Deleted.Select(d => Guid.Parse(d)))
            {
                var existing = dbSet.Where(deletePrecheck).FirstOrDefault(o => o.Id == deleted);
                if (existing == null)
                {
                    var newObj = new T()
                    {
                        Id = deleted,
                    };
                    try
                    {
                        dbSet.Add(newObj);
                        existing = newObj;
                    }
                    catch
                    {
                        // Todo: handle
                    }
                }

                if (existing?.IsDeleted == false)
                {
                    existing.DeletedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                    existing.IsDeleted = true;
                }
            }
        }
    }
}
