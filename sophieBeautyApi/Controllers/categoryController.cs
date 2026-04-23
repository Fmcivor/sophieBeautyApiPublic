using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.services;

namespace sophieBeautyApi.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class categoryController : ControllerBase
    {
        private categoryService _categoryService;

        public categoryController(categoryService categoryService)
        {
            _categoryService = categoryService;
        }



        [HttpGet]
        public async Task<ActionResult> all()
        {
            var categories = await _categoryService.getAll();

            return Ok(categories);
        }


        [HttpPost]
        public async Task<ActionResult> create([FromBody] category c)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _categoryService.create(c.name);
            if (category == null)
            {
                return Conflict("Category already exists");
            }

            

            Response.Headers.Append("Category-created-successfully", category.Id);
            return StatusCode(201, category);

        }


        [HttpDelete]
        public async Task<ActionResult> delete(category category)
        {
            bool deleted = await _categoryService.delete(category);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}