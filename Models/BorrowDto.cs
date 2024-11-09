using AutoMapper;
using Entities;
using WebFramework.Api;

namespace LibraryApplication_Api.Models
{
    public class BorrowDto : BaseDto<BorrowDto, Borrow>
    {
        public BorrowDto()
        {
            TakeDate = DateOnly.FromDateTime(DateTime.Now);
        }
        public DateOnly TakeDate { get; set; }
        public DateOnly ReturnDate { get; set; }
        public StatusType BorrowStatus { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
    }
    public class BorrowSelectDto : BaseDto<BorrowSelectDto, Borrow>
    {
        public BorrowSelectDto()
        {
            TakeDate = DateOnly.FromDateTime(DateTime.Now);
        }
        public DateOnly TakeDate { get; set; }
        public DateOnly ReturnDate { get; set; }
        public StatusType BorrowStatus { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        //public int NumberOfBorrow { get; set; }

        //public override void CustomMappings(IMappingExpression<Borrow, BorrowSelectDto> mappingExpression)
        //{
        //    mappingExpression.ForMember(
        //            dest => dest.NumberOfBorrow,
        //            config => config.MapFrom(src => $"{src.Book.NumberOfBorrow}"));
        //}
    }
    public class BorrowDetailsDto : BaseDto<BorrowDetailsDto, Borrow>
    {
        public DateOnly TakeDate { get; set; }
        public DateOnly ReturnDate { get; set; }
        public StatusType BorrowStatus { get; set; }

        public string BookTitle { get; set; }
        public string BookAuthor { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }
        public int UserAge { get; set; }

        public override void CustomMappings(IMappingExpression<Borrow, BorrowDetailsDto> mappingExpression)
        {
            mappingExpression.ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book.Title))
                .ForMember(dest => dest.BookAuthor, opt => opt.MapFrom(src => src.Book.Author))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.UserAge, opt => opt.MapFrom(src => src.User.Age));
        }
    }


}
