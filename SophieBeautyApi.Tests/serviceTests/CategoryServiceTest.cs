using Moq;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.services;

public class CategoryServiceTest
{

    private readonly Mock<ICategoryRepository> _categoryRepoMock;

    private readonly categoryService _sut;

    public CategoryServiceTest()
    {
        this._categoryRepoMock = new Mock<ICategoryRepository>();

        _sut = new categoryService(_categoryRepoMock.Object);
    }

    // Create test data

    private category makeCategory()
    {
        category c = new category("Test Category");
        c.Id = "abc123";
        return c;
    }





    [Fact]
    public async Task getAll_noCategories_returnsEmpty()
    {
        // Given
        _categoryRepoMock.Setup(r => r.GetAllAsync())
        .ReturnsAsync(new List<category>());

        // When
        var categories = await _sut.getAll();

        // Then
        Assert.Empty(categories);
    }


    [Fact]
    public async Task getAll_returnsCategories()
    {
        // Given
        _categoryRepoMock.Setup(r => r.GetAllAsync())
        .ReturnsAsync(new List<category> { makeCategory() });

        // When

        var categories = await _sut.getAll();

        // Then

        Assert.Single(categories);
    }


    // create 

    [Fact]
    public async Task create_validCategory_returnsSuccess()
    {
        // Given

        _categoryRepoMock.Setup(r => r.GetAllAsync())
        .ReturnsAsync(new List<category>());

        _categoryRepoMock.Setup(r => r.CreateAsync(It.IsAny<category>()))
        .ReturnsAsync((category c) => c);

        // When

        var result = await _sut.create("valid category");

        // Then

        Assert.NotNull(result);
        Assert.Equal("valid category", result.name);
    }

    [Fact]
    public async Task create_duplicateCategory_returnsNull()
    {
        // Given

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<category> { makeCategory() });

        // When

        var result = await _sut.create("Test Category");

        // Then

        Assert.Null(result);
    }

    // delete

    [Fact]
    public async Task delete_categoryExists_returnsTrue()
    {
        // Given
        var existingCategory = makeCategory();
        _categoryRepoMock.Setup(r => r.DeleteAsync(It.IsAny<category>()))
            .ReturnsAsync(true);

        // When
        var result = await _sut.delete(existingCategory);

        // Then
        Assert.True(result);
        _categoryRepoMock.Verify(r => r.DeleteAsync(existingCategory), Times.Once);
    }

    [Fact]
    public async Task delete_invalidCategory_returnsFalse()
    {
        // Given
        var notExistingCategory = makeCategory();
        _categoryRepoMock.Setup(r => r.DeleteAsync(It.IsAny<category>()))
            .ReturnsAsync(false);

        // When
        var result = await _sut.delete(notExistingCategory);

        // Then
        Assert.False(result);
        _categoryRepoMock.Verify(r => r.DeleteAsync(notExistingCategory), Times.Once);
    }



}