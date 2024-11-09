using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Exeptions;
using Data.Contracts;
using Entities;
using LibraryApplication_Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryApplication_Api.Controllers
{
    //[AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowController : ControllerBase
    {
        private readonly IRepository<Borrow> _repository;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public BorrowController(IRepository<Borrow> repository, UserManager<User> userManager, IMapper mapper)
        {
            _repository = repository;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<BorrowDetailsDto>>> Get(CancellationToken cancellationToken)
        {
            var list = await _repository.TableNoTracking.ProjectTo<BorrowDetailsDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (list == null)
                return NotFound();

            return Ok(list);
        }

        //[HttpGet("borrowInBoroorwDetails")]
        //public async Task<ActionResult<List<BorrowSelectDto>>> GetBorrowInBoroorwDetails(CancellationToken cancellationToken)
        //{
        //    var list = await _repository.TableNoTracking.ProjectTo<BorrowSelectDto>(_mapper.ConfigurationProvider)
        //        .Where(x => x.BorrowStatus == StatusType.InBorrow).ToListAsync(cancellationToken);

        //    if (list == null)
        //        return NotFound();

        //    return Ok(list);
        //}

        //[HttpGet("borrowReturnedDetails")]
        //public async Task<ActionResult<List<BorrowSelectDto>>> GetBorrowReturnedDetails(CancellationToken cancellationToken)
        //{
        //    var list = await _repository.TableNoTracking.ProjectTo<BorrowSelectDto>(_mapper.ConfigurationProvider)
        //        .Where(x => x.BorrowStatus == StatusType.GivenBack).ToListAsync(cancellationToken);

        //    if (list == null)
        //        return NotFound();

        //    return Ok(list);
        //}

        [HttpGet("userBorrows")]
        public async Task<ActionResult<List<BorrowDetailsDto>>> GetUserBorrows(bool? isActive, CancellationToken cancellationToken)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _userManager.FindByIdAsync(userId.ToString());

            var query = _repository.TableNoTracking
                .Where(b => b.UserId == userId);

            if (query.Count() == 0)
                throw new BadRequestExeption("امانتی برای این کاربر وجود ندارد");

            if (isActive == true)
            {
                query = query.Where(b => b.BorrowStatus == StatusType.InBorrow);
            }
            else if (isActive == false)
            {
                query = query.Where(b => b.BorrowStatus == StatusType.GivenBack);
            }   

            var borrows = await query.ProjectTo<BorrowDetailsDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Ok(borrows);
        }

        [HttpPost]
        public async Task<ActionResult<BorrowSelectDto>> Borrowing(BorrowDto dto, CancellationToken cancellationToken)
        {
            var model = dto.ToEntity(_mapper);

            var book = await _repository.Context.Set<Book>().FindAsync(model.BookId);
            if (book == null)
            {
                throw new BadRequestExeption("کتاب مورد نظر پیدا نشد");
            }
            if (book.BorrowStatus == StatusType.InBorrow)
            {
                throw new BadRequestExeption("کتاب مورد نظر در دست امانت است");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (model.ReturnDate >= DateOnly.FromDateTime(DateTime.Now).AddDays(15))
                throw new BadRequestExeption("شما نمیتوانید بیشتر از 15 روز امانت بگیرید");

            var userNumberOfBorrow = await _repository.Context.Set<User>()
                .FirstOrDefaultAsync(b => b.Id == userId && b.NumberOfBorrow >= 3, cancellationToken);

            if (userNumberOfBorrow != null)
            {
                throw new BadRequestExeption("شما نمیتوانید بیشتر از سه کتاب امانت بگیرید");
            }

            model.UserId = userId;
            model.BorrowStatus = StatusType.InBorrow;


            book.NumberOfBorrow += 1;
            book.BorrowStatus = StatusType.InBorrow;
            user.NumberOfBorrow += 1;

            await _repository.AddAsync(model, cancellationToken);

            await _repository.Context.SaveChangesAsync(cancellationToken);

            var resultDto = await _repository.TableNoTracking.ProjectTo<BorrowSelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            return resultDto;
        }

        [HttpPut("giveBack/{bookId}")]
        public async Task<ActionResult> GiveBack(int bookId, CancellationToken cancellationToken)
        {
            var book = await _repository.Context.Set<Book>().FindAsync(bookId);

            if (book == null)
            {
                throw new BadRequestExeption("کتاب مورد نظر یافت نشد");
            }

            if (book.BorrowStatus == StatusType.GivenBack)
            {
                throw new BadRequestExeption("کتاب قبلا پس داده شده است");
            }

            var model = await _repository.Context.Set<Borrow>().FirstOrDefaultAsync(x => x.BookId == book.Id);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null && user.NumberOfBorrow > 0)
            {
                user.NumberOfBorrow -= 1;
            }

            book.BorrowStatus = StatusType.GivenBack;

            model.BorrowStatus = StatusType.GivenBack;
            model.ReturnDate = DateOnly.FromDateTime(DateTime.Now);

            await _repository.Context.SaveChangesAsync(cancellationToken);

            return Ok("کتاب با موفقیت پس داده شد");
        }
    }
}
