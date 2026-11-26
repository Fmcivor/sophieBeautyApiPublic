using Moq;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.services;
public class TreatmentServiceTests
{
    private readonly Mock<ITreatmentRepository> _repoMock;
    private readonly treatmentService _sut;

    public TreatmentServiceTests()
    {
        _repoMock = new Mock<ITreatmentRepository>();
        _sut = new treatmentService(_repoMock.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static treatment MakeTreatment(string id = "abc123") => new treatment
    {
        Id = id,
        name = "facial",
        price = 50,
        duration = 60,
        type = "face",
        description = "A relaxing facial treatment for all skin types available"
    };

    // ── getAll ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task getAll_ReturnsTreatments()
    {
        _repoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<treatment> { MakeTreatment(), MakeTreatment("def456") });

        var result = await _sut.getAll();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task getAll_NoTreatments_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<treatment>());

        var result = await _sut.getAll();

        Assert.Empty(result);
    }

    // ── create ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task create_ReturnsTreatment()
    {
        var treatment = MakeTreatment();
        _repoMock.Setup(r => r.CreateAsync(treatment))
            .ReturnsAsync(treatment);

        var result = await _sut.create(treatment);

        Assert.NotNull(result);
        Assert.Equal(treatment.name, result.name);
    }

    [Fact]
    public async Task create_CallsRepositoryOnce()
    {
        var treatment = MakeTreatment();
        _repoMock.Setup(r => r.CreateAsync(treatment))
            .ReturnsAsync(treatment);

        await _sut.create(treatment);

        _repoMock.Verify(r => r.CreateAsync(treatment), Times.Once);
    }

    // ── getById ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task getById_ValidId_ReturnsTreatment()
    {
        _repoMock.Setup(r => r.GetByIdAsync("abc123"))
            .ReturnsAsync(MakeTreatment());

        var result = await _sut.getById("abc123");

        Assert.NotNull(result);
        Assert.Equal("abc123", result!.Id);
    }

    [Fact]
    public async Task getById_InvalidId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("notreal"))
            .ReturnsAsync((treatment?)null);

        var result = await _sut.getById("notreal");

        Assert.Null(result);
    }

    // ── update ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task update_ValidTreatment_ReturnsTrue()
    {
        var treatment = MakeTreatment();
        _repoMock.Setup(r => r.UpdateAsync(treatment))
            .ReturnsAsync(true);

        var result = await _sut.update(treatment);

        Assert.True(result);
    }

    [Fact]
    public async Task update_TreatmentNotFound_ReturnsFalse()
    {
        var treatment = MakeTreatment("notreal");
        _repoMock.Setup(r => r.UpdateAsync(treatment))
            .ReturnsAsync(false);

        var result = await _sut.update(treatment);

        Assert.False(result);
    }

    // ── delete ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task delete_ValidId_ReturnsTrue()
    {
        _repoMock.Setup(r => r.DeleteAsync("abc123"))
            .ReturnsAsync(true);

        var result = await _sut.delete("abc123");

        Assert.True(result);
    }

    [Fact]
    public async Task delete_InvalidId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.DeleteAsync("notreal"))
            .ReturnsAsync(false);

        var result = await _sut.delete("notreal");

        Assert.False(result);
    }

    // ── getListByIds ──────────────────────────────────────────────────────────
    [Fact]
    public async Task getListByIds_ValidIds_ReturnsTreatments()
    {
        var ids = new List<string> { "abc123", "def456" };
        _repoMock.Setup(r => r.GetListByIdsAsync(ids))
            .ReturnsAsync(new List<treatment> { MakeTreatment("abc123"), MakeTreatment("def456") });

        var result = await _sut.getListByIds(ids);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task getListByIds_SomeInvalidIds_ReturnsOnlyMatched()
    {
        var ids = new List<string> { "abc123", "notreal" };
        _repoMock.Setup(r => r.GetListByIdsAsync(ids))
            .ReturnsAsync(new List<treatment> { MakeTreatment("abc123") });

        var result = await _sut.getListByIds(ids);

        Assert.Single(result);
    }

    [Fact]
    public async Task getListByIds_EmptyList_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetListByIdsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<treatment>());

        var result = await _sut.getListByIds(new List<string>());

        Assert.Empty(result);
    }
}