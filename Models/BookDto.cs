using AutoMapper;
using Entities;
using WebFramework.Api;

namespace LibraryApplication_Api.Models
{
    public class BookDto : BaseDto<BookDto, Book> // => Post
    {
        public string Title { get; set; }
        public string PublicationDate { get; set; }
        public string Author { get; set; }
        public int NumberOfPages { get; set; }
        public StatusType BorrowStatus { get; set; }
        public int BookCategoryId { get; set; }
    }
    public class BookSelectDto : BaseDto<BookSelectDto, Book>
    {
        public string Title { get; set; }
        public string PublicationDate { get; set; }
        public string Author { get; set; }
        public int NumberOfPages { get; set; }
        public int NumberOfBorrow { get; set; }
        public StatusType BorrowStatus { get; set; }
        public int BookCategoryId { get; set; }
        public string BookCategoryName { get; set; } //=> BookCategory.Name
        public string TitleWithCategoryName { get; set; } // => mapped from "Title (Category.Name)"

        public override void CustomMappings(IMappingExpression<Book, BookSelectDto> mappingExpression)
        {
            mappingExpression.ForMember(
                    dest => dest.TitleWithCategoryName,
                    config => config.MapFrom(src => $"{src.Title}-{src.BookCategory.Name}"));
        }
    }
    public class BookUpdateDto : BaseDto<BookUpdateDto, Book>
    {
        public string Title { get; set; }
        public string PublicationDate { get; set; }
        public string Author { get; set; }
        public int NumberOfPages { get; set; }
        public int BookCategoryId { get; set; }
    }
}
