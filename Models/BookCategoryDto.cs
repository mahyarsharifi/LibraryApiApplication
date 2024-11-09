using AutoMapper;
using Entities;
using WebFramework.Api;

namespace LibraryApplication_Api.Models
{
    public class BookCategoryDto : BaseDto<BookCategoryDto, BookCategory, int> // => Post
    {
        public string Name { get; set; }
    }
    public class BookCategorySelectDto : BaseDto<BookCategorySelectDto, BookCategory, int>
    {
        public string Name { get; set; }

        public override void CustomMappings(IMappingExpression<BookCategory, BookCategorySelectDto> mappingExpression)
        {
            mappingExpression.ForMember(
                    dest => dest.Name,
                    config => config.MapFrom(src => $"{src.Name}"));
        }
    }
}
