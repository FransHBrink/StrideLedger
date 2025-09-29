using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrideLedger.Data;
using StrideLedger.Models;

namespace StrideLedger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RunsController : ControllerBase
    {
        private readonly ShoeContext _context;

        public RunsController(ShoeContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Run>> CreateRun([FromBody] Run run)
        {
            // Validate the ShoeId exists
            var shoe = await _context.Shoes.FindAsync(run.ShoeId);
            if (shoe == null) return BadRequest("Shoe not found");

            // Update the shoe's mileage
            shoe.CurrentMileage += run.DistanceKm;

            // Remove the Shoe object from the run (avoid EF tracking conflicts)
            run.Shoe = null!; 

            // Add the run
            _context.Runs.Add(run);

            // Save changes (EF will track both Run and update the existing Shoe)
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRun), new { id = run.RunId }, run);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Run>> GetRun(int id)
        {
            var run = await _context.Runs.Include(r => r.Shoe).FirstOrDefaultAsync(r => r.RunId == id);
            if (run == null) return NotFound();
            return run;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Run>>> GetAllRuns()
        {
            return await _context.Runs.Include(r => r.Shoe).ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRun(int id, [FromBody] Run updatedRun)
        {
            if (id != updatedRun.RunId) return BadRequest("Run ID mismatch");

            var existingRun = await _context.Runs.FindAsync(id);

            if (existingRun == null) return NotFound("Run not found");

            // Validate the ShoeId exists
            var shoe = await _context.Shoes.FindAsync(updatedRun.ShoeId);
            if (shoe == null) return BadRequest("Shoe not found");

            // Adjust the shoe's mileage based on the difference
            double mileageDifference = updatedRun.DistanceKm - existingRun.DistanceKm;
            shoe.CurrentMileage += mileageDifference;

            // Update the run details
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

            // Adjust the shoe's mileage
            var shoe = await _context.Shoes.FindAsync(run.ShoeId);

            if (shoe != null)
            {
                shoe.CurrentMileage -= run.DistanceKm;
                if (shoe.CurrentMileage < 0) shoe.CurrentMileage = 0; // Prevent negative mileage
            }

            _context.Runs.Remove(run);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
