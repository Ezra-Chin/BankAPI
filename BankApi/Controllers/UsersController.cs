
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankApi.Data;

//rmv if not needed
using Microsoft.AspNetCore.Hosting;

[ApiController]
[Route("api/users")]
public class UsersController : Controller
{
    private readonly DBManager _context;
    
    //pfp folder (both user uploads and the default one if user didnt upload any)
    private const string ProfilePictureFolder = "ProfilePictures";
    private const string DefaultProfilePicture = "ProfilePictures/default.png";
    private static readonly string[] AllowedPictureType = {".jpg", ".jpeg", ".png"};

    public UsersController(DBManager context)
    {
        _context = context;
    }

    // POST api/users
    //register new user
    [HttpPost]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UserCreateProfile userCreateProfile)
    {
        //assumption: email address are not case sensitive
        string email = userCreateProfile.Email.Trim().ToLower();

        //to be changed for #7 ref: WebAPI slide 31
        if (!IsPictureType(userCreateProfile.Picture))
        {
            return BadRequest(new ApiError("PICTURE_FORMAT","Picture must be in .jpg, .jpeg or .png format"));
        }

        bool usernameExists = await _context.Users.AnyAsync(u => u.Username == userCreateProfile.Username);
        if (usernameExists)
        {
            return Conflict(new ApiError("USERNAME_TAKEN", "This username is already taken"));
        }

        bool emailExists = await _context.Users.AnyAsync(u => u.Email == userCreateProfile.Email);
        if (emailExists)
        {
            return Conflict(new ApiError("EMAIL_TAKEN", "This email address is already taken"));
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
            PictureURL = DefaultProfilePicture,
            PasswordHash = hashedPassword,
            PasswordSalt = Convert.ToBase64String(salt),
            IsActive = true
        };

        //add user to dbs
        _context.Add(user);
        //save the changes to the dbs
        await _context.SaveChangesAsync();

        //return RedirectToAction(nameof(Index));

        //if user upload a pic, save it. if not, use default.png
        if (userCreateProfile.Picture != null)
        {
            user.PictureURL = await SaveProfilePicture(userCreateProfile.Picture, user.Id);
            await _context.SaveChangesAsync();
        }
        
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
            return NotFound(new ApiError("USER_NOT_FOUND", "User does not exists"));
        }

        return Ok(uResponse(user));
    }

    // GET api/users/email/"email"
    //get user profile using email
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string Email)
    {
        //find user by email
        string email = Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    
        //return 404 iif user does not exist
        if (user == null)
        {
            return NotFound(new ApiError("USER_NOT_FOUND", "User does not exist"));
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
            return NotFound(new ApiError("USER_NOT_FOUND", "User does not exist"));
        }

        if (!IsPictureType(userUpdateProfile.Picture))
        {
            return BadRequest(new ApiError("PICTURE_FORMAT", "Picture must be in .jpg, .jpeg or .png format"));
        }

        //only change if its not taken cause these are meant to be unique (ref: Data/DBManager.cs)
        if (userUpdateProfile.Username != null)
        {
            string newUsername = userUpdateProfile.Username;
            bool usernameTaken = await _context.Users.AnyAsync(u => u.Username == newUsername && u.Id != user.Id);
            if (usernameTaken)
            {
                return Conflict(new ApiError("USERNAME_TAKEN", "This username is taken already."));
            }
            user.Username = newUsername;
        }

        if (userUpdateProfile.Email != null)
        {
            string newEmail = userUpdateProfile.Email;
            bool emailTaken = await _context.Users.AnyAsync(u => u.Email == newEmail && u.Id != user.Id);
            if (emailTaken)
            {
                return Conflict(new ApiError("EMAIL_TAKEN", "This email address is taken already."));
            }
            user.Email = newEmail;
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
        if (userUpdateProfile.Picture != null)
        {
            user.PictureURL = await SaveProfilePicture(userUpdateProfile.Picture, user.Id);
        }

        //new password
        if (userUpdateProfile.Password != null)
        {
            byte[] salt = HashingHelper.GenerateSalt();
            user.PasswordHash = HashingHelper.HashPassword(userUpdateProfile.Password, salt);
            user.PasswordSalt = Convert.ToBase64String(salt);
        }

        await _context.SaveChangesAsync();

        //will return 204
        return NoContent();
    }

    //deactivate user acc
    //PATCH api/users/"username"/deactivate
    [HttpPatch("{username}/deactivate")]
    public async Task<IActionResult> DeactivateAccount(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound(new ApiError("USER_NOT_FOUND", "User does not exist"));
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();
        //will return 204
        return NoContent();
    }

    //user login
    //POST api/users/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLogin userLogin)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userLogin.Username);

        //hash the pw and use the same salt when user signed up
        if (user != null)
        {
            byte[] salt = Convert.FromBase64String(user.PasswordSalt);
            var hashedPassword = HashingHelper.HashPassword(userLogin.Password, salt);

            if (hashedPassword == user.PasswordHash)
            {
                if (!user.IsActive)
                {
                    return UnprocessableEntity(new ApiError("ACCOUNT_DEACTIVATED", "Account is deactivated"));
                }

                //return 200
                return Ok(uResponse(user));
            }
        }

        //return 422 I suppose
        return UnprocessableEntity(new ApiError( "INVALID_USERNAME_PASSWORD" ,"Username or password is incorrect"));
    }

    //check picture type
    private static bool IsPictureType(IFormFile? profilepicture)
    {
        if (profilepicture == null)
        {
            return true;
        }
        //get file type
        string extension = Path.GetExtension(profilepicture.FileName).ToLower();
        return AllowedPictureType.Contains(extension);

        //else it will return 400
    }

    //save pictureand return path to store in dbs
    //IFormFile -> takes file uploaded by user and accesses its contents and file information
    public async Task<string> SaveProfilePicture(IFormFile profilePicture, int id)
    {
        //ref source: https://stackoverflow.com/questions/4087564/how-can-i-save-an-image-to-the-file-system
        if (!Directory.Exists(ProfilePictureFolder))
        {
            Directory.CreateDirectory(ProfilePictureFolder);
        }

        //save filename as username+id
        string filename = id + "_PFP" + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(profilePicture.FileName).ToLower();
        string path = ProfilePictureFolder + "/" + filename;

        //write the uploaded file to the folder
        //ref source: https://stackoverflow.com/questions/53857251/uploading-files-to-a-directory-with-an-asp-net-core-web-application-hosted-on-az
        using var stream = new FileStream(path, FileMode.Create);
        //{
        await profilePicture.CopyToAsync(stream);
        //}

        return path;
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