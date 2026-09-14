using KaraokeMaster.Core.Data;
using Xunit;

namespace KaraokeMaster.Core.Tests.Data;

public class WatchedFolderRepositoryTests : SqliteTestBase
{
    private readonly WatchedFolderRepository _repo;

    public WatchedFolderRepositoryTests()
    {
        _repo = new WatchedFolderRepository(Factory);
    }

    [Fact]
    public async Task Add_And_GetAll_RoundTrips()
    {
        await _repo.AddAsync(@"D:\Music");
        await _repo.AddAsync(@"D:\Karaoke");

        var all = await _repo.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Add_SamePathTwice_DoesNotDuplicate()
    {
        await _repo.AddAsync(@"D:\Music");
        await _repo.AddAsync(@"D:\Music");

        var all = await _repo.GetAllAsync();

        Assert.Single(all);
    }

    [Fact]
    public async Task Remove_DeletesFolder()
    {
        var id = await _repo.AddAsync(@"D:\Music");

        await _repo.RemoveAsync(id);

        Assert.Empty(await _repo.GetAllAsync());
    }
}
