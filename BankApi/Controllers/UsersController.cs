
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
    //register new user
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Username,Name,Email,Address,Phone,PictureURL,Password")] UserCreateProfile userCreateProfile)
    {
        //check whether user submitted information is invalid
        if (!ModelState.IsValid)
        {
            //IM NOT SURE ABOUT THIS ENTIRE ERROR BLOCK
            var details = new Dictionary<string, string[]>();
            
            //go thru each field in ModelState
            for each (var item in ModelState)
            {
                //check if the field has any validation errors
                if (item.Value.Errors.Count > 0)
                {
                    //store field error msg in details dictionary
                    details[item.Key] = item.Value.Errors
                        .Select(error => 
                            //use a default msg if error msg is empty
                            string.IsNullorEmpty(error.ErrorMessage) 
                                ? "Invalid Value" : error.ErrorMessage)
                        //convert error msg into array
                        .ToArray();
                }
            }

            return BadRequest (new ApiError(
                "Validation Error",
                "One or more fields are invalid",
                details
            ));
        }

        //THIS TOO
        bool usernameExist = await _context.Users.AnyAsync(u => u.Username == userCreateProfile.Username);
        if (usernameExist)
        {
            return Conflict (new ApiError(
                "Duplicate Username",
                "This username is already taken"
            ));
        }

        //THIS TOO
        bool emailExist = await _context.Users.AnyAsync(u => u.Email == userCreateProfile.Email);
        if (emailExist)
        {
            return Conflict (new ApiError(
                "Duplicate Email",
                "This email is already taken"
            ));
        }

        byte[] salt = HashingHelper.GenerateSalt();
        string hashedPassword = HashingHelper.HashPassword(userCreateProfile.Password, salt);

        //create a user tntity to store in dbs
        var user = new User
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
        
        //return RedirectToAction(nameof(Index));

        //returns 201 created with a location header
        var userResponse = new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            Address = user.Address,
            Phone = user.Phone,
            PictureURL = user.PictureURL,
            IsActive = user.IsActive
        };

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            userResponse
        );
    }
}

    // GET: USERS/Edit/5
    //get user profile using id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        //find user by id
        var user = await _context.Users.FindAsync(id);

        //return 404 iif user does not exist
        if (user == null)
        {
            return NotFound(new ApiError("User Not Found", "User does not exist"));
        }

        //if exist
        var userResponse = new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            Address = user.Address,
            Phone = user.Phone,
            PictureURL = user.PictureURL,
            IsActive = user.IsActive
        };

        
        /*if (user == null)
        {
            return NotFound();
        }*/
        
        return Ok(userResponse);
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
