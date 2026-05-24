using Azure;
using Azure.Data.Tables;
using BenchmarkDotNet.Attributes;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using Microsoft.AspNetCore.Identity;
using Model = ElCamino.AspNetCore.Identity.AzureTable.Model;

namespace ElCamino.AspNetCore.Identity.AzureTable.Benchmark;

[MemoryDiagnoser]
[MarkdownExporter]
public class UserOnlyStoreCreateBenchmarks
{
    private BenchmarkTableServiceClient _tableServiceClient = default!;
    private BenchmarkUserOnlyStoreOperations _store = default!;
    private int _userSequence;
    private Model.IdentityUser _user = default!;

    [GlobalSetup]
    public void Setup()
    {
        (_tableServiceClient, _store, _) = BenchmarkStoreFactory.CreateUserOnlyStore();
    }

    [IterationSetup]
    public void PrepareUser()
    {
        _tableServiceClient.ClearAll();
        _user = BenchmarkStoreFactory.CreateUser($"create-{_userSequence++}");
    }

    [Benchmark]
    public async Task<int> UserOnlyStore_CreateAsync()
    {
        IdentityResult result = await _store.CreateAsync(_user).ConfigureAwait(false);
        return result.Succeeded ? 1 : 0;
    }
}

[MemoryDiagnoser]
[MarkdownExporter]
public class UserOnlyStoreDeleteBenchmarks
{
    private BenchmarkTableServiceClient _tableServiceClient = default!;
    private BenchmarkUserOnlyStoreOperations _store = default!;
    private Model.IKeyHelper _keyHelper = default!;
    private int _userSequence;
    private Model.IdentityUser _user = default!;

    [GlobalSetup]
    public void Setup()
    {
        (_tableServiceClient, _store, _keyHelper) = BenchmarkStoreFactory.CreateUserOnlyStore();
    }

    [IterationSetup]
    public void SeedUserAggregate()
    {
        _tableServiceClient.ClearAll();
        _user = BenchmarkStoreFactory.CreateUser($"delete-{_userSequence++}");
        BenchmarkStoreFactory.SeedUserAggregate(_tableServiceClient, _keyHelper, _user);
    }

    [Benchmark]
    public async Task<int> UserOnlyStore_DeleteAsync()
    {
        IdentityResult result = await _store.DeleteAsync(_user).ConfigureAwait(false);
        return result.Succeeded ? 1 : 0;
    }
}

[MemoryDiagnoser]
[MarkdownExporter]
public class UserOnlyStoreGetUserQueryBenchmarks
{
    private BenchmarkUserOnlyStoreOperations _store = default!;
    private string[] _userIds = [];

    [Params(0, 1, 10, 100)]
    public int UserCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (tableServiceClient, store, keyHelper) = BenchmarkStoreFactory.CreateUserOnlyStore();
        _store = store;

        InMemoryTableClient userTable = tableServiceClient.GetInMemoryTable(TableConstants.TableNames.UsersTable);
        _userIds = new string[UserCount];
        for (int userIndex = 0; userIndex < UserCount; userIndex++)
        {
            Model.IdentityUser user = BenchmarkStoreFactory.CreateUser($"query-{UserCount}-{userIndex}");
            userTable.SeedEntity(user);
            _userIds[userIndex] = keyHelper.GenerateRowKeyUserId(user.Id).ToString();
        }
    }

    [Benchmark]
    public async Task<int> UserOnlyStore_GetUserQueryAsync()
    {
        IEnumerable<Model.IdentityUser> users = await _store.GetUserQuery(_userIds).ConfigureAwait(false);
        return users.Count();
    }
}

[MemoryDiagnoser]
[MarkdownExporter]
public class UserStoreAddToRoleBenchmarks
{
    private BenchmarkTableServiceClient _tableServiceClient = default!;
    private BenchmarkUserStoreOperations _store = default!;
    private int _roleSequence;
    private Model.IdentityUser _user = default!;
    private string _roleName = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        (_tableServiceClient, _store, _) = BenchmarkStoreFactory.CreateUserStore();
    }

    [IterationSetup]
    public void PrepareRole()
    {
        _tableServiceClient.ClearAll();
        _user = BenchmarkStoreFactory.CreateUser($"role-user-{_roleSequence}");
        _roleName = $"role-{_roleSequence++}";
    }

    [Benchmark]
    public async Task<int> UserStore_AddToRoleAsync()
    {
        await _store.AddToRoleAsync(_user, _roleName).ConfigureAwait(false);
        return _tableServiceClient.GetInMemoryTable(TableConstants.TableNames.UsersTable).Count
            + _tableServiceClient.GetInMemoryTable(TableConstants.TableNames.IndexTable).Count;
    }
}

internal sealed class BenchmarkUserOnlyStoreOperations : UserOnlyStore<Model.IdentityUser, IdentityCloudContext>
{
    public BenchmarkUserOnlyStoreOperations(IdentityCloudContext context, Model.IKeyHelper keyHelper)
        : base(context, keyHelper)
    {
    }

    public Task<IEnumerable<Model.IdentityUser>> GetUserQuery(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        return GetUserQueryAsync(userIds, cancellationToken);
    }
}

internal sealed class BenchmarkUserStoreOperations : UserStore<Model.IdentityUser, Model.IdentityRole, IdentityCloudContext>
{
    public BenchmarkUserStoreOperations(IdentityCloudContext context, Model.IKeyHelper keyHelper)
        : base(context, keyHelper)
    {
    }
}

internal static class BenchmarkStoreFactory
{
    public static (BenchmarkTableServiceClient TableServiceClient, BenchmarkUserOnlyStoreOperations Store, Model.IKeyHelper KeyHelper) CreateUserOnlyStore()
    {
        var keyHelper = new DefaultKeyHelper();
        var tableServiceClient = new BenchmarkTableServiceClient();
        var context = new IdentityCloudContext(new Model.IdentityConfiguration(), tableServiceClient);
        return (tableServiceClient, new BenchmarkUserOnlyStoreOperations(context, keyHelper), keyHelper);
    }

    public static (BenchmarkTableServiceClient TableServiceClient, BenchmarkUserStoreOperations Store, Model.IKeyHelper KeyHelper) CreateUserStore()
    {
        var keyHelper = new DefaultKeyHelper();
        var tableServiceClient = new BenchmarkTableServiceClient();
        var context = new IdentityCloudContext(new Model.IdentityConfiguration(), tableServiceClient);
        return (tableServiceClient, new BenchmarkUserStoreOperations(context, keyHelper), keyHelper);
    }

    public static Model.IdentityUser CreateUser(string id)
    {
        var user = new Model.IdentityUser
        {
            Id = id,
            UserName = $"{id}@example.com",
            NormalizedUserName = $"{id.ToUpperInvariant()}@EXAMPLE.COM",
            Email = $"{id}@example.com",
            NormalizedEmail = $"{id.ToUpperInvariant()}@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = id,
            ETag = ETag.All,
        };

        ((Model.IGenerateKeys)user).GenerateKeys(new DefaultKeyHelper());
        return user;
    }

    public static void SeedUserAggregate(BenchmarkTableServiceClient tableServiceClient, Model.IKeyHelper keyHelper, Model.IdentityUser user)
    {
        InMemoryTableClient userTable = tableServiceClient.GetInMemoryTable(TableConstants.TableNames.UsersTable);
        userTable.SeedEntity(user);

        var claim = new Model.IdentityUserClaim
        {
            UserId = user.Id,
            ClaimType = "scope",
            ClaimValue = "delete",
            PartitionKey = user.PartitionKey,
            ETag = ETag.All,
        };
        ((Model.IGenerateKeys)claim).GenerateKeys(keyHelper);
        userTable.SeedEntity(claim);

        var login = new Model.IdentityUserLogin
        {
            UserId = user.Id,
            LoginProvider = "provider",
            ProviderKey = "delete-provider-key",
            ProviderDisplayName = "provider",
            PartitionKey = user.PartitionKey,
            ETag = ETag.All,
        };
        ((Model.IGenerateKeys)login).GenerateKeys(keyHelper);
        userTable.SeedEntity(login);

        var token = new Model.IdentityUserToken
        {
            UserId = user.Id,
            LoginProvider = "provider",
            Name = "delete-token",
            Value = "delete-token-value",
            PartitionKey = user.PartitionKey,
            ETag = ETag.All,
        };
        ((Model.IGenerateKeys)token).GenerateKeys(keyHelper);
        userTable.SeedEntity(token);
    }

    public static InMemoryTableClient GetInMemoryTable(this BenchmarkTableServiceClient tableServiceClient, string tableName)
    {
        return (InMemoryTableClient)tableServiceClient.GetTableClient(tableName);
    }
}
