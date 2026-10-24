namespace BankWeb.Models
{
    //profile picture form part A
    public class ProfilePictureFile
    {
        public byte[] Bytes {get; set;} = new byte[0];
        public string ContentType {get; set;} = "image/png";
    }
}