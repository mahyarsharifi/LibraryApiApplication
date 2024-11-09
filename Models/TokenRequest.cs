using System.ComponentModel.DataAnnotations;

namespace LibraryApplication_Api.Models
{
    public class TokenRequest
    {
        [Required]
        public string username { get; set; }
        [Required]
        public string password { get; set; }
    }
}
