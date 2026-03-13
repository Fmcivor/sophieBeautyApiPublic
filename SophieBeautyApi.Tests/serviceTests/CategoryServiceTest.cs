using Moq;
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




    


}