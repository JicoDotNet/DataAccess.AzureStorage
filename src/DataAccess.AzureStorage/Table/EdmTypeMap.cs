using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    /// <summary>
    /// Single source of truth for which CLR types map to a supported Azure
    /// Table Storage EDM type. Used both by DynamicTableEntity (to decide what
    /// belongs in its Properties bag) and by AzureTableAccess's generic entity
    /// mapper (to decide which properties on a strongly-typed entity can
    /// actually be sent to Table Storage).
    /// </summary>
    internal static class EdmTypeMap
    {
        private static readonly IReadOnlyDictionary<EdmType, Type[]> EdmTypeToClrTypes =
            new Dictionary<EdmType, Type[]>
            {
                [EdmType.String] = new[] { typeof(string) },
                [EdmType.Binary] = new[] { typeof(byte[]) },
                [EdmType.Boolean] = new[] { typeof(bool) },
                [EdmType.DateTimeOffset] = new[] { typeof(DateTimeOffset) },
                [EdmType.DateTime] = new[] { typeof(DateTime) },
                [EdmType.Double] = new[] { typeof(double) },
                [EdmType.Guid] = new[] { typeof(Guid) },
                [EdmType.Int32] = new[] { typeof(int) },
                [EdmType.Int64] = new[] { typeof(long) }
            };

        private static readonly HashSet<Type> SupportedClrTypes =
            new HashSet<Type>(EdmTypeToClrTypes.Values.SelectMany(types => types));

        /// <summary>
        /// True if Table Storage can represent this CLR type as a column —
        /// unwraps Nullable&lt;T&gt; first, so DateTime?, int?, etc. are
        /// correctly recognized as supported, matching how ExpiryDate already
        /// works today.
        /// </summary>
        public static bool IsSupportedClrType(Type type)
        {
            Type underlying = Nullable.GetUnderlyingType(type) ?? type;
            return SupportedClrTypes.Contains(underlying);
        }
    }
}