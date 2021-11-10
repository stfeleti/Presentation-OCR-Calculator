using BlazorInputFile;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using OCR_APP1.Client.Services;
using OCR_APP1.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OCR_APP1.Client.Pages
{
    public partial class OCRPage : ComponentBase
    {
        [Inject]
        protected ComputerVisionService computerVisionService { get; set; }


        protected string DetectedTextLanguage;
        protected string imagePreview;
        protected bool loading = false;
        byte[] imageFileBytes;

        //The Computer Vision Has a file limit of 6MB but 4 MB is safer
        const string DefaultStatus = "Maximum size allowed for the image is 4 MB";
        protected string status = DefaultStatus;

        protected OcrResultDTO Result = new OcrResultDTO();

        private AvailableLanguage availableLanguages;
        private Dictionary<string, LanguageDetails> LanguageList = new Dictionary<string, LanguageDetails>();
        const int MaxFileSize = 4 * 1024 * 1024; // 4MB  


        //This starts when the component is ready to start
        protected override async Task OnInitializedAsync()
        {
            //This is retrieves all the available langauges
            availableLanguages = await computerVisionService.GetAvailableLanguages();
            LanguageList = availableLanguages.Translation;
        }
        /// <summary>
        /// The Microsoft InputFile component is very unstable.
        /// The BlazorFileInput is simpler and works well
        /// </summary>
        /// <param name="files"> this is an interface from the BlazorFileInput package</param>
        /// <returns></returns>
        protected async Task ViewImage(IFileListEntry[] files)
        {
            //Get the first file or deafault (empty)
            var file = files.FirstOrDefault();
            if (file == null)
            {
                return;
            }
            else if (file.Size > MaxFileSize)
            {
                status = $"The file size is {file.Size} bytes, this is more than the allowed limit of {MaxFileSize} bytes.";
                return;
            }
            else if (!file.Type.Contains("image"))
            {
                status = "Please uplaod a valid image file";
                return;
            }
            else
            {
                //We don't have a file system in blazor WASM to store the file
                //but we do have RAM
                //This will allow us to store the file in-memory
                var memoryStream = new MemoryStream();
                //Copy the file into memory
                await file.Data.CopyToAsync(memoryStream);
                //Convert the memery stream into byte array
                imageFileBytes = memoryStream.ToArray();
                string base64String = Convert.ToBase64String(imageFileBytes, 0, imageFileBytes.Length);


                //File name
                imagePreview = string.Concat("data:image/png;base64,", base64String);
                //Clean the memory stream
                memoryStream.Flush();
                //Any error of messages
                status = DefaultStatus;
            }
        }
        /// <summary>
        /// This our point of entry (kind of)
        /// Onclick for extract text button
        /// show loading screen 
        /// then display languages, errors and the detected text
        /// </summary>
        /// <returns></returns>
        protected private async Task GetText()
        {
            if (imageFileBytes != null)
            {
                loading = true;
                Result = await computerVisionService.GetTextFromImage(imageFileBytes);
                if (LanguageList.ContainsKey(Result.Language))
                {
                    DetectedTextLanguage = LanguageList[Result.Language].Name;
                    
                }
                else
                {
                    DetectedTextLanguage = "Unknown";
                }
                loading = false;
            }
        }
    }
}
