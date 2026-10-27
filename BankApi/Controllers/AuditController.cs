using BankApi.Data;
using BankApi.Models;
using BankWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BankApi.Controllers
{

    //set route to api/audit
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : Controller
    {
        private readonly DBManager _context;

        public AuditController(DBManager context)
        {
            _context = context;
        }

        // POST /api/audit 
        // record admin action 
        [HttpPost]
        public async Task<IActionResult> AddEntry([FromBody] AuditEntryRequest req)
        {
            //if dont have username, then throw bad reqeuest 
            if (string.IsNullOrWhiteSpace(req.AdminUsername) )
            {
                return BadRequest(new ApiError("INVALID_USERNAME", "Please provide an admin username"));
            }

            // create an auditEtnry object based on teh request 
            AuditEntry auditEntry = new AuditEntry
            {
                AdminUsername = req.AdminUsername.Trim().ToLower(),
                Action = req.Action.Trim(),
                Details = req.Details ?? "",
                CreatedAt = DateTime.UtcNow
            
            };

            // add the audit entry into the context
            _context.AuditEntry.Add(auditEntry);
            //save the database based on the context 
            await _context.SaveChangesAsync();

            //return the audit entry and Ok
            return Ok(auditEntry);
        }



        // GET /api/audit?page=1&pageSize=10
        // used to reutrn all audit actions made by admin 
        [HttpGet]
        public async Task<ActionResult<PagedResult<AuditEntry>>> GetEntries([FromQuery] int page = 1 , [FromQuery] int pageSize= 10)
        {
            //just in case the query is less than 1 
            if (page < 1)
            {
                page = 1;
            }
            //just in case page size is less stha n1 
            if (pageSize < 1)
            {
                pageSize = 1;
            }
            //view all audit entires and arrange them newest to oldest 
            var entries = _context.AuditEntry.OrderByDescending(a => a.CreatedAt);

            // create teh response that is paged 
            var result = new PagedResult<AuditEntry>
            {
                TotalCount = await entries.CountAsync(),
                Page = page,
                PageSize = pageSize,
                Items = await entries.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
            };
            //return the paginated response 
            return Ok(result);
        }



    }




}
