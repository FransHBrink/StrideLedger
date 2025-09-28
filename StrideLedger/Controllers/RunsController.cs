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
    }
}
