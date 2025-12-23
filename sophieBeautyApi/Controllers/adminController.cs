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
    public class adminController : ControllerBase
    {
        private readonly adminService _adminService;
        private readonly jwtTokenHandler _tokenHandler;

        public adminController(adminService adminService, jwtTokenHandler tokenHandler)
        {
            _adminService = adminService;
            _tokenHandler = tokenHandler;
        }


        // [Authorize]
        // [HttpPost("register")]
        // public async Task<ActionResult> register(adminDTO registerDto)
        // {
        //     if (!ModelState.IsValid)
        //     {
        //         return BadRequest(ModelState);
        //     }

        //     await _adminService.register(registerDto);

        //     return CreatedAtAction(nameof(register), "account created");

        // }


        [HttpPost("login")]
        public async Task<ActionResult> login(adminDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            admin validAdmin = await _adminService.validateLogin(loginDto);

            if (validAdmin == null)
            {
                return BadRequest("Invalid username or password");
            }

            var token = _tokenHandler.generateToken(validAdmin);

            // var cookieOptions = new CookieOptions
            // {
            //     HttpOnly = true,
            //     Secure = true,
            //     SameSite = SameSiteMode.None,
            //     Expires = DateTime.UtcNow.AddHours(2)
            // };

            // HttpContext.Response.Cookies.Append("jwt", token, cookieOptions);

            return Ok(new {jwt=token});


        }


        [Authorize]
        [HttpGet("verify")]
        public async Task<ActionResult<bool>> verifyJwt()
        {
            return Ok(true);
        }


        [Authorize]
        [HttpPost("remindBookings")]
        public async Task<ActionResult> remindBookings()
        {
            await _adminService.remindBookings();
            return Ok();
        }
    }
}