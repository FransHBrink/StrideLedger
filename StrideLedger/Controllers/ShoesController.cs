using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrideLedger.Data;
using StrideLedger.Models;
using System.Security.Claims;

namespace StrideLedger.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShoesController : ControllerBase
    {
        private readonly ShoeContext _context;

        public ShoesController(ShoeContext context)
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
        public async Task<ActionResult<Shoe>> CreateShoe([FromBody] Shoe shoe)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            shoe.OwnerId = GetCurrentUserId();
            _context.Shoes.Add(shoe);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetShoe), new { id = shoe.ShoeId }, shoe);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shoe>> GetShoe(int id)
        {
            var shoe = await _context.Shoes.FindAsync(id);
            if (shoe == null) return NotFound();
            if (shoe.OwnerId != GetCurrentUserId()) return Forbid();
            return shoe;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shoe>>> GetAllShoes()
        {
            var userId = GetCurrentUserId();
            return await _context.Shoes.Where(s => s.OwnerId == userId).ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShoe(int id, [FromBody] Shoe updatedShoe)
        {
            if (id != updatedShoe.ShoeId)
            {
                return BadRequest("Shoe ID mismatch");
            }

            var existing = await _context.Shoes.FindAsync(id);
            if (existing == null) return NotFound();
            if (existing.OwnerId != GetCurrentUserId()) return Forbid();

            existing.Name = updatedShoe.Name;
            existing.Description = updatedShoe.Description;
            existing.Brand = updatedShoe.Brand;
            existing.Model = updatedShoe.Model;
            existing.TargetMileage = updatedShoe.TargetMileage;
            existing.CurrentMileage = updatedShoe.CurrentMileage;

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Shoes.Any(e => e.ShoeId == id))
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

            if (shoe == null) return NotFound();
            if (shoe.OwnerId != GetCurrentUserId()) return Forbid();

            _context.Shoes.Remove(shoe);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}