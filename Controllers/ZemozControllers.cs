using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZemozSmart.Data;
using ZemozSmart.Models;

namespace ZemozSmart.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SupportersController : ControllerBase
    {
        private readonly ZemozDbContext _context;

        public SupportersController(ZemozDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supporter>>> GetSupporters()
        {
            return await _context.Supporters.Include(s => s.Cards).ToListAsync();
        }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        private readonly ZemozDbContext _context;

        public CardsController(ZemozDbContext context)
        {
            _context = context;
        }

        [HttpPost("auto-supporter")]
        public async Task<ActionResult<Card>> PostCardWithAutoSupporter(CardType type)
        {
            // Count existing cards of this type to determine the next SerialNumber (BOSSA-0001, ASSA-0001, ...)
            int cardCountByType = await _context.Cards.CountAsync(c => c.Type == type);
            string serialNumber = $"{type.ToString().ToUpper()}-{(cardCountByType + 1):D4}";

            var supporter = new Supporter
            {
                Name = serialNumber,
                PhoneNumber = "00000000" // Placeholder
            };

            _context.Supporters.Add(supporter);
            await _context.SaveChangesAsync(); // Save to get the SupporterId

            var card = new Card
            {
                SupporterId = supporter.Id,
                SerialNumber = serialNumber,
                Type = type,
                RemainingScans = 20,
                ExpiryDate = DateTime.MaxValue // No expiration
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            return Ok(new { 
                CardId = card.Id, 
                SerialNumber = card.SerialNumber,
                SupporterId = supporter.Id, 
                SupporterName = supporter.Name,
                Type = card.Type.ToString()
            });
        }

        [HttpPost("refill")]
        public async Task<IActionResult> RefillCard(string serialNumber, int scansToAdd)
        {
            var card = await _context.Cards
                .Include(c => c.Supporter)
                .FirstOrDefaultAsync(c => c.SerialNumber == serialNumber);

            if (card == null) return NotFound("Carte non trouvée.");

            card.RemainingScans += scansToAdd;
            
            // If the card was blocked because it had 0 scans, unblock it
            if (card.RemainingScans > 0 && card.IsBlocked)
            {
                card.IsBlocked = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = $"Recharge réussie. {scansToAdd} scans ajoutés.", 
                SerialNumber = card.SerialNumber,
                NewTotal = card.RemainingScans,
                Supporter = card.Supporter?.Name,
                IsBlocked = card.IsBlocked
            });
        }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ScansController : ControllerBase
    {
        private readonly ZemozDbContext _context;

        public ScansController(ZemozDbContext context)
        {
            _context = context;
        }

        [HttpPost("by-serial/{serialNumber}")]
        public async Task<IActionResult> ScanCard(string serialNumber)
        {
            var card = await _context.Cards
                .Include(c => c.Supporter)
                .FirstOrDefaultAsync(c => c.SerialNumber == serialNumber);

            if (card == null) return NotFound("Carte non trouvée.");

            if (card.IsBlocked) 
                return BadRequest($"Alerte: Le supporter {card.Supporter?.Name} est bloqué.");

            if (card.RemainingScans <= 0)
            {
                card.IsBlocked = true;
                await _context.SaveChangesAsync();
                return BadRequest("Nombre de scans épuisé. Carte bloquée.");
            }

            // Perform scan
            card.RemainingScans--;
            var scan = new Scan { CardId = card.Id, ScanDate = DateTime.Now };
            _context.Scans.Add(scan);

            // If it was the last scan, block it
            if (card.RemainingScans == 0)
            {
                card.IsBlocked = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = "Scan réussi", 
                SerialNumber = card.SerialNumber,
                Remaining = card.RemainingScans,
                Supporter = card.Supporter?.Name,
                Type = card.Type.ToString()
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(int page = 1, int pageSize = 50)
        {
            var query = _context.Scans
                .Include(s => s.Card)
                .ThenInclude(c => c!.Supporter)
                .OrderByDescending(s => s.ScanDate);

            var totalItems = await query.CountAsync();
            var history = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var grouped = history
                .GroupBy(s => s.ScanDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Count = g.Count(),
                    Scans = g.Select(s => new {
                        s.Id,
                        SerialNumber = s.Card?.SerialNumber,
                        Supporter = s.Card?.Supporter?.Name,
                        Type = s.Card?.Type.ToString(),
                        Time = s.ScanDate.ToShortTimeString()
                    })
                });

            return Ok(new {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                Data = grouped
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var scans = await _context.Scans
                .Include(s => s.Card)
                .ToListAsync();

            var stats = scans
                .GroupBy(s => s.ScanDate.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new {
                    Date = g.Key.ToShortDateString(),
                    Total = g.Count(),
                    ByType = g.GroupBy(s => s.Card?.Type)
                              .Select(tg => new {
                                  Type = tg.Key.ToString(),
                                  Count = tg.Count()
                              })
                });

            return Ok(stats);
        }
    }
}
