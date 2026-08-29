[![CI Pipeline](https://github.com/JicoDotNet/DataAccess.AzureStorage/actions/workflows/build.yml/badge.svg)](https://github.com/JicoDotNet/DataAccess.AzureStorage/actions/workflows/build.yml)

![GitHub repo size](https://img.shields.io/github/repo-size/JicoDotNet/DataAccess.AzureStorage)
![GitHub stars](https://img.shields.io/github/stars/JicoDotNet/DataAccess.AzureStorage?style=social)
![GitHub license](https://img.shields.io/github/license/JicoDotNet/DataAccess.AzureStorage)
![Contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen)  

# DataAccess.AzureStorage

A dependency-light data access layer over **Azure Table Storage** and **Azure Blob Storage**, built on
the official [`Azure.Data.Tables`](https://www.nuget.org/packages/Azure.Data.Tables) and
[`Azure.Storage.Blobs`](https://www.nuget.org/packages/Azure.Storage.Blobs) SDKs.
It targets **.NET Standard 2.0 / 2.1**, so it can be consumed from .NET Framework, .NET Core, and modern .NET projects alike.

This document covers the `DataAccess.AzureStorage.Table` namespace — 
table name and connection string are supplied by the caller, 
and everything else (table creation, entity CRUD, optimistic concurrency, 
schema-less rows) is handled for you.

Also it covers the `DataAccess.AzureStorage.Blob` namespace — 
blob file and connection string are supplied by the caller, 
and everything else (container creation, file upload-download-delete, 
optimistic concurrency) is handled for you.

## Features

### Azure Table Storage

- Simple connection-string based initialization
- Automatic table creation
- Table name validation
- Strongly typed entity support
- Dynamic entity support
- CRUD operations
- OData filtering
- Async APIs
- CancellationToken support
- ETag-based optimistic concurrency
- Automatic handling of supported Azure Table EDM/CLR types
- Clear exception wrapping
- Reusable access objects

### Azure Blob Storage

- Simple connection-string based initialization
- Automatic container creation
- Container name validation
- Upload from `Stream`
- Download to `byte[]`
- Download content type
- Blob existence check
- Blob deletion
- Delete by absolute URL
- Download by absolute URL
- Virtual directory support
- Overwrite/non-overwrite upload modes
- Blob ETag information
- Blob details including size, content type and last modified time
- Async APIs
- CancellationToken support
- Path sanitization and basic path-traversal protection

---

## Requirements

- .NET Standard 2.0 or 2.1 compatible runtime (.NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+, etc.)
- NuGet package: `Azure.Data.Tables` (v12.10.0 or compatible)
- NuGet package: `Azure.Data.Blobs` (v12.26.0 or compatible)
- An Azure Storage account connection string (or the Azurite emulator connection string for local dev)
- The library accepts: `UseDevelopmentStorage=true`

---

## Installation

Install the `DataAccess.AzureStorage` package from NuGet.

```powershell
Install-Package DataAccess.AzureStorage
```
Or:

```bash
dotnet add package DataAccess.AzureStorage
```

The package currently targets `.NET Standard 2.0` and `.NET Standard 2.1`.

The package depends on:

```text
Azure.Data.Tables >= 12.12.0
Azure.Storage.Blobs >= 12.26.0
```
These dependencies are defined by the package specification and project configuration.

## Part 1 — Table `DataAccess.AzureStorage.Table`

### Architecture

```
AzureStorageManager             (abstract) — holds & validates the connection string
    └── AzureTableManager       (abstract) — owns TableServiceClient / TableClient, table name validation & creation
            └── AzureTableAccess  (sealed) — implements IAzureTableAccess; all CRUD + query operations

IAzureTableAccess / AzureTableAccess       — public contract; program against this for testability/DI
IAzureTableEntity / AzureTableEntity       — abstract base for your own entity classes (implements ITableEntity)
IDynamicTableEntity / DynamicTableEntity   — schema-less entity for when the row shape isn't known at compile time
EdmType                                    — enum of the Azure Table Storage EDM types DynamicTableEntity accepts
EdmTypeMap                                 — internal static class to handle Supported CLR Type for entity
```

| Type | Role |
|---|---|
| `AzureStorageManager` | Validates and stores the connection string. Base for any future Azure service manager (Table, Blob, etc.). |
| `AzureTableManager` | Creates the `TableServiceClient`, validates table names against Azure's naming rules, creates the table on first use, and guards every operation with `EnsureTableReady()`. |
| `AzureTableAccess` | The class you actually instantiate. Implements `IAzureTableAccess`. Sealed — not meant to be subclassed further. |
| `IAzureTableAccess` | The interface to depend on in your own code (constructors, DI registrations, mocks in unit tests). |
| `IAzureTableEntity` | Your custom entities inherit from this instead of `Azure.Data.Tables.AzureTableEntity` directly. Exposes `PartitionKey`, `RowKey`, `Timestamp`, `ETag`. |
| `AzureTableEntity` | Your custom entities inherit from `IAzureTableEntity`. |
| `DynamicTableEntity` | A ready-to-use entity for tables whose column shape you don't want to model as a C# class — properties are stored in a dictionary instead. |

```csharp
var tableAccess = new AzureTableAccess("MyTable", "<connection string>");
tableAccess.InsertEntity(myEntity);
tableAccess.ReplaceEntity(myEntity);
```
---

### Getting started

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

Enforced automatically, matches Azure's own requirements:

- 3–63 characters
- Must start with a letter
- Letters and digits only
- Cannot be the reserved name `tables`

An invalid name throws `ArgumentException` before any network call is made.

### Defining your own entity

```csharp
using DataAccess.AzureStorage.Table;

public class CustomerEntity : AzureTableEntity
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

### CRUD operations

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

### Optimistic concurrency (ETag)

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

### Schema-less rows with `DynamicTableEntity`

For tables where the columns aren't known at compile time (or vary row to row), use `DynamicTableEntity`
instead of a custom `AzureTableEntity` subclass. It supports the same full set of operations:

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
  Call `entity.Get()` if you want one flat dictionary containing everything.
- Supported property types: `string`, `byte[]`, `bool`, `DateTime`, `DateTimeOffset`, `double`, `int`,
  `long`, `Guid` — matching Azure Table Storage's own EDM type set (see `EdmType`). Unsupported types and
  `null` values are silently skipped, since Table Storage has no first-class "null property" concept —
  omit the property instead of storing null.

---

### Error handling

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

### Full example

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


## Part 2 — Blob Storage `DataAccess.AzureStorage.Blob`

### Architecture

```
AzureStorageManager             (abstract) — holds & validates the connection string
    └── AzureBlobManager        (abstract) — owns BlobServiceClient / BlobContainerClient, container name validation & creation
            └── AzureBlobAccess   (sealed) — implements IAzureBlobAccess; all upload/list/delete/download operations

IAzureBlobAccess / AzureBlobAccess         — public contract; program against this for testability/DI
IBlobRequestClient / BlobRequestClient     — describes a file to upload (stream, file name, content type, virtual directory path)
IBlobResponseClient / BlobResponseClient   — details of a blob returned after a successful upload (URI, container, ETag, etc.)
IBlobDetails / BlobDetails                 — metadata for a blob returned by a listing operation
```

| Type | Role |
|---|---|
| `AzureStorageManager` | Validates and stores the connection string. Shared base for any Azure service manager (Table, Blob, etc.). |
| `AzureBlobManager` | OCreates the `BlobServiceClient`, validates container names against Azure's naming rules, creates the container on first use, and guards every operation with `EnsureContainerReady()`. |
| `AzureBlobAccess` | The class you actually instantiate. Implements `IAzureBlobAccess`. Sealed — not meant to be subclassed further. |
| `IAzureBlobAccess` | The interface to depend on in your own code (constructors, DI registrations, mocks in unit tests). |
| `BlobRequestClient` | Describes a file to upload — file stream, file name, content type, and an optional virtual directory path (e.g. `new[] { "invoices", "2026" }`). |
| `BlobResponseClient` | What you get back after a successful upload — the blob's `Uri`, `ContainerName`, `AccountName`, `AbsolutePath`, and `ETag` (for a later conditional operation). |
| `BlobDetails` | Metadata for a single blob returned by ListBlobDetails — `Path`, `ContentLength`, `ContentType`, `LastModified`, `ETag`. |

```csharp
var blobAccess = new AzureBlobAccess("container-name", "<connection string>");
var blobRequestClient = new BlobRequestClient("<fileStream>", "<FileNameWithExtension>");
    blobRequestClient.Directories = new string[] { "NewFolder" };
    blobRequestClient.ContentType = <MIME Content Type>";
var blobResponseClient = blobAccess.Upload(blobRequestClient)
string filePath = blobResponseClient.Path;
```
---

### Getting started

```csharp
// Container name known up front — container is created (if missing) immediately.
var blobAccess = new AzureBlobAccess("customer-photos", connectionString);

// Container name decided later.
var blobAccess = new AzureBlobAccess(connectionString);
blobAccess.SetContainer("customer-photos");
```

> Calling any operation before a container has been selected throws a clear `InvalidOperationException`
> ("No container has been selected...") rather than a `NullReferenceException`.

### Container naming rules

Enforced automatically, matching Azure's own requirements: 
- 3–63 characters
- lowercase letters/numbers/single hyphens only
- must start and end with a letter or number
- no consecutive hyphens
- Casing is normalized automatically — `"Customer-Photos"` is accepted and stored as `"customer-photos"`.

An invalid name throws `ArgumentException` before any network call is made.

### Uploading files

```csharp
var request = new BlobRequestClient(fileStream, "invoice-1042.pdf")
{
    ContentType = "application/pdf",
    Directories = new[] { "invoices", "2026" } // uploads to "invoices/2026/invoice-1042.pdf"
};

IBlobResponseClient result = blobAccess.Upload(request);
await blobAccess.UploadAsync(request);

Console.WriteLine(result.Uri);   // https://mystorageacct.blob.core.windows.net/customer-photos/invoices/2026/invoice-1042.pdf
Console.WriteLine(result.ETag);  // for a later conditional operation against this exact blob
```

By default, `Upload` **overwrites** an existing blob at that path with no error. Pass `overwrite: false`
for true "insert" semantics — fails instead if a blob already exists there:

```csharp
blobAccess.Upload(request, overwrite: false);
```

### Listing blobs

```csharp
// Everything in the container
List<IBlobDetails> all = blobAccess.ListBlobDetails();

// Only blobs under a virtual folder path
List<IBlobDetails> invoices2026 = blobAccess.ListBlobDetails(new[] { "invoices", "2026" });

// Async
List<IBlobDetails> allAsync = await blobAccess.ListBlobDetailsAsync();

foreach (var blob in all)
{
    Console.WriteLine($"{blob.Path} — {blob.ContentLength} bytes, {blob.ContentType}, modified {blob.LastModified}");
}
```

### Checking existence

```csharp
bool exists = blobAccess.Exists("invoices/2026/invoice-1042.pdf");
bool existsAsync = await blobAccess.ExistsAsync("invoices/2026/invoice-1042.pdf");
```

### Deleting blobs

Two forms, depending on whether you're working within the currently selected container or across
containers:

```csharp
// Name-relative — operates within the currently selected container.
bool deleted = blobAccess.Delete("invoices/2026/invoice-1042.pdf");
await blobAccess.DeleteAsync("invoices/2026/invoice-1042.pdf");

// By full URL — may belong to a different container than the one currently
// selected. Does NOT change the selected container, and does NOT create the
// target container if it's missing.
bool deletedByUrl = blobAccess.DeleteByUrl(
    "https://mystorageacct.blob.core.windows.net/archived-invoices/2024/old-invoice.pdf");
await blobAccess.DeleteByUrlAsync(url);
```

Both return `false` (not an exception) if no blob existed at that path.

### Downloading files

```csharp
// Name-relative
(byte[] fileContent, string contentType) = blobAccess.DownloadFile("invoices/2026/invoice-1042.pdf");
var asyncResult = await blobAccess.DownloadFileAsync("invoices/2026/invoice-1042.pdf");

// By full URL, across containers
var byUrl = blobAccess.DownloadFileByUrl("https://mystorageacct.blob.core.windows.net/archived-invoices/2024/old-invoice.pdf");
```

> Downloads load the entire blob into memory as `byte[]`. For very large files, consider working directly
> against `ContainerClient`/`BlobClient` (exposed via the base class) for a streaming download instead.

### Error handling (Blob)

Same pattern as the Table side — SDK failures are wrapped in `InvalidOperationException` with the original
exception preserved as `InnerException`:

```csharp
try
{
    blobAccess.Upload(request, overwrite: false);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);        // "Upload failed for blob 'invoices/2026/invoice-1042.pdf' in container 'customer-photos'."
    Console.WriteLine(ex.InnerException); // The original Azure.RequestFailedException
}
```

`ArgumentException`/`ArgumentNullException` are thrown immediately for bad input (empty blob name, invalid
container name, a `blobUrl` missing a container or blob segment, a path-traversal segment like `..`) —
before any network call is made.

---



## Thread safety & reuse

Both `AzureTableAccess` and `AzureBlobAccess` are safe to share as long-lived singletons — create one
instance per table/container and reuse it across your application rather than constructing a new one per
call. `SetTableName`/`SetContainer` are safe to call from multiple threads and are a no-op (no network
call) if the name hasn't actually changed since the last call.

---

## Local development with Azurite

To test without a real Azure account, run the
[Azurite storage emulator](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) and use:

```
UseDevelopmentStorage=true
```

as the connection string.
