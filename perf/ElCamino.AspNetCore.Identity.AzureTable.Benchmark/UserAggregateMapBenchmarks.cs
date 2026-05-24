using System.Collections.Generic;
using System.Reflection;
using Azure;
using Azure.Data.Tables;
using BenchmarkDotNet.Attributes;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using Model = ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable.Benchmark;

[MemoryDiagnoser]
[MarkdownExporter]
public class UserAggregateMapBenchmarks
{
    private readonly List<UserAggregateData> _aggregateData = [];
    private BenchmarkUserOnlyStore _userOnlyStore = default!;
    private BenchmarkUserStore _userStore = default!;
    private Model.IKeyHelper _keyHelper = default!;

    [Params(10, 100)]
    public int UserCount { get; set; }

    [Params(2)]
    public int ClaimsPerUser { get; set; }

    [Params(2)]
    public int RolesPerUser { get; set; }

    [Params(2)]
    public int LoginsPerUser { get; set; }

    [Params(2)]
    public int TokensPerUser { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _aggregateData.Clear();
        _keyHelper = new DefaultKeyHelper();

        var config = new Model.IdentityConfiguration();
        var credential = new TableSharedKeyCredential("benchmark", Convert.ToBase64String(new byte[32]));
        var tableServiceClient = new TableServiceClient(new Uri("https://benchmark.table.core.windows.net"), credential);
        var context = new IdentityCloudContext(config, tableServiceClient);

        _userOnlyStore = new BenchmarkUserOnlyStore(context, _keyHelper);
        _userStore = new BenchmarkUserStore(context, _keyHelper);

        for (int userIndex = 0; userIndex < UserCount; userIndex++)
        {
            var user = new Model.IdentityUser
            {
                Id = $"user-{userIndex}",
                UserName = $"user{userIndex}@example.com",
                NormalizedUserName = $"USER{userIndex}@EXAMPLE.COM",
                Email = $"user{userIndex}@example.com",
                NormalizedEmail = $"USER{userIndex}@EXAMPLE.COM",
            };
            ((Model.IGenerateKeys)user).GenerateKeys(_keyHelper);

            List<TableEntity> rows = [ToTableEntity(user)];

            for (int claimIndex = 0; claimIndex < ClaimsPerUser; claimIndex++)
            {
                var claim = new Model.IdentityUserClaim
                {
                    UserId = user.Id,
                    ClaimType = "scope",
                    ClaimValue = $"claim-{claimIndex}",
                    PartitionKey = user.PartitionKey,
                    ETag = ETag.All,
                };
                ((Model.IGenerateKeys)claim).GenerateKeys(_keyHelper);
                rows.Add(ToTableEntity(claim));
            }

            for (int roleIndex = 0; roleIndex < RolesPerUser; roleIndex++)
            {
                var role = new Model.IdentityUserRole
                {
                    UserId = user.Id,
                    RoleId = $"role-id-{roleIndex}",
                    RoleName = $"role-{roleIndex}",
                    PartitionKey = user.PartitionKey,
                    ETag = ETag.All,
                };
                ((Model.IGenerateKeys)role).GenerateKeys(_keyHelper);
                rows.Add(ToTableEntity(role));
            }

            for (int loginIndex = 0; loginIndex < LoginsPerUser; loginIndex++)
            {
                var login = new Model.IdentityUserLogin
                {
                    UserId = user.Id,
                    LoginProvider = "provider",
                    ProviderKey = $"provider-key-{loginIndex}",
                    ProviderDisplayName = "provider",
                    PartitionKey = user.PartitionKey,
                    ETag = ETag.All,
                };
                ((Model.IGenerateKeys)login).GenerateKeys(_keyHelper);
                rows.Add(ToTableEntity(login));
            }

            for (int tokenIndex = 0; tokenIndex < TokensPerUser; tokenIndex++)
            {
                var token = new Model.IdentityUserToken
                {
                    UserId = user.Id,
                    LoginProvider = "provider",
                    Name = $"token-{tokenIndex}",
                    Value = "token-value",
                    PartitionKey = user.PartitionKey,
                    ETag = ETag.All,
                };
                ((Model.IGenerateKeys)token).GenerateKeys(_keyHelper);
                rows.Add(ToTableEntity(token));
            }

            _aggregateData.Add(new UserAggregateData(user.PartitionKey, [.. rows]));
        }
    }

    [Benchmark(Baseline = true)]
    public int UserOnlyStore_MapUserAggregate()
    {
        int total = 0;
        foreach (var aggregate in _aggregateData)
        {
            var mapped = _userOnlyStore.MapAggregate(aggregate.UserId, aggregate.Rows);
            if (mapped.User is not null)
            {
                total++;
            }

            total += CountItems(mapped.Claims);
            total += CountItems(mapped.Logins);
            total += CountItems(mapped.Tokens);
        }

        return total;
    }

    [Benchmark]
    public int UserStore_MapUserAggregate()
    {
        int total = 0;
        foreach (var aggregate in _aggregateData)
        {
            var mapped = _userStore.MapAggregate(aggregate.UserId, aggregate.Rows);
            if (mapped.User is not null)
            {
                total++;
            }

            total += CountItems(mapped.Roles);
            total += CountItems(mapped.Claims);
            total += CountItems(mapped.Logins);
            total += CountItems(mapped.Tokens);
        }

        return total;
    }

    private static int CountItems<T>(IEnumerable<T> source)
    {
        int count = 0;
        foreach (var _ in source)
        {
            count++;
        }

        return count;
    }

    private static TableEntity ToTableEntity<T>(T entity)
    {
        var properties = entity!.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
        Dictionary<string, object?> dictionary = new(properties.Length);

        foreach (var property in properties)
        {
            dictionary[property.Name] = property.GetValue(entity);
        }

        return new TableEntity(dictionary);
    }

    private readonly record struct UserAggregateData(string UserId, TableEntity[] Rows);
}

internal sealed class BenchmarkUserOnlyStore : UserOnlyStore<Model.IdentityUser, IdentityCloudContext>
{
    public BenchmarkUserOnlyStore(IdentityCloudContext context, Model.IKeyHelper keyHelper)
        : base(context, keyHelper)
    {
    }

    public (Model.IdentityUser? User,
        IEnumerable<Model.IdentityUserClaim> Claims,
        IEnumerable<Model.IdentityUserLogin> Logins,
        IEnumerable<Model.IdentityUserToken> Tokens)
        MapAggregate(string userId, IEnumerable<TableEntity> userResults)
    {
        return MapUserAggregate(userId, userResults);
    }
}

internal sealed class BenchmarkUserStore : UserStore<Model.IdentityUser, Model.IdentityRole, IdentityCloudContext>
{
    public BenchmarkUserStore(IdentityCloudContext context, Model.IKeyHelper keyHelper)
        : base(context, keyHelper)
    {
    }

    public (Model.IdentityUser? User,
        IEnumerable<Model.IdentityUserRole> Roles,
        IEnumerable<Model.IdentityUserClaim> Claims,
        IEnumerable<Model.IdentityUserLogin> Logins,
        IEnumerable<Model.IdentityUserToken> Tokens)
        MapAggregate(string userId, IEnumerable<TableEntity> userResults)
    {
        return MapUserAggregate(userId, userResults);
    }
}
