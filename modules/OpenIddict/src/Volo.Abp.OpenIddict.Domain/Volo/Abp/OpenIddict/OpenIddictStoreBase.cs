﻿using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Volo.Abp.OpenIddict;

public abstract class OpenIddictStoreBase<TRepository>
    where TRepository : IRepository
{
    public ILogger<OpenIddictStoreBase<TRepository>> Logger { get; set; }

    protected TRepository Repository { get; }
    protected IUnitOfWorkManager UnitOfWorkManager { get; }
    protected IMemoryCache Cache { get; }

    protected OpenIddictStoreBase(TRepository repository, IUnitOfWorkManager unitOfWorkManager, IMemoryCache cache)
    {
        Repository = repository;
        UnitOfWorkManager = unitOfWorkManager;
        Cache = cache;

        Logger = NullLogger<OpenIddictStoreBase<TRepository>>.Instance;
    }

    protected virtual Guid ConvertIdentifierFromString(string identifier)
    {
        return string.IsNullOrEmpty(identifier) ? default : Guid.Parse(identifier);
    }

    protected virtual string ConvertIdentifierToString(Guid identifier)
    {
        return identifier.ToString("D");
    }

    protected virtual string WriteStream(Action<Utf8JsonWriter> action)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                   {
                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                       Indented = false
                   }))
            {
                action(writer);
                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    protected virtual async Task<string> WriteStreamAsync(Func<Utf8JsonWriter, Task> func)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                   {
                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                       Indented = false
                   }))
            {
                await func(writer);
                await writer.FlushAsync();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
