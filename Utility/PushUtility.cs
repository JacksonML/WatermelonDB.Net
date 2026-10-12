using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            Expression<Func<T, bool>>? updatePrecheck = null, 
            Expression<Func<T, bool>>? deletePrecheck = null)
            where T : class, IWatermelonDBModel<T>, new()
        {
            if (changes == null) return;

            updatePrecheck ??= o => true;
            deletePrecheck ??= o => true;

            var pushedIds = changes.Created.Select(o => o.Id)
                .Concat(changes.Updated.Select(o => o.Id))
                .Distinct()
                .ToList();
            var existingRows = pushedIds.Count == 0
                ? new Dictionary<Guid, T>()
                : dbSet.Where(o => pushedIds.Contains(o.Id)).ToDictionary(o => o.Id);
            var accessibleRows = pushedIds.Count == 0
                ? new HashSet<Guid>()
                : dbSet.Where(o => pushedIds.Contains(o.Id)).Where(updatePrecheck).Select(o => o.Id).ToHashSet();

            foreach (var created in changes.Created)
            {
                if (existingRows.TryGetValue(created.Id, out var existing))
                {
                    if (!accessibleRows.Contains(created.Id)) continue;
                    existing.Update(created);
                    existing.LastModifiedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                    continue;
                }

                created.CreatedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                created.LastModifiedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                onCreate?.Invoke(created);
            }
            dbSet.AddRange(changes.Created.Where(o => !existingRows.ContainsKey(o.Id)));

            foreach (var updated in changes.Updated)
            {
                if (!existingRows.TryGetValue(updated.Id, out var existing) || !accessibleRows.Contains(updated.Id)) continue;
                // todo, need to convert LastModifiedAt to epoch seconds
                // if (existing.LastModifiedAt > updated.LastModifiedAt) throw new Exception(); // todo, better bubble up

                existing.Update(updated);
                existing.LastModifiedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
            }

            var deletedIds = changes.Deleted.Select(d => Guid.Parse(d)).Distinct().ToList();
            var accessibleDeletes = deletedIds.Count == 0
                ? new Dictionary<Guid, T>()
                : dbSet.Where(o => deletedIds.Contains(o.Id)).Where(deletePrecheck).ToDictionary(o => o.Id);
            foreach (var deleted in deletedIds)
            {
                accessibleDeletes.TryGetValue(deleted, out var existing);

                // A record created and deleted on the device between syncs never reached the
                // server, so no other device has it and it needs no tombstone. Inserting one
                // with only an Id broke required foreign keys (e.g. a CookingSession's
                // RecipeId) and failed every later push from that user.
                if (existing == null) continue;

                if (existing.IsDeleted == false)
                {
                    existing.DeletedAt = DateTime.UtcNow.ToLinuxEpochSeconds();
                    existing.IsDeleted = true;
                }
            }
        }
    }
}
