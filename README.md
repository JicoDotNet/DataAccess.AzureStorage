[![CI Pipeline](https://github.com/JicoDotNet/DataAccess.AzureStorage/actions/workflows/build.yml/badge.svg)](https://github.com/JicoDotNet/DataAccess.AzureStorage/actions/workflows/build.yml)

![GitHub repo size](https://img.shields.io/github/repo-size/JicoDotNet/DataAccess.AzureStorage)
![GitHub stars](https://img.shields.io/github/stars/JicoDotNet/DataAccess.AzureStorage?style=social)
![GitHub license](https://img.shields.io/github/license/JicoDotNet/DataAccess.AzureStorage)
![Contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen)  

# DataAccess.AzureStorage

A small, dependency-light data access layer over **Azure Table Storage**, built on top of the official
[`Azure.Data.Tables`](https://www.nuget.org/packages/Azure.Data.Tables) SDK. It targets **.NET Standard 2.0 / 2.1**,
so it can be consumed from .NET Framework, .NET Core, and modern .NET projects alike.

This document covers the `DataAccess.AzureStorage.Table` namespace specifically — table name and connection
string are supplied by the caller, and everything else (table creation, entity CRUD, optimistic concurrency,
schema-less rows) is handled for you.

```csharp
var tableAccess = new AzureTableAccess("MyTable", "<connection string>");
tableAccess.InsertEntity(myEntity);
tableAccess.ReplaceEntity(myEntity);
```

---

## Requirements

- .NET Standard 2.0 or 2.1 compatible runtime (.NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+, etc.)
- NuGet package: `Azure.Data.Tables` (v12.10.0 or compatible)
- An Azure Storage account connection string (or the Azurite emulator connection string for local dev)

---

## Architecture

```
AzureManager                    (abstract) — holds & validates the connection string
    └── AzureTableManager       (abstract) — owns TableServiceClient / TableClient, table name validation & creation
            └── AzureTableAccess (sealed)  — implements IAzureTableAccess; all CRUD + query operations

IAzureTableAccess                — public contract; program against this for testability/DI
TableEntity                      — abstract base for your own entity classes (implements ITableEntity)
IDynamicTableEntity / DynamicTableEntity
                                  — schema-less entity for when the row shape isn't known at compile time
EdmType                          — enum of the Azure Table Storage EDM types DynamicTableEntity accepts
```

| Type | Role |
|---|---|
| `AzureManager` | Validates and stores the connection string. Base for any future Azure service manager (Table, Blob, etc.). |
| `AzureTableManager` | Creates the `TableServiceClient`, validates table names against Azure's naming rules, creates the table on first use, and guards every operation with `EnsureTableReady()`. |
| `AzureTableAccess` | The class you actually instantiate. Implements `IAzureTableAccess`. Sealed — not meant to be subclassed further. |
| `IAzureTableAccess` | The interface to depend on in your own code (constructors, DI registrations, mocks in unit tests). |
| `TableEntity` | Your custom entities inherit from this instead of `Azure.Data.Tables.TableEntity` directly. Exposes `PartitionKey`, `RowKey`, `Timestamp`, `ETag`. |
| `DynamicTableEntity` | A ready-to-use entity for tables whose column shape you don't want to model as a C# class — properties are stored in a dictionary instead. |

---

## Getting started

### Constructors

```csharp
// Table name known up front — table is created (if missing) immediately.
var tableAccess = new AzureTableAccess("Customers", connectionString);

// Table name decided later.
var tableAccess = new AzureTableAccess(connectionString);
tableAccess.SetTableName("Customers");
```

> Calling any CRUD method before a table name has been set throws a clear
> `InvalidOperationException` ("No table has been selected...") rather than a `NullReferenceException`.

### Table naming rules

Enforced automatically (matches Azure's own requirements):

- 3–63 characters
- Must start with a letter
- Letters and digits only
- Cannot be the reserved name `tables`

An invalid name throws `ArgumentException` before any network call is made.

### Defining your own entity

```csharp
using DataAccess.AzureStorage.Table;

public class CustomerEntity : TableEntity
{
    public CustomerEntity() { }

    public CustomerEntity(string partitionKey, string rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }

    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```

---

## CRUD operations

Every operation below has a synchronous and an `...Async` version. Async versions accept an optional
`CancellationToken`.

### Insert

Fails if the entity already exists (true insert — uses the SDK's `AddEntity`, which returns a 409 Conflict
on a duplicate `PartitionKey`/`RowKey`).

```csharp
var entity = new CustomerEntity("UK", Guid.NewGuid().ToString())
{
    Name = "Jane Doe",
    Email = "jane.doe@example.com",
    Age = 32
};

tableAccess.InsertEntity(entity);
await tableAccess.InsertEntityAsync(entity);
```

### Upsert (insert-or-merge)

Use this when you want "create it if it's not there, merge changed fields if it is" — no error on duplicates.

```csharp
tableAccess.UpsertEntity(entity);
await tableAccess.UpsertEntityAsync(entity);
```

### Replace

Overwrites **all** properties of an existing entity. Requires the entity to already exist.

```csharp
tableAccess.ReplaceEntity(entity);
await tableAccess.ReplaceEntityAsync(entity);
```

### Merge

Updates only the properties present on the entity you pass in; any other stored properties on that row are
left untouched.

```csharp
tableAccess.MergeEntity(entity);
await tableAccess.MergeEntityAsync(entity);
```

### Delete

```csharp
tableAccess.DeleteEntity(entity);
await tableAccess.DeleteEntityAsync(entity);

// Or by key, without loading the entity first:
tableAccess.DeleteEntity(partitionKey: "UK", rowKey: "abc123");
await tableAccess.DeleteEntityAsync(partitionKey: "UK", rowKey: "abc123");
```

### Retrieve

```csharp
// Everything in the table
List<CustomerEntity> all = tableAccess.RetrieveEntities<CustomerEntity>();

// Filtered with an OData filter string
List<CustomerEntity> uk = tableAccess.RetrieveEntities<CustomerEntity>("PartitionKey eq 'UK'");

// Single entity
CustomerEntity one = tableAccess.RetrieveEntity<CustomerEntity>(
    "PartitionKey eq 'UK' and RowKey eq 'abc123'");

// Async
List<CustomerEntity> allAsync = await tableAccess.RetrieveEntitiesAsync<CustomerEntity>();
```

Filter strings follow standard [Azure Table Storage OData filter syntax](https://learn.microsoft.com/rest/api/storageservices/querying-tables-and-entities).
Common operators: `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `and`, `or`.

---

## Optimistic concurrency (ETag)

`Replace`, `Merge`, and `Delete` all accept an optional `ETag? ifMatch` parameter:

```csharp
CustomerEntity loaded = tableAccess.RetrieveEntity<CustomerEntity>(
    "PartitionKey eq 'UK' and RowKey eq 'abc123'");

loaded.Age = 33;

// Uses loaded.ETag automatically — fails with a conflict if someone else
// changed the row since you read it.
tableAccess.ReplaceEntity(loaded);

// Or pass an ETag explicitly:
tableAccess.ReplaceEntity(loaded, someTrackedETag);

// Force an unconditional write regardless of concurrent changes:
tableAccess.ReplaceEntity(loaded, ETag.All);
```

If you don't pass `ifMatch` and the entity's own `ETag` was never populated (e.g. a brand-new object you
built yourself rather than one returned by `Retrieve`), the call falls back to `ETag.All` (unconditional) —
so simple insert-then-write flows keep working without you having to think about ETags at all.

---

## Schema-less rows with `DynamicTableEntity`

For tables where the columns aren't known at compile time (or vary row to row), use `DynamicTableEntity`
instead of a custom `TableEntity` subclass. It supports the same full set of operations:

```csharp
var entity = new DynamicTableEntity
{
    PartitionKey = "UK",
    RowKey = Guid.NewGuid().ToString()
};

entity.Set(new Dictionary<string, object>
{
    ["Name"] = "Jane Doe",
    ["Age"] = 32,
    ["SignupDate"] = DateTimeOffset.UtcNow
});

tableAccess.InsertEntity(entity);
tableAccess.UpsertEntity(entity);
tableAccess.ReplaceEntity(entity);
tableAccess.MergeEntity(entity);
tableAccess.DeleteEntity(entity);

List<DynamicTableEntity> rows = tableAccess.RetrieveEntities("PartitionKey eq 'UK'");

foreach (var row in rows)
{
    Console.WriteLine($"{row.PartitionKey}/{row.RowKey}: {row.Properties["Name"]}");
}
```

Notes:

- `Properties` holds **only your custom columns** — `PartitionKey`, `RowKey`, `Timestamp`, and `ETag` are
  read directly off the entity itself (not duplicated inside the dictionary), so they can never go stale.
  Call `entity.ToDictionary()` if you want one flat dictionary containing everything.
- Supported property types: `string`, `byte[]`, `bool`, `DateTime`, `DateTimeOffset`, `double`, `int`,
  `long`, `Guid` — matching Azure Table Storage's own EDM type set (see `EdmType`). Unsupported types and
  `null` values are silently skipped, since Table Storage has no first-class "null property" concept —
  omit the property instead of storing null.

---

## Error handling

All SDK-level failures are caught and re-thrown as `InvalidOperationException`, with the original
exception preserved as `InnerException` (so `ex.InnerException` still gives you the real Azure SDK
`RequestFailedException`, status code, etc. — nothing is swallowed).

```csharp
try
{
    tableAccess.InsertEntity(entity);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);            // "Insert failed for PartitionKey='UK', RowKey='abc123'."
    Console.WriteLine(ex.InnerException);      // The original Azure.RequestFailedException
}
```

Argument problems (null entity, missing `PartitionKey`/`RowKey`, invalid table name) throw
`ArgumentNullException` / `ArgumentException` immediately, before any network call is made.

---

## Thread safety & reuse

`AzureTableAccess` is safe to share as a long-lived singleton per table — create one instance and reuse it
across your application rather than constructing a new one per call. `SetTableName` is safe to call from
multiple threads and is a no-op (no network call) if the table name hasn't actually changed.

---

## Full example

```csharp
using DataAccess.AzureStorage.Table;

var tableAccess = new AzureTableAccess("Customers", "<connection string>");

var entity = new CustomerEntity("UK", Guid.NewGuid().ToString())
{
    Name = "Jane Doe",
    Email = "jane.doe@example.com",
    Age = 32
};

tableAccess.InsertEntity(entity);

var loaded = tableAccess.RetrieveEntity<CustomerEntity>(
    $"PartitionKey eq '{entity.PartitionKey}' and RowKey eq '{entity.RowKey}'");

loaded.Age = 33;
tableAccess.ReplaceEntity(loaded); // ETag-checked automatically

foreach (var c in tableAccess.RetrieveEntities<CustomerEntity>("PartitionKey eq 'UK'"))
{
    Console.WriteLine($"{c.RowKey}: {c.Name}, age {c.Age}");
}

tableAccess.DeleteEntity(loaded);
```

---

## Local development with Azurite

To test without a real Azure account, run the
[Azurite storage emulator](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) and use:

```
UseDevelopmentStorage=true
```

as the connection string.
