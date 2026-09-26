using AutoMapper;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Application.Mappings;

public sealed class DomainMappingProfile : Profile
{
    public DomainMappingProfile()
    {
        CreateMap<Book, BookDto>()
            .ForMember(destination => destination.CategoryName,
                options => options.MapFrom(source => source.Category.Name))
            .ForMember(destination => destination.RowVersion,
                options => options.MapFrom(source => Convert.ToBase64String(source.RowVersion)));

        CreateMap<Category, CategoryDto>();
    }
}
