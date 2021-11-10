using OCR_APP1.Shared;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OCR_APP1.Client.Services
{

    public class ComputerVisionService
    {
        static string subscriptionKey;
        static string endpoint;
        static string uriBase;

        public ComputerVisionService()
        {
            subscriptionKey = "93bce0de2bd64f4d8fd3e9a29acb0501";
            endpoint = "https://ocr-yam.cognitiveservices.azure.com/";
            uriBase = endpoint + "vision/v3.2/ocr";
        }
        /// <summary>
        /// Get the text that is in the manage byte
        /// This is, convert image byte[] to ocrResultDTO,
        /// which has the detected language and the detected text
        /// 
        /// </summary>
        /// <param name="imageFileBytes"></param>
        /// <returns></returns>
        public async Task<OcrResultDTO> GetTextFromImage(byte[] imageFileBytes)
        {
            //We use string builder because it is optimized for strings that keep changing (not immutable)
            StringBuilder sb = new StringBuilder();
            OcrResultDTO ocrResultDTO = new OcrResultDTO();
            try
            {
                string JSONResult = await ReadTextFromStream(imageFileBytes);

                OcrResult ocrResult = JsonConvert.DeserializeObject<OcrResult>(JSONResult);

                if (!ocrResult.Language.Equals("unk"))
                {
                    foreach (OcrLine ocrLine in ocrResult.Regions[0].Lines)
                    {
                        foreach (OcrWord ocrWord in ocrLine.Words)
                        {
                            sb.Append(ocrWord.Text);
                            sb.Append(' ');
                        }
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.Append("This language is not supported.");
                }
                ocrResultDTO.DetectedText = sb.ToString();
                ocrResultDTO.Language = ocrResult.Language;
                return ocrResultDTO;
            }
            catch
            {
                ocrResultDTO.DetectedText = "Error occurred. Try again";
                ocrResultDTO.Language = "unk";
                return ocrResultDTO;
            }
        }


        /// <summary>
        /// This method takes in an image byte.
        /// Converts it in a json format (application/octet-stream) so that it can be transmitted over the internet to AZURE's OCR API.
        /// The OCR API will recieves a post request from the app and respond with a string of the detect characters and the image text
        /// </summary>
        /// <param name="byteData">Image file as an in memery byte array</param>
        /// <returns> Result from the api</returns>
        static async Task<string> ReadTextFromStream(byte[] byteData)
        {
            try
            {
                //We create a client to communicate with the api
                HttpClient client = new HttpClient();

                //Add Subscription key to the clients header
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

                //Create and add some request parameters for the uri
                string requestParameters = "language=unk&detectOrientation=true";
                string uri = uriBase + "?" + requestParameters;

                //This will be the response message from the api
                HttpResponseMessage response;

                //The image file is currently an array of bytes that can be read by the .NET Runtume 
                // We need to convert them into a format the can be sent over the internet and can be reader be an API
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    //Specify the media type for the image bytes
                    //Convert the bytes into application/octet-stream (the json format is used for images and unknown media formats
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    //Send the http (POST Request)
                    //And get the respose
                    response = await client.PostAsync(uri, content);
                }
                //Read and convert response to C# string 
                string contentString = await response.Content.ReadAsStringAsync();
                //This will help detect line break and any other fromatting
                //JTokenParse is used for security staff but for now it's a hack
                string result = JToken.Parse(contentString).ToString();
                return result;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public async Task<AvailableLanguage> GetAvailableLanguages()
        {
            string endpoint = "https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=translation";
            var client = new HttpClient();
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(endpoint);
                var response = await client.SendAsync(request).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync();

                AvailableLanguage deserializedOutput = JsonConvert.DeserializeObject<AvailableLanguage>(result);

                return deserializedOutput;
            }
        }
    }
}

