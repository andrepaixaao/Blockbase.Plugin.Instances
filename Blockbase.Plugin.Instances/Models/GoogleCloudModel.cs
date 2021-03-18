using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Blockbase.Plugin.Instances.Models
{
    [DisplayName("Google Cloud")]
    public class GoogleCloudModel
    {
        [JsonProperty("type")]
        [DisplayName("Type")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string Type { get; set; }

        [JsonProperty("project_id")]
        [DisplayName("Project Id")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string ProjectId { get; set; }

        [JsonProperty("private_key_id")]
        [DisplayName("Project Key Id")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string ProjectKeyId { get; set; }
        [JsonProperty("private_key")]
        [DisplayName("Private Key")]
        [Required]
        [RegularExpression("[^A-Za-z0-9]+", ErrorMessage = "Only letters and numbers")]
        public string PrivateKey { get; set; }
        [JsonProperty("client_email")]
        [DisplayName("Client Email")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string ClientEmail { get; set; }
        [JsonProperty("client_id")]
        [DisplayName("Client Id")]
        [Required]
        [RegularExpression("^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$", ErrorMessage = "Wrong input")]
        public string ClientId { get; set; }
        [JsonProperty("auth_uri")]
        [DisplayName("Auth Uri")]
        [Required]
        [RegularExpression("[0-9]", ErrorMessage = "Only numbers")]
        public string AuthUri { get; set;}
        [JsonProperty("token_uri")]
        [DisplayName("Token Uri")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string TokenUri { get; set; }
        [JsonProperty("auth_provider_x509_url")]
        [DisplayName("Auth Provider")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string AuthProvider { get; set; }
        [JsonProperty("client_x509_cert_url")]
        [DisplayName("Client Certificate")]
        [Required]
        [RegularExpression("[\\s\\S]", ErrorMessage = "Required field")]
        public string ClientCertificate { get; set; }


     

    }
}
