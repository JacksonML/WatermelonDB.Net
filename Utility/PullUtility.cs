using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using WatermelonDB.Net.Models;

namespace WatermelonDB.Net.Utility
{
    public static class PullUtility
    {
        // precheck is an Expression so EF Core runs it in SQL. A Func<T, bool> binds to
        // Enumerable.Where and loads the whole table into memory.
        public static ChangedTable<T> Pull<T>(DbSet<T> dbSet, long lastPulledAt, Expression<Func<T, bool>>? precheck) where T : class, DbModels.IWatermelonDBModel<T>
        {
            IQueryable<T> rows = precheck == null ? dbSet : dbSet.Where(precheck);

            var created = rows.Where(o => o.CreatedAt > lastPulledAt && o.IsDeleted == false).ToList();
            var updated = rows.Where(o => o.LastModifiedAt > lastPulledAt && o.CreatedAt <= lastPulledAt && o.IsDeleted == false).ToList();
            // Format the ids in .NET, not SQL: SQLite stores Guids as upper-case text, and WatermelonDB
            // ids are lower-case, so a SQL-side ToString sent deletions the app never matched.
            var deleted = rows.Where(o => o.IsDeleted == true && o.DeletedAt > lastPulledAt).Select(o => o.Id).ToList().Select(id => id.ToString()).ToList();

            var changes = new ChangedTable<T>(created, updated, deleted);

            return changes;
        }
    }
}
