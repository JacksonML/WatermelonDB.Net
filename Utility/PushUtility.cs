using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;
using SpiceRackAPI.Utilities;
using WatermelonDB.Net.DbModels;
using WatermelonDB.Net.Models;

namespace WatermelonDB.Net.Utility
{
    public static class PushUtility
    {
        private static readonly ConcurrentDictionary<Type, (string Column, PropertyInfo Property)[]> s_columns = new();

        /// <summary>
        /// Copies a pushed update onto the stored row. The device sends its whole copy of the
        /// record, but only <paramref name="changedColumns"/> were edited there; the rest may be
        /// stale, from before another device's edit that this one hasn't pulled yet. Applying the
        /// whole row would undo that edit, so every other column keeps its stored value. Two
        /// devices editing the same column still resolve to the last push. Without a column list
        /// (an older app) the whole row is applied.
        /// </summary>
        public static void ApplyChangedColumns<T>(T existing, T pushed, IReadOnlySet<string>? changedColumns)
            where T : class, IWatermelonDBModel<T>
        {
            if (changedColumns is null || changedColumns.Count == 0)
            {
                existing.Update(pushed);
                return;
            }

            var keep = ColumnsOf(typeof(T)).Where(c => !changedColumns.Contains(c.Column)).ToArray();
            var stored = keep.Select(c => c.Property.GetValue(existing)).ToArray();
            existing.Update(pushed);
            for (var i = 0; i < keep.Length; i++)
            {
                keep[i].Property.SetValue(existing, stored[i]);
            }
        }

        // Synced columns are the properties the JSON carries, under their JSON (WatermelonDB) names.
        private static (string Column, PropertyInfo Property)[] ColumnsOf(Type type) =>
            s_columns.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is not { Condition: JsonIgnoreCondition.Always })
                .Select(p => (Column: p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name), Property: p))
                .Where(c => c.Column != "id")
                .ToArray());


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

                ApplyChangedColumns(existing, updated, changes.ChangedColumnsOf(updated));
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
