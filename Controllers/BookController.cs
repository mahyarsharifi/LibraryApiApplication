using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Exeptions;
using Data.Contracts;
using Entities;
using LibraryApplication_Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Api;

namespace LibraryApplication_Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IRepository<Book> _bookRepository;
        private readonly IMapper _mapper;

        public BookController(IRepository<Book> bookRepository, IMapper mapper)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<BookDto>>> Get(CancellationToken cancellationToken)
        {
            var list = await _bookRepository.TableNoTracking.ProjectTo<BookDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (list == null)
                return NotFound();

            return Ok(list);
        }

        [HttpGet("getById/{id}")]
        public async Task<ApiResult<BookSelectDto>> Get(int id, CancellationToken cancellationToken)
        {
            var dto = await _bookRepository.TableNoTracking.ProjectTo<BookSelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (dto == null)
                throw new BadRequestExeption("کتاب مورد نظر یافت نشد");

            return dto;
        }

        //[HttpGet("{bookCategoryId:int}")]
        [HttpGet("searchByBookCategoryId/{bookCategoryId}")]
        public async Task<ActionResult<List<BookSelectDto>>> GetByBookCategoryId(int bookCategoryId, CancellationToken cancellationToken)
        {
            var dto = await _bookRepository.TableNoTracking.ProjectTo<BookSelectDto>(_mapper.ConfigurationProvider)
                .Where(x => x.BookCategoryId == bookCategoryId).ToListAsync(cancellationToken);

            //if (dto.Count == 0)
            //throw new Exception("No Book Was Found With This Category's Id!");

            if (dto.Count == 0)
                throw new BadRequestExeption("کتاب مورد نظر با این آیدی دسته بندی یافت نشد");

            return dto;
        }

        [HttpGet("searchByAuthorName/{author}")]
        public async Task<ActionResult<List<BookSelectDto>>> GetByAuthor(string author, CancellationToken cancellationToken)
        {
            var dto = await _bookRepository.TableNoTracking.ProjectTo<BookSelectDto>(_mapper.ConfigurationProvider)
                .Where(x => x.Author == author).ToListAsync(cancellationToken);

            if (dto.Count == 0)
                throw new BadRequestExeption("کتاب مورد نظر با این نام نویسنده یافت نشد");

            return dto;
        }

        [HttpPost]
        public async Task<ApiResult<BookSelectDto>> Create(BookDto dto, CancellationToken cancellationToken)
        {
            var model = dto.ToEntity(_mapper);

            if (_bookRepository.Context.Set<Book>().Any(x => x.Title == model.Title))
                throw new BadRequestExeption("امکان ثبت کتاب تکراری نمی باشد");

            var categoryExists = await _bookRepository.Context.Set<BookCategory>()
                .AnyAsync(x => x.Id == model.BookCategoryId, cancellationToken);

            if (!categoryExists)
            {
                throw new BadRequestExeption("این دسته بندی وجود ندارد");
            }

            await _bookRepository.AddAsync(model, cancellationToken);
            var resultDto = await _bookRepository.TableNoTracking.ProjectTo<BookSelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            //model.book

            return resultDto;
        }

        [HttpPut("{id}")]
        public async Task<ApiResult<BookSelectDto>> Update(int id, BookUpdateDto dto, CancellationToken cancellationToken)
        {
            var model = await _bookRepository.GetByIdAsync(cancellationToken, id);

            if (model == null)
                return NotFound();

            if (model.BookCategoryId == null)
                throw new BadRequestExeption("دسته بندی نمیتواند خالی باشد");

            //model = dto.ToEntity(_mapper, model);
            model.Title = dto.Title;
            model.Author = dto.Author;
            model.NumberOfPages = dto.NumberOfPages;
            model.PublicationDate = dto.PublicationDate;
            model.BookCategoryId = dto.BookCategoryId;

            await _bookRepository.UpdateAsync(model, cancellationToken);

            var resultDto = await _bookRepository.TableNoTracking.ProjectTo<BookSelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            return resultDto;
        }

        [HttpDelete("{id}")]
        public async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var model = await _bookRepository.GetByIdAsync(cancellationToken, id);

            if (model == null)
                return NotFound();

            if (model.BorrowStatus == StatusType.InBorrow || model.BorrowStatus == StatusType.GivenBack)
                throw new BadRequestExeption("با توجه به وضعیت امانت این کتاب، امکان حذف این کتاب وجود ندارد");

            await _bookRepository.DeleteAsync(model, cancellationToken);

            return Ok();
        }
    }
}
