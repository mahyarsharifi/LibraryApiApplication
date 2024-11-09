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
    public class BookCategoryController : ControllerBase
    {
        private readonly IRepository<BookCategory> _repository;
        private readonly IMapper _mapper;

        public BookCategoryController(IRepository<BookCategory> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<BookCategorySelectDto>>> Get(CancellationToken cancellationToken)
        {
            var list = await _repository.TableNoTracking.ProjectTo<BookCategorySelectDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (list == null)
                return NotFound();

            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ApiResult<BookCategorySelectDto>> Get(int id, CancellationToken cancellationToken)
        {
            var dto = await _repository.TableNoTracking.ProjectTo<BookCategorySelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (dto == null)
                return NotFound();

            return dto;
        }

        [HttpPost]
        public async Task<ApiResult<BookCategorySelectDto>> Create(BookCategoryDto dto, CancellationToken cancellationToken)
        {
            var model = dto.ToEntity(_mapper);

            if (_repository.Context.Set<BookCategory>().Any(x => x.Name == model.Name))
                throw new BadRequestExeption("دسته بندی با این نام وجود دارد");

            await _repository.AddAsync(model, cancellationToken);
            var resultDto = await _repository.TableNoTracking.ProjectTo<BookCategorySelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            return resultDto;
        }

        [HttpPut("{id}")]
        public async Task<ApiResult<BookCategorySelectDto>> Update(int id, BookCategoryDto dto, CancellationToken cancellationToken)
        {
            var model = await _repository.GetByIdAsync(cancellationToken, id);

            if (model == null)
                return NotFound();

            //Mapper.Map(dto, model);
            //model = dto./*ToEntity*/(_mapper, model);

            model.Name = dto.Name;

            await _repository.UpdateAsync(model, cancellationToken);

            var resultDto = await _repository.TableNoTracking.ProjectTo<BookCategorySelectDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            return resultDto;
        }

        [HttpDelete("{id}")]
        public async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var model = await _repository.Context.Set<BookCategory>().Include(x => x.Books).FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
                return NotFound();

            if (model.Books.Count != 0)
                throw new BadRequestExeption("در این دسته بندی کتاب وجود دارد، امکان حذف نمی باشد");

            await _repository.DeleteAsync(model, cancellationToken);

            return Ok();
        }
    }
}
