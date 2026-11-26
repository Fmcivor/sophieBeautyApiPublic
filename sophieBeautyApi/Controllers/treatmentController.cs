using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.ServiceInterfaces;
using sophieBeautyApi.services;

namespace sophieBeautyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class treatmentController : ControllerBase
    {
        private readonly ITreatmentService _treatmentService;

        public treatmentController(ITreatmentService treatmentService)
        {
            this._treatmentService = treatmentService;
        }

        [HttpGet("AllTreatments")]
        public async Task<ActionResult> getAll()
        {
            var treatments = await _treatmentService.getAll();

            return Ok(treatments);
        }

        [Authorize]
        [HttpPost("Create")]
        public async Task<ActionResult> create([FromBody] treatment newTreatment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var treatment = await _treatmentService.create(newTreatment);

            return CreatedAtAction(nameof(create), treatment);
        }

        [Authorize]
        [HttpPut("Update")]
        public async Task<ActionResult> update([FromBody] treatment updatedTreatment)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (updatedTreatment.Id == null)
            {
                return NotFound();
            }

            bool succeeded = await _treatmentService.update(updatedTreatment);

            if (!succeeded)
            {
                return StatusCode(500, "An internal error occurred while updating the treatment.");
            }

            return Ok();
        }

        [Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Id is required.");
            }

            bool succeeded = await _treatmentService.delete(id);

            if (!succeeded)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}