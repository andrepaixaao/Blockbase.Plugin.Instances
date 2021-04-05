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
using Blockbase.Plugin.Instances.Properties;

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
            var azureForm = FormViewModel.From(test);
            return View(azureForm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<ViewResult> Create(IFormCollection form)
        {
            var className = form["ClassName"];
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Blockbase"));
            Type formType = null;
            foreach (var assembly in assemblies)
            {
                var type = assembly.DefinedTypes.FirstOrDefault(t => t.Name == className);
                if (type == null) continue;
                formType = type;
                break;
            }

            if (formType == null)
            {
                ViewBag.ErrorMessage = "Class not found.";
                return View("Index");

            }
            var obj = Activator.CreateInstance(formType);

            foreach (var (formField, fieldValue) in form)
            {
                foreach (var modelProperty in obj.GetType().GetProperties())
                {
                    if (formField == modelProperty.Name)
                    {
                        modelProperty.SetValue(obj, fieldValue.ToString());
                    }
                }
            }

            var method = this.GetType().GetMethods()
                .FirstOrDefault(x => x.Name == "CreateWithModel" && x.GetParameters().Any(p => p.ParameterType == obj.GetType()));

            if (method == null)
            {
                ViewBag.ErrorMessage = " Method not found.";
                return View("Index");

            }


            return await (Task<ViewResult>) method.Invoke(this, new[] { obj });
        }


        public async Task<ViewResult> CreateWithModel(AmazonWebServiceModel model)
        {
            var instanceStartResult = StartAmazonElasticContainer2Instance(model);
            if (!instanceStartResult.IsSuccessful)
            {
                ViewBag.ErrorMessage = instanceStartResult.ErrorMessage;
            }
            return View("Index", FormViewModel.From(model));
        }

        public async Task<ViewResult> CreateWithModel(AzureModel model)
        {
            var instanceStartResult = StartAzureInstance(model);
            if (!instanceStartResult.IsSuccessful)
            {
                ViewBag.ErrorMessage = instanceStartResult.ErrorMessage;
            }
            return View("Index", FormViewModel.From(model));
        }

        public async Task<ViewResult> CreateWithModel(GoogleCloudModel model)
        {
            var instanceStartResult = await StartGoogleCloudInstance(model);
            if (!instanceStartResult.IsSuccessful)
            {
                ViewBag.ErrorMessage = instanceStartResult.ErrorMessage;
            }
            return View("Index", FormViewModel.From(model));
        }

        #region Azure

        private OperationResult StartAzureInstance(AzureModel model)
        {
            try
            {
                string containerGroupName = SdkContext.RandomResourceName("blc-", 6);
                string multiContainerGroupName = containerGroupName + "-multi";
                string postgresImagePath = Resources.POSTGRES_IMAGE_PATH;
                string mongoImagePath = Resources.MONGO_IMAGE_PATH;
                string blockbaseImagePath = Resources.BLOCKBASE_IMAGE_PATH;
                IAzure azureCon = GetAzureContext(model);
                if (!azureCon.ResourceGroups.CheckExistence(model.ResourceGroupName))
                {
                    azureCon.ResourceGroups.Define(model.ResourceGroupName).WithRegion(Microsoft.Azure.Management.ResourceManager.Fluent.Core.Region.USCentral).Create();
                }

                CreateContainerGroupMulti(azureCon, model.ResourceGroupName, multiContainerGroupName, postgresImagePath, mongoImagePath, blockbaseImagePath);
                ViewBag.errorMessage = "Instance initiated";
                return new OperationResult() { IsSuccessful = true };
            }
            catch (Exception e)
            {
                return new OperationResult()
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Instance failed ( Error: {e.Message} )"
                };
            }
        }

        private static IAzure GetAzureContext(AzureModel azure)
        {
            IAzure azureContext;

            var credentials = new AzureCredentials(new ServicePrincipalLoginInformation { ClientId = azure.ClientId, ClientSecret = azure.ClientSecret }, azure.TenantId, AzureEnvironment.AzureGlobalCloud);
            azureContext = Azure.Authenticate(credentials).WithDefaultSubscription();
            return azureContext;
        }

        private static void CreateContainerGroupMulti(IAzure azure,
                                                      string resourceGroupName,
                                                      string containerGroupName,
                                                      string containerImage1,
                                                      string containerImage2, string containerImage3)
        {
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            var azureRegion = resGroup.Region;
            var postgresEnvironmentVariables = new Dictionary<string, string>();
            postgresEnvironmentVariables.Add("POSTGRES_PASSWORD", Resources.POSTGRES_PASSWORD);
            postgresEnvironmentVariables.Add("POSTGRES_USER", Resources.POSTGRES_USER);
            azure.ContainerGroups.Define(containerGroupName)
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
                    .WithEnvironmentVariables(postgresEnvironmentVariables)
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

        #endregion

        #region Amazon EC2
        private OperationResult StartAmazonElasticContainer2Instance(AmazonWebServiceModel model)
        {
            try
            {
                BasicAWSCredentials credentials = new BasicAWSCredentials(model.AcessKey, model.SecretKey);
                var awsConnection = new AmazonEC2Client(credentials, RegionEndpoint.USEast1);
                var amazonImageId = "ami-0472e9821a913b9b9";
                var keyPairName = "Teste";
                var groups = new List<string>() { "sg-0f4b34f09a4cdb524" };
                var subnetId = "subnet-f7cf54d6";

                var elasticNetworkInterfaces = new List<InstanceNetworkInterfaceSpecification>()
                {
                    new InstanceNetworkInterfaceSpecification()
                    {
                        DeviceIndex = 0,
                        SubnetId = subnetId,
                        Groups = groups,
                        AssociatePublicIpAddress = true
                    }
                };


                var launchRequest = new RunInstancesRequest()
                {
                    ImageId = amazonImageId,
                    InstanceType = "t2.micro",
                    MinCount = 1,
                    MaxCount = 1,
                    KeyName = keyPairName,
                    NetworkInterfaces = elasticNetworkInterfaces
                };

                awsConnection.RunInstancesAsync(launchRequest);
                ViewBag.errorMessage = "Instance initiated";
                return new OperationResult() { IsSuccessful = true };
            }
            catch (Exception e)
            {
                return new OperationResult()
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Instance failed ( Error: {e.Message} )"
                };
            }

        }

        #endregion

        #region Google Platform
        private async Task<OperationResult> StartGoogleCloudInstance(GoogleCloudModel model)
        {
            try
            {
                var computeService = new ComputeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = await GetCredential(model),
                    ApplicationName = "Blockbase",
                });

                var name = SdkContext.RandomResourceName("blockbase-", 2);

                var zone = "us-central1-a";

                var requestBody = JsonConvert.DeserializeObject<Google.Apis.Compute.v1.Data.Instance>(Resources.GOOGLE_REQUEST_BODY);
                requestBody.Name = name;

                var request = computeService.Instances.Insert(requestBody, model.ProjectId, zone);

                var response = request.Execute();
                return new OperationResult() { IsSuccessful = true };
            }
            catch (Exception e)
            {
                return new OperationResult()
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Instance failed ( Error: {e.Message} )"
                };
            }
        }

        public static async Task<GoogleCredential> GetCredential(GoogleCloudModel model)
        {
            model.PrivateKey = Regex.Replace(model.PrivateKey, @"\\n", "");


            var credentialsJson = JsonConvert.SerializeObject(model);

            {
                var credential = await Task.Run(() => GoogleCredential.FromJson(credentialsJson));
                if (credential.IsCreateScopedRequired)
                {
                    credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }
                return credential;
            }
        }
        #endregion

    }
}