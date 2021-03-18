using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Blockbase.Plugin.Instances.Models
{
    [DisplayName("Amazon Web Service")]

    public class AmazonWebServiceModel
    {
        [DisplayName("Acess Key")]
        [Required]
        [RegularExpression("[\\s\\S]{20}", ErrorMessage = "Field incorrect")]
        public string AcessKey { get; set; }

        [DisplayName("Secret Key")]
        [Required]
        [RegularExpression("[\\s\\S]{40}", ErrorMessage = "Field incorrect")]
        public string SecretKey { get; set; }
       
    }
}
