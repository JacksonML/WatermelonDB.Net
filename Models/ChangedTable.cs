using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WatermelonDB.Net.Models
{
    [JsonConverter(typeof(ChangedTableConverterFactory))]
    public class ChangedTable<T>
    {
        public List<T> Created { get; set; }
        public List<T> Updated { get; set; }
        public List<string> Deleted { get; set; }

        /// <summary>
        /// For pushed records, the columns the device changed since it last synced (WatermelonDB's
        /// <c>_changed</c>), keyed by the record object. A record without an entry is applied whole.
        /// </summary>
        [JsonIgnore]
        public Dictionary<T, IReadOnlySet<string>> ChangedColumns { get; } = new(ReferenceEqualityComparer.Instance as IEqualityComparer<T>);

        public ChangedTable(List<T> created, List<T> updated, List<string> deleted)
        {
            Created = created;
            Updated = updated;
            Deleted = deleted;
        }

        public IReadOnlySet<string>? ChangedColumnsOf(T record) =>
            ChangedColumns.TryGetValue(record, out var columns) ? columns : null;
    }

    /// <summary>
    /// Reads WatermelonDB's per-record <c>_changed</c> list, which the typed models don't carry,
    /// into <see cref="ChangedTable{T}.ChangedColumns"/>. Writes the table as before.
    /// </summary>
    public sealed class ChangedTableConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ChangedTable<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(ChangedTableConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

        private sealed class ChangedTableConverter<T> : JsonConverter<ChangedTable<T>>
        {
            public override ChangedTable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                using var document = JsonDocument.ParseValue(ref reader);
                var table = new ChangedTable<T>(new List<T>(), new List<T>(), new List<string>());

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array) continue;
                    if (Is(property.Name, "created")) ReadRecords(property.Value, table.Created, table, options);
                    else if (Is(property.Name, "updated")) ReadRecords(property.Value, table.Updated, table, options);
                    else if (Is(property.Name, "deleted"))
                        table.Deleted.AddRange(property.Value.EnumerateArray().Select(id => id.GetString()).OfType<string>());
                }
                return table;
            }

            public override void Write(Utf8JsonWriter writer, ChangedTable<T> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(Name("Created", options));
                JsonSerializer.Serialize(writer, value.Created, options);
                writer.WritePropertyName(Name("Updated", options));
                JsonSerializer.Serialize(writer, value.Updated, options);
                writer.WritePropertyName(Name("Deleted", options));
                JsonSerializer.Serialize(writer, value.Deleted, options);
                writer.WriteEndObject();
            }

            private static void ReadRecords(JsonElement array, List<T> into, ChangedTable<T> table, JsonSerializerOptions options)
            {
                foreach (var element in array.EnumerateArray())
                {
                    var record = element.Deserialize<T>(options);
                    if (record is null) continue;
                    into.Add(record);
                    if (element.ValueKind == JsonValueKind.Object
                        && element.TryGetProperty("_changed", out var changed)
                        && changed.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(changed.GetString()))
                    {
                        table.ChangedColumns[record] = changed.GetString()!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToHashSet(StringComparer.Ordinal);
                    }
                }
            }

            private static bool Is(string name, string expected) => string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

            private static string Name(string name, JsonSerializerOptions options) =>
                options.PropertyNamingPolicy?.ConvertName(name) ?? name;
        }
    }
}
