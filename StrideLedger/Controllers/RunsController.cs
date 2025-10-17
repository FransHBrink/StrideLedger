using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrideLedger.Data;
using StrideLedger.Models;
using StrideLedger.Models.Dtos;
using System.Security.Claims;

namespace StrideLedger.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RunsController : ControllerBase
    {
        private readonly ShoeContext _context;

        public RunsController(ShoeContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                   ?? throw new UnauthorizedAccessException("User identifier not found in token.");
        }

        [HttpPost]
        public async Task<ActionResult<Run>> CreateRun([FromBody] CreateRun createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var shoe = await _context.Shoes.FindAsync(createDto.ShoeId);
            if (shoe == null) return BadRequest("Shoe not found");
            if (shoe.OwnerId != GetCurrentUserId()) return Forbid();

            double distanceMile = createDto.DistanceMile ?? (createDto.DistanceKm * 0.621371);

            var run = new Run
            {
                ShoeId = createDto.ShoeId,
                Date = createDto.Date,
                DistanceKm = createDto.DistanceKm,
                DistanceMile = distanceMile
            };

            shoe.CurrentMileage += run.DistanceKm;

            _context.Runs.Add(run);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRun), new { id = run.RunId }, run);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Run>> GetRun(int id)
        {
            var run = await _context.Runs.Include(r => r.Shoe).FirstOrDefaultAsync(r => r.RunId == id);
            if (run == null) return NotFound();
            if (run.Shoe == null || run.Shoe.OwnerId != GetCurrentUserId()) return Forbid();
            return run;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Run>>> GetAllRuns()
        {
            var userId = GetCurrentUserId();
            return await _context.Runs
                                 .Include(r => r.Shoe)
                                 .Where(r => r.Shoe != null && r.Shoe.OwnerId == userId)
                                 .ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRun(int id, [FromBody] Run updatedRun)
        {
            if (id != updatedRun.RunId) return BadRequest("Run ID mismatch");

            var existingRun = await _context.Runs.Include(r => r.Shoe).FirstOrDefaultAsync(r => r.RunId == id);
            if (existingRun == null) return NotFound("Run not found");
            if (existingRun.Shoe == null || existingRun.Shoe.OwnerId != GetCurrentUserId()) return Forbid();

            var newShoe = await _context.Shoes.FindAsync(updatedRun.ShoeId);
            if (newShoe == null) return BadRequest("Shoe not found");
            if (newShoe.OwnerId != GetCurrentUserId()) return Forbid();

            double mileageDifference = updatedRun.DistanceKm - existingRun.DistanceKm;
            if (existingRun.ShoeId != updatedRun.ShoeId)
            {
                var oldShoe = existingRun.Shoe;
                if (oldShoe != null)
                {
                    oldShoe.CurrentMileage -= existingRun.DistanceKm;
                    if (oldShoe.CurrentMileage < 0) oldShoe.CurrentMileage = 0;
                }
                newShoe.CurrentMileage += updatedRun.DistanceKm;
            }
            else
            {
                newShoe.CurrentMileage += mileageDifference;
            }

            existingRun.Date = updatedRun.Date;
            existingRun.DistanceKm = updatedRun.DistanceKm;
            existingRun.ShoeId = updatedRun.ShoeId;
            existingRun.DistanceMile = updatedRun.DistanceMile;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRun(int id)
        {
            var run = await _context.Runs.FindAsync(id);
            if (run == null) return NotFound("Run not found");

            var shoe = await _context.Shoes.FindAsync(run.ShoeId);
            if (shoe == null) return BadRequest("Shoe not found");
            if (shoe.OwnerId != GetCurrentUserId()) return Forbid();

            shoe.CurrentMileage -= run.DistanceKm;
            if (shoe.CurrentMileage < 0) shoe.CurrentMileage = 0;

            _context.Runs.Remove(run);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}