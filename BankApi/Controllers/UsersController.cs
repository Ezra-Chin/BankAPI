
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankAPI.Data;

[ApiController]
[Route("api/users")]
public class UsersController : Controller
{
    private readonly DBManager _context;

    //pfp folder (both user uploads and the default one if user didnt upload any)
    private const string ProfilePictureFolder = "ProfilePictures";
    private const string DefaultProfilePicture = "ProfilePictures/default.png";
    private static readonly string[] IsPictureType = {".jpg", ".jpeg", ".png"};

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

    // POST api/users
    //register new user
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Username,Name,Email,Address,Phone,PictureURL,Password")] UserCreateProfile userCreateProfile)
    {
        //assumption: email address are not case sensitive
        string email = userCreateProfile.Email.Trim().ToLower();

        //to be changed for #7
        if (!IsPictureType(userCreateProfile.PictureURL))
        {
            return BadRequest("Picture must be in .jpg, .jpeg or .png format");
        }

        bool usernameExists = await _context.Users.AnyAsync(u => u.Username == userCreateProfile.Username)
        if (usernameExists)
        {
            return Conflict("This username is already taken");
        }

        bool emailExists = await _context.Users.AnyAsync(u => u.Email == userCreateProfile.Email)
        if (emailExists)
        {
            return Conflict("This email address is already taken");
        }


        byte[] salt = HashingHelper.GenerateSalt();
        string hashedPassword = HashingHelper.HashPassword(userCreateProfile.Password, salt);

        //save picture file 
        string profilePicturePath = DefaultProfilePicture;
        if (userCreateProfile.PictureURL != null)
        {
            profilePicturePath = await SaveProfilePicture(userCreateProfile.PictureURL);
        }

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
        return CreatedAtAction(
            nameof(GetUserByUsername),
            new { username = user.Username },
            uResponse(user)
        );
    }


    // GET api/users/"username"
    //get user profile using username
    [HttpGet("{username}")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        //find user by username
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    
        //return 404 iif user does not exist
        if (user == null)
        {
            return NotFound();
        }

        return Ok(uResponse(user));
    }

    // GET api/users/"email"
    //get user profile using email
    [HttpGet("{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        //find user by email
        string email = email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    
        //return 404 iif user does not exist
        if (user == null)
        {
            return NotFound();
        }

        return Ok(uResponse(user));
    }

    // PUT api/users/"username"
    //update user profile
    //note to self -> [FromForm] handles pictures directly, [FromBody] cannot read raw files & needs a Base64 text string
    [HttpPut("{username}")]
    public async Task<IActionResult> Update(string username, [FromForm] UserUpdateProfile userUpdateProfile)
    {
        //find user by username
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    
        //return 404 iif user does not exist
        if (user == null)
        {
            return NotFound();
        }

        if (!IsPictureType(userCreateProfile.PictureURL))
        {
            return BadRequest("Picture must be in .jpg, .jpeg or .png format");
        }

        //only change if its not taken cause these are meant to be unique (ref: Data/DBManager.cs)
        if (userUpdateProfile.username != null)
        {
            string username = userUpdateProfile.Username;
            bool usernameTaken = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameTaken)
            {
                return Conflict("This username is taken already.");
            }
            user.Username = username;
        }

        if (userUpdateProfile.Email != null)
        {
            string email = userUpdateProfile.Email;
            bool emailTaken = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
            {
                return Conflict("This email address is taken already.");
            }
            user.Email = email;
        }

        //new name, address, phone
        if (userUpdateProfile.Name != null)
        {
            user.Name = userUpdateProfile.Name;
        }

        if (userUpdateProfile.Address != null)
        {
            user.Address = userUpdateProfile.Address;
        }

        if (userUpdateProfile.Phone != null)
        {
            user.Phone = userUpdateProfile.Phone;
        }

        //new profile picture
        string? previousProfilePicture = null;
        if (userUpdateProfile.Picture != null)
        {
            previousProfilePicture = user.PictureURL;
            user.PictureURL = await SaveProfilePicture(userUpdateProfile.Picture);
        }

        //new password
        if (userUpdateProfile.Password != null)
        {
            byte[] salt = HashingHelper.GenerateSalt();
            user.PasswordHash = HashingHelper.HashPassword(userUpdateProfile.Password, salt);
            user.PasswordSalt = Convert.ToBase64String(salt);
        }

        await _context.SaveChangesAsync();

        //delete previous pfp
        DeleteProfilePicture(previousProfilePicture);

        //will return 204
        return NoContent();
    }

    // POST: USERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    /*[HttpPost]
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
    }*/

    //check picture type
    private static bool IsPictureType(IFormFile? profilepicture)
    {
        if (profilepicture == null)
        {
            return true;
        }
        //get file type
        string extension = Path.GetExtension(profilePicture.FileName).ToLower();
        return IsPictureType.Contains(extension);

        //else it will return 400
    }

    //save pictureand return path to store in dbs
    public async Task<string> SaveProfilePicture(IFormFile profilePicture, int id)
    {
        //ref source: https://stackoverflow.com/questions/4087564/how-can-i-save-an-image-to-the-file-system
        if (!Directory.Exists(ProfilePictureFolder))
        {
            Directory.CreateDirectory(ProfilePictureFolder);
        }

        //save filename as username+id
        string filename = Path.GetFileName(id) + "_PFP" + Path.GetExtension(profilePicture.FileName).ToLower();

        return ProfilePictureFolder + "/" + filename;
    }

    //return all except for password hash & salt
    private static UserResponse uResponse (User user)
    {
        return new UserResponse
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
    }
}