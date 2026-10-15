
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankAPI.Data;

public class UsersController : Controller
{
    private readonly DBManager _context;

    public UsersController(DBManager context)
    {
        _context = context;
    }

    // GET: USERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Users.ToListAsync());
    }

    // GET: USERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(m => m.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // GET: USERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: USERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Username,Name,Email,Address,Phone,PictureURL,Password")] UserCreateProfile userCreateProfile)
    {
        //check whether user submitted information was correct
        if (ModelState.IsValid)
        {
            byte[] salt = HashingHelper.GenerateSalt();
            string hashedPassword = HashingHelper.HashPassword(userCreateProfile.Password, salt);

            //create a user tntity to store in dbs
            User user = new User
            {
                Username = userCreateProfile.Username,
                Name = userCreateProfile.Name,
                Email = userCreateProfile.Email,
                Address = userCreateProfile.Address,
                Phone = userCreateProfile.Phone,
                PictureURL = userCreateProfile.PictureURL,
                PasswordHash = hashedPassword,
                PasswordSalt = Convert.ToBase64String(salt),
                IsActive = true
            };

            //add user to dbs
            _context.Add(user);
            //save the changes to the dbs
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        //returns 201 created with a location header 
        return CreatedAtAction (
            nameof(GetUser),
            new { id = user.Id },
            new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone,
                PictureURL = user.PictureURL,
                IsActive = user.IsActive
            }
        );
    }

    //get a user profile using Id
    [HttpGet("{id}")]
    

    // GET: USERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // POST: USERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Username,Name,Email,Address,Phone,PictureURL,PasswordHash,PasswordSalt,IsActive,Accounts")] User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }

    // GET: USERS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(m => m.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: USERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool UserExists(int? id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
}
