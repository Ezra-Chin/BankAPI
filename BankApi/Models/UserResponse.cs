using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankApi.Models
{
    //information that can be returned to user
    public class UserResponse
    {
        public int Id {get; set;}
        public string Username {get; set;} = "";
        public string Name {get; set;} = "";
        public string Email {get; set;} = "";
        public string Address {get; set;} = "" ;
        public string Phone {get; set;} = "";
        public string PictureURL {get; set;} = "";
        public bool IsActive { get; set; }
    }
}