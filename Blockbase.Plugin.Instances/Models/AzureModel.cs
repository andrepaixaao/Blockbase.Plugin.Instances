using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Blockbase.Plugin.Instances.Models
{
    [DisplayName("Azure")]

    public class AzureModel
    {
        [DisplayName("Tenant Id")]
        [Required]
        [RegularExpression("^[a-zA-Z0-9_]{8}[-][a-zA-Z0-9]{4}[-][a-zA-Z0-9]{4}[-][a-zA-Z0-9]{4}[-][a-zA-Z0-9]{12}$", ErrorMessage = "Field incorrect")]
        public string TenantId { get; set; }
        [DisplayName("Client Id")]
        [Required]
        [RegularExpression("^[a-zA-Z0-9_]{8}[-][a-zA-Z0-9]{4}[-][a-zA-Z0-9]{4}[-][a-zA-Z0-9]{4}[-][a-zA-Z0-9]{12}$", ErrorMessage = "Field incorrect")]
        public string ClientId { get; set; }
        [DisplayName("Client Secret")]
        [Required]
        [RegularExpression("[\\s\\S]{34}", ErrorMessage = "Field incorrect")]
        public string ClientSecret { get; set; }
        [DisplayName("Resource Group Name")]
        [Required]
        [RegularExpression("[\\s\\S]*", ErrorMessage = "Field incorrect")]
        public string ResourceGroupName { get; set; }
    }
}
