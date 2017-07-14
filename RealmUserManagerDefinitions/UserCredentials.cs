using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace QuoteMyDay.SharedClasses
{


    public class UserCredentials
    {
        [Required]
        [RegularExpression(".*@.*")]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        public string Language { get; set; }
    }
}
