using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Models
{
    [Table("RevokedTokens")]
    public class RevokedToken
    {
        [Key]
        [MaxLength(64)]
        public string Jti { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
