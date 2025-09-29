using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrideLedger.Data;
using StrideLedger.Models;

namespace StrideLedger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShoesController : ControllerBase
    {
        private readonly ShoeContext _context;

        public ShoesController(ShoeContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Shoe>> CreateShoe(Shoe shoe)
        {
            _context.Shoes.Add(shoe);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetShoe), new { id = shoe.ShoeId }, shoe);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shoe>> GetShoe(int id)
        {
            var shoe = await _context.Shoes.FindAsync(id);
            if (shoe == null)
            {
                return NotFound();
            }
            return shoe;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shoe>>> GetAllShoes()
        {
            return await _context.Shoes.ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShoe(int id, Shoe updatedShoe)
        {
            if (id != updatedShoe.ShoeId)
            {
                return BadRequest("Shoe ID mismatch");
            }

            _context.Entry(updatedShoe).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShoeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShoe(int id)
        {
            var shoe = await _context.Shoes.FindAsync(id);

            if (shoe == null)
            {
                return NotFound();
            }

            _context.Shoes.Remove(shoe);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool ShoeExists(int id)
        {
            return _context.Shoes.Any(e => e.ShoeId == id);
        }
    }
}
