using Blockbase.Plugin.Instances.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Amazon.Runtime;
using Amazon.EC2;
using Amazon;
using Amazon.EC2.Model;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.Threading.Tasks;
using Google.Apis.Compute.v1;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Google.Apis.Services;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Blockbase.Plugin.Instances.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var test = new AzureModel();
            return View(preencherForm(test));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public static FormViewModel preencherForm(object obj)
        {
            var list = new List<InputObjectModel>();
            var DisplayClassName = (DisplayNameAttribute)obj.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault();


            foreach (var prop in obj.GetType().GetProperties())
            {
                PropertyInfo display = prop;
                DisplayNameAttribute dp = display.GetCustomAttribute<DisplayNameAttribute>();
                RegularExpressionAttribute reg = display.GetCustomAttribute<RegularExpressionAttribute>();
                string type = "text";
                if (prop.PropertyType == typeof(Int32)) type = "number";
                list.Add(new InputObjectModel()
                {
                    Name = prop.Name,
                    Type = type,
                    DisplayName = dp.DisplayName,
                    Pattern = reg.Pattern,
                    Title = reg.ErrorMessage
                });

            }
            return new FormViewModel() { Inputs = list, ClassName = obj.GetType().Name, DisplayClassName = DisplayClassName.DisplayName };

        }

        [HttpPost]
        public ViewResult Create(AmazonWebServiceModel aws)
        {
            try
            {
                BasicAWSCredentials credentials = new BasicAWSCredentials(aws.AcessKey, aws.SecretKey);
                var awsCon = new AmazonEC2Client(credentials, RegionEndpoint.USEast1);
                string amiID = "ami-0472e9821a913b9b9";
                string keyPairName = "Teste";
                List<string> groups = new List<string>() { "sg-0f4b34f09a4cdb524" };
                string subnetID = "subnet-f7cf54d6";

                var eni = new InstanceNetworkInterfaceSpecification()
                {
                    DeviceIndex = 0,
                    SubnetId = subnetID,
                    Groups = groups,
                    AssociatePublicIpAddress = true
                };
                List<InstanceNetworkInterfaceSpecification> enis = new List<InstanceNetworkInterfaceSpecification>() { eni };

                var launchRequest = new RunInstancesRequest()
                {
                    ImageId = amiID,
                    InstanceType = "t2.micro",
                    MinCount = 1,
                    MaxCount = 1,
                    KeyName = keyPairName,
                    NetworkInterfaces = enis
                };

                awsCon.RunInstancesAsync(launchRequest);

                ViewBag.errorMessage = "Instance initiated)";
                return View("Index", preencherForm(aws));

            }
            catch (Exception e)
            {
                ViewBag.errorMessage = "Instance failed ( Error: " + e.Message + " )";
                return View("Index", preencherForm(aws));
            }
        }

        public ViewResult Create(AzureModel azure)
        {
            try
            {
                string containerGroupName = SdkContext.RandomResourceName("blc-", 6);
                string multiContainerGroupName = containerGroupName + "-multi";
                string postgres = "andrepaixaao/postgres";
                string mongo = "andrepaixaao/mongo";
                string blockbase = "andrepaixaao/blockbase";
                IAzure azureCon = GetAzureContext(azure);
                if (azureCon.ResourceGroups.CheckExistence(azure.ResourceGroupName) == null)
                {
                    azureCon.ResourceGroups.Define(azure.ResourceGroupName).WithRegion(Microsoft.Azure.Management.ResourceManager.Fluent.Core.Region.USCentral).Create();
                }

                CreateContainerGroupMulti(azureCon, azure.ResourceGroupName, multiContainerGroupName, postgres, mongo, blockbase);
                ViewBag.errorMessage = "Instance initiated";
                return View("Index", preencherForm(azure));
            }
            catch (Exception e)
            {

                ViewBag.errorMessage = "Instance failed ( Error: " + e.Message + " )";
                return View("Index", preencherForm(azure));
            }

        }

        private static IAzure GetAzureContext(AzureModel azure)
        {
            IAzure azureCon;

            var credentials = new AzureCredentials(new ServicePrincipalLoginInformation { ClientId = azure.ClientId, ClientSecret = azure.ClientSecret }, azure.TenantId, AzureEnvironment.AzureGlobalCloud);
            azureCon = Microsoft.Azure.Management.Fluent.Azure.Authenticate(credentials).WithDefaultSubscription();

            return azureCon;
        }
        private static void CreateContainerGroupMulti(IAzure azure,
                                                      string resourceGroupName,
                                                      string containerGroupName,
                                                      string containerImage1,
                                                      string containerImage2, string containerImage3)
        {
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Microsoft.Azure.Management.ResourceManager.Fluent.Core.Region azureRegion = resGroup.Region;
            var postgresEnviromentVariables = new Dictionary<string, string>();
            postgresEnviromentVariables.Add("POSTGRES_PASSWORD", "yourpassword");
            postgresEnviromentVariables.Add("POSTGRES_USER", "postgres");
            var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName + "-1")
                    .WithImage(containerImage1)
                    .WithExternalTcpPort(80)
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)
                    .WithEnvironmentVariables(postgresEnviromentVariables)
                    .Attach()
                .DefineContainerInstance(containerGroupName + "-2")
                    .WithImage(containerImage2)
                    .WithoutPorts()
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)
                    .Attach()
                   .DefineContainerInstance(containerGroupName + "-3")
                    .WithImage(containerImage3)
                    .WithoutPorts()
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)

                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();
        }

        public async Task<ViewResult> Create( GoogleCloudModel google)
        {
            try
            {


                ComputeService computeService = new ComputeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = await GetCredential(google),
                    ApplicationName = "Blockbase",
                });

                string name = SdkContext.RandomResourceName("blockbase-", 2);

                string zone = "us-central1-a";

                var requestBody = Newtonsoft.Json.JsonConvert.DeserializeObject<Google.Apis.Compute.v1.Data.Instance>("{  \"kind\": \"compute#instance\" ,  \"zone\": \"projects/blockbase/zones/us-central1-a\",  \"machineType\": \"projects/blockbase/zones/us-central1-a/machineTypes/e2-medium\",  \"displayDevice\": {    \"enableDisplay\": false  },  \"metadata\": {    \"kind\": \"compute#metadata\",    \"items\": []  },  \"tags\": {    \"items\": [      \"http-server\",      \"https-server\"    ]  },  \"disks\": [    {      \"kind\": \"compute#attachedDisk\",      \"type\": \"PERSISTENT\",      \"boot\": true,      \"mode\": \"READ_WRITE\",      \"autoDelete\": true,      \"deviceName\": \"instance-1\",      \"initializeParams\": {        \"sourceImage\": \"projects/blockbase/global/images/blockbase\",        \"diskType\": \"projects/blockbase/zones/us-central1-a/diskTypes/pd-balanced\",        \"diskSizeGb\": \"20\",        \"labels\": {}      },      \"diskEncryptionKey\": {}    }  ],  \"canIpForward\": false,  \"networkInterfaces\": [    {      \"kind\": \"compute#networkInterface\",      \"subnetwork\": \"projects/blockbase/regions/us-central1/subnetworks/default\",      \"accessConfigs\": [        {          \"kind\": \"compute#accessConfig\",          \"name\": \"External NAT\",          \"type\": \"ONE_TO_ONE_NAT\",          \"networkTier\": \"PREMIUM\"        }      ],      \"aliasIpRanges\": []    }  ],  \"description\": \"\",  \"labels\": {},  \"scheduling\": {    \"preemptible\": false,    \"onHostMaintenance\": \"MIGRATE\",    \"automaticRestart\": true,    \"nodeAffinities\": []  },  \"deletionProtection\": false,  \"reservationAffinity\": {    \"consumeReservationType\": \"ANY_RESERVATION\"  },  \"serviceAccounts\": [    {      \"email\": \"867652000955-compute@developer.gserviceaccount.com\",      \"scopes\": [        \"https://www.googleapis.com/auth/cloud-platform\"      ]    }  ],\"shieldedInstanceConfig\": {    \"enableSecureBoot\": false,    \"enableVtpm\": true,    \"enableIntegrityMonitoring\": true  },  \"confidentialInstanceConfig\": {    \"enableConfidentialCompute\": false  }}");
                requestBody.Name = name;

                InstancesResource.InsertRequest request = computeService.Instances.Insert(requestBody, google.ProjectId, zone);

                Google.Apis.Compute.v1.Data.Operation response = request.Execute();

                ViewBag.resultado = "Instance initiated";

                return View("Index", preencherForm(google));

            }
            catch (Exception e)
            {
                ViewBag.errorMessage = "Instance failed ( Error: " + e.Message + " )";
                return View("Index", preencherForm(google));
            }


        }
        public static async Task<GoogleCredential> GetCredential(GoogleCloudModel obj)
        {
            obj.PrivateKey = Regex.Replace(obj.PrivateKey, @"\\n", "");


            string credentialsJson = JsonConvert.SerializeObject(obj);

            {
                GoogleCredential credential = await Task.Run(() => GoogleCredential.FromJson(credentialsJson));
                if (credential.IsCreateScopedRequired)
                {
                    credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }
                return credential;
            }
        }



        public void CreateTeste(IFormCollection data)
        {
            var a = Teste(data);
            Create(a);

          
        }

        public Object Teste(IFormCollection data)
        {
            Type a = null;
            
            string className = (string)data["ClassName"];
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a=> a.FullName.StartsWith("Blockbase"));
            foreach(var assembly in assemblies)
            {
                var type = assembly.DefinedTypes.FirstOrDefault(t=> t.Name==className);
                if(type!=null)
                {
                    a = type;
                }

            }
            var obj = Activator.CreateInstance(a);
           
            foreach(var propform in data)
            {
                foreach (var propmodel in obj.GetType().GetProperties())
                {
                    if (propform.Key == propmodel.Name)
                    {
                        propmodel.SetValue(obj, propform.Value);
                    }
                }
            }
            
            return  obj;

        }




    }
}
