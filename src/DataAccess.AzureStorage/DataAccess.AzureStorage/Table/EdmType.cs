using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Table
{
    public enum EdmType
    {
        //
        // Summary:
        //     Represents fixed- or variable-length character data.
        String,
        //
        // Summary:
        //     Represents fixed- or variable-length binary data.
        Binary,
        //
        // Summary:
        //     Represents the mathematical concept of binary-valued logic.
        Boolean,
        //
        // Summary:
        //     Represents date and time.
        DateTime,
        //
        // Summary:
        //     Represents a floating point number with 15 digits precision that can represent
        //     values with approximate range of +/- 2.23e -308 through +/- 1.79e +308.
        Double,
        //
        // Summary:
        //     Represents a 16-byte (128-bit) unique identifier value.
        Guid,
        //
        // Summary:
        //     Represents a signed 32-bit integer value.
        Int32,
        //
        // Summary:
        //     Represents a signed 64-bit integer value.
        Int64
    }
}
