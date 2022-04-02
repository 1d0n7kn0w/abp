﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;

namespace Volo.Abp.OpenIddict.Scopes;

public class OpenIddictScopeRepository : EfCoreRepository<IOpenIddictDbContext, OpenIddictScope, Guid>, IOpenIddictScopeRepository
{
    public OpenIddictScopeRepository(IDbContextProvider<IOpenIddictDbContext> dbContextProvider)
        : base(dbContextProvider)
    {

    }

    public virtual async Task<long> CountAsync<TResult>(Func<IQueryable<OpenIddictScope>, IQueryable<TResult>> query, CancellationToken cancellationToken = default)
    {
        return await query(await GetQueryableAsync()).LongCountAsync(cancellationToken);
    }

    public virtual async Task<OpenIddictScope> FindByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync()).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual async Task<OpenIddictScope> FindByNameAsync(string name, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync()).FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public virtual async Task<List<OpenIddictScope>> FindByNamesAsync(string[] names, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync()).Where(x => names.Contains(x.Name)).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<OpenIddictScope>> FindByResourceAsync(string resource, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync()).Where(x => x.Resources.Contains(resource)).ToListAsync(cancellationToken);
    }

    public virtual async Task<TResult> GetAsync<TState, TResult>(Func<IQueryable<OpenIddictScope>, TState, IQueryable<TResult>> query, TState state, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await query(await GetQueryableAsync(), state).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<List<OpenIddictScope>> ListAsync(int? count, int? offset, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await (await GetQueryableAsync())
            .OrderBy(x => x.Id)
            .SkipIf<OpenIddictScope, IQueryable<OpenIddictScope>>(offset.HasValue, offset.Value)
            .TakeIf<OpenIddictScope, IQueryable<OpenIddictScope>>(count.HasValue, count.Value)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TResult>> ListAsync<TState, TResult>(Func<IQueryable<OpenIddictScope>, TState, IQueryable<TResult>> query, TState state, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await query(await GetQueryableAsync(), state).ToListAsync(cancellationToken);
    }
}
