using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.IO;

namespace FigmaReader
{
    internal class OpenAIService
    {
        private const string Endpoint = "https://httpqas26-frontend-qas-sdf-mw1p.qas.binginternal.com/completions";
        private static int ComponentCount = 0;
        public const string ApiKey = "";
        static IEnumerable<string> Scopes = new List<string>() {
         "api://68df66a4-cad9-4bfd-872b-c6ddde00d6b2/access"
        };


        private static async Task<MsalCacheHelper> CreateCacheHelperAsync()
        {
            StorageCreationProperties storageProperties;

            try
            {
                storageProperties = new StorageCreationPropertiesBuilder(
                    ".llmapi-token-cache.txt",
                    ".")
                .WithLinuxKeyring(
                    "com.microsoft.substrate.llmapi",
                    MsalCacheHelper.LinuxKeyRingDefaultCollection,
                    "MSAL token cache for LLM API",
                    new KeyValuePair<string, string>("Version", "1"),
                    new KeyValuePair<string, string>("ProductGroup", "LLMAPI"))
                .WithMacKeyChain(
                    "llmapi_msal_service",
                    "llmapi_msla_account")
                .Build();

                var cacheHelper = await MsalCacheHelper.CreateAsync(
                    storageProperties).ConfigureAwait(false);

                cacheHelper.VerifyPersistence();
                return cacheHelper;

            }
            catch (MsalCachePersistenceException e)
            {
                Console.WriteLine($"WARNING! Unable to encrypt tokens at rest." +
                    $" Saving tokens in plaintext at {System.IO.Path.Combine(".", ".llmapi-token-cache.txt")} ! Please protect this directory or delete the file after use");
                Console.WriteLine($"Encryption exception: " + e);

                storageProperties =
                    new StorageCreationPropertiesBuilder(
                        ".llmapi-token-cache.txt" + ".plaintext", // do not use the same file name so as not to overwrite the encypted version
                        ".")
                    .WithUnprotectedFile()
                    .Build();

                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
                cacheHelper.VerifyPersistence();

                return cacheHelper;
            }
        }

        static IPublicClientApplication app = PublicClientApplicationBuilder.Create("68df66a4-cad9-4bfd-872b-c6ddde00d6b2")
            .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
            .Build();

        static async Task<string> GetToken()
        {

            var accounts = await app.GetAccountsAsync();
            AuthenticationResult result = null;
            if (accounts.Any())
            {
                var chosen = accounts.First();

                try
                {
                    result = await app.AcquireTokenSilent(Scopes, chosen).ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    // cannot get a token silently, so redirect the user to be challenged 
                }
            }
            if (result == null)
            {
                var tokenURL = string.Empty;
                result = await app.AcquireTokenWithDeviceCode(Scopes,
                    deviceCodeResult => {
                        // This will print the message on the console which tells the user where to go sign-in using
                        // a separate browser and the code to enter once they sign in.
                        // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                        // device code callback to look for the successful login of the user via that browser.
                        // This background polling (whose interval and timeout data is also provided as fields in the
                        // deviceCodeCallback class) will occur until:
                        // * The user has successfully logged in via browser and entered the proper code
                        // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                        // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                        //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                        Console.WriteLine(deviceCodeResult.Message);
                        tokenURL = deviceCodeResult.Message;
                        return Task.FromResult(0);
                    }).ExecuteAsync();

            }
            return (result.AccessToken);
        }

        static async Task<string> SendRequest(string modelType, string requestData)
        {
            var token = await GetToken();
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Content = new StringContent(requestData, Encoding.UTF8, "application/json");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-ModelType", modelType);

            var httpResponse = await httpClient.SendAsync(request);

            return (await httpResponse.Content.ReadAsStringAsync());
        }

        public static async Task<string> CallOpenAPI(string request)
        {

            //////----Call Internal Open AI----///////
            //string modelType = "dev-text-davinci-003";// "dev -gpt-35-turbo";// "dev -chat-completion-gpt-35-turbo"; // "dev -moonshot";//"dev-text-davinci-003
            //var cacheHelper = await CreateCacheHelperAsync().ConfigureAwait(false);
            //cacheHelper.RegisterCache(app.UserTokenCache);
            //ComponentCount++;
            //string requestData = JsonConvert.SerializeObject(new ModelPrompt
            //{
            //    Prompt = "Generate React code which has all Microsoft accessibility standard rules applied having component name 'Component" + ComponentCount + "' for given Figma API JSON data: " + request,
            //    MaxTokens = 500,
            //    Temperature = 0.2,
            //    TopP = 1,
            //    N = 1,
            //    Stream = false,
            //    //LogProbs = null,
            //    Stop = null
            //});
            //// Available models are listed here: https://msasg.visualstudio.com/QAS/_wiki/wikis/QAS.wiki/134728/Getting-Started-with-Substrate-LLM-API?anchor=available-models
            //var response = await SendRequest(modelType, requestData);
            //Console.WriteLine(response);


            //return response;


            //////----Call Public Open AI----///////

            string endpoint = "https://api.openai.com/v1/completions";
            string modelType = "text-davinci-003"; //"davinci:ft-personal-2023-05-25-03-37-57"; // 

            // Set the headers and content
            var client = new HttpClient();

            // Set the required headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            //client.DefaultRequestHeaders.Add("X-ModelType", modelType);
            //client.DefaultRequestHeaders.Add("x-policy-id", "121");

            ComponentCount++;
            string requestBody = JsonConvert.SerializeObject(new ModelPrompt
            {
                Prompt = $@"Generate React code which has all Microsoft accessibility standard rules applied for given Figma API JSON data:  
                 Instructions:
                    1. Use component name as 'FigmaComponent {ComponentCount}'.
                    2. Ensure all form inputs have appropriate labels for screen reader accessibility. For example, 'htmlFor' attribute on the <label> element should corresponds to 'id' of the corresponding form control element.
                    3. Do not include <label> element while generating button element.
                    4. If any element ID is generated, then append a numerical value {ComponentCount} to it.
                    5. Do not use Material UI library.
                    6. Use inline css styling.
                    7. Ensure there is some margin or spaces between two elements.
                    8. Check the foreground color against the background color using a color contrast ratio checker. If the contrast ratio is below the recommended threshold, adjust the color values to meet the minimum contrast ratio as per WCAG guidelines.
                    8. Provide some meaningful options for dropdown or ComboBox as required.
                    10. Apply appropriate styling and attributes according to the Microsoft accessibility standard guidelines
                    11. Ensure proper indentation, line breaks, and formatting in the generated React code.
                    12. Ensure that the focus order of interactive elements follows a logical and meaningful sequence.
                    13. Apply visible focus styles to form controls (such as input fields, buttons, and checkboxes) to indicate when they receive focus, making it easier for keyboard users to navigate and interact with them.
                    14. When using ARIA attributes, ensure that you only use the attributes allowed by the WAI-ARIA specification. Refer to the WAI-ARIA specification documentation for a list of allowed ARIA attributes and their proper usage.
                Figma API JSON data: {request}",


                //Prompt = $@"Generate React code which has all Microsoft accessibility standard rules applied for given Figma API JSON data:  
                //Instructions:
                //    1. Use component name as 'FigmaComponent {ComponentCount}'.
                //    2. If any element ID is generated, then append a numerical value {ComponentCount} to it.
                //Figma API JSON data: {request}",

                Model = modelType,
                MaxTokens = 1000,
                Temperature = 0,
                TopP = 1,
                N = 1,
                Stream = false,
                //LogProbs = null,
                Stop = null
            });

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            // Send the POST request
           
            response = await client.PostAsync(endpoint, content);

            // Get the response data
            string responseData = await response.Content.ReadAsStringAsync();

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                // Successful API call
                Console.WriteLine("API call succeeded!");
                Console.WriteLine(responseData);
            }
            else
            {
                // Error occurred
                Console.WriteLine("API call failed!");
                Console.WriteLine(responseData);
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Message: {responseData}");
            }
            return responseData;
        }

        public static async Task<UploadFileResponse> UploadFile()
        {
            string endpoint = "https://api.openai.com/v1/files";
            string modelType = "text-davinci-003";

            // Read the contents of the training data file
            string filepath = Environment.CurrentDirectory + @"\TrainingData\basicFigmaCodeTrainingData.jsonl";
            string trainingData = File.ReadAllText(filepath);
           
            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                // Set the request headers
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                // Create the multipart form data content
                MultipartFormDataContent formData = new MultipartFormDataContent();

                // Add the training data file
                byte[] fileBytes = File.ReadAllBytes(filepath);
                ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                formData.Add(fileContent, "file", "figmaCodeTrainingData.jsonl");
                // Send the POST request

                // Add the purpose string
                string purpose = "fine-tune";
                StringContent purposeContent = new StringContent(purpose);
                formData.Add(purposeContent, "purpose");

                HttpResponseMessage response = await client.PostAsync(endpoint, formData);
                    string responseData = await response.Content.ReadAsStringAsync();
                // Check the response status
                UploadFileResponse responseModel = null;
                    if (response.IsSuccessStatusCode)
                    {
                        responseModel = JsonConvert.DeserializeObject<UploadFileResponse>(responseData);
                       
                        Console.WriteLine("Training data uploaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to upload training data. Status code: {response.StatusCode}");
                    }
                    return responseModel;
            }
        }

        public static async Task<string> DeleteFile()
        {
            string uploadedFileId = "ft-DkqvS3XcVouDC3aDMGJe4TNi";
            string endpoint = $"https://api.openai.com/v1/files/{uploadedFileId}";
            

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            HttpResponseMessage response;
            // Send the POST request

            response = await client.DeleteAsync(endpoint);

            // Get the response data
            string responseData = await response.Content.ReadAsStringAsync();

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                // Successful API call
                Console.WriteLine("API call succeeded!");
                Console.WriteLine(responseData);
            }
            else
            {
                // Error occurred
                Console.WriteLine("API call failed!");
                Console.WriteLine(responseData);
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Message: {responseData}");
            }

            return responseData;
        }

        public static async Task<string> CreateFineTuneModel(string uploadedFileId)
        {

            string endpoint = "https://api.openai.com/v1/fine-tunes";
            string modelType = "text-davinci-003";
            //string uploadedFileId = "file-TiRLZIkpHVjG0qXHnmTdKGjx";
            // Set the headers and content
            var client = new HttpClient();

            // Set the required headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            //client.DefaultRequestHeaders.Add("X-ModelType", modelType);
            //client.DefaultRequestHeaders.Add("x-policy-id", "121");

            string requestBody = JsonConvert.SerializeObject(new
            {
                training_file = uploadedFileId,
                model= "davinci" // default to curie.
            });

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            // Send the POST request

            response = await client.PostAsync(endpoint, content);

            // Get the response data
            string responseData = await response.Content.ReadAsStringAsync();

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                // Successful API call
                Console.WriteLine("API call succeeded!");
                Console.WriteLine(responseData);
            }
            else
            {
                // Error occurred
                Console.WriteLine("API call failed!");
                Console.WriteLine(responseData);
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Message: {responseData}");
            }
            return responseData;
        }

        public static async Task<string> GetFineTuneModelById(string fineTuneModelId)
        {

            // Set your OpenAI API credentials
            
            string fineTuneId = "file-TiRLZIkpHVjG0qXHnmTdKGjx";
            string endpoint = $"https://api.openai.com/v1/fine-tunes/{fineTuneModelId}";
            string modelType = "text-davinci-003";
           
            // Set the headers and content
            var client = new HttpClient();

            // Set the required headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

            HttpResponseMessage response;
            // Send the POST request

            response = await client.GetAsync(endpoint);

            // Get the response data
            string responseData = await response.Content.ReadAsStringAsync();

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                // Successful API call
                Console.WriteLine("API call succeeded!");
                Console.WriteLine(responseData);
            }
            else
            {
                // Error occurred
                Console.WriteLine("API call failed!");
                Console.WriteLine(responseData);
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Message: {responseData}");
            }
            return responseData;
        }

        public static async Task<string> DeleteFineTuneModel()
        {
            string uploadedFineTuneModel = "davinci:ft-personal-2023-05-25-03-37-57";
            string endpoint = $"https://api.openai.com/v1/models/{uploadedFineTuneModel}";
            

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
            HttpResponseMessage response;
            // Send the POST request

            response = await client.DeleteAsync(endpoint);

            // Get the response data
            string responseData = await response.Content.ReadAsStringAsync();

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                // Successful API call
                Console.WriteLine("API call succeeded!");
                Console.WriteLine(responseData);
            }
            else
            {
                // Error occurred
                Console.WriteLine("API call failed!");
                Console.WriteLine(responseData);
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Message: {responseData}");
            }

            return responseData;
        }
    }
}
