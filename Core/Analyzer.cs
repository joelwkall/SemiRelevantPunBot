using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class Analyzer
    {
        private string _imgurClientId;
        private string _imgurClientSecret;
        private string _azureKey;
        private string _azureRegion;

        public Analyzer(string imgurClientId, string imgurClientSecret, string azureKey, string azureRegion)
        {
            _imgurClientId = imgurClientId;
            _imgurClientSecret = imgurClientSecret;
            _azureKey = azureKey;
            _azureRegion = azureRegion;
        }

        public async void Run()
        {
            var albums = await ImgurConnector.Gallery.GetAlbums(_imgurClientId);
            foreach (var album in albums.Where(i => i.ImagesCount == 1))
            {
                var imageUrl = album.Images.First().Link;

                MakeAnalysisRequest(imageUrl);
            }
        }

        /// <summary>
        /// Gets the analysis of the specified image url by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file.</param>
        private async void MakeAnalysisRequest(string imageUrl)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _azureKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "visualFeatures=Categories,Description,Color&language=en";

            // Assemble the URI for the REST API Call.
            string uri = _azureRegion + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageUrl);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n");

                var j = JObject.Parse(contentString);
            }
        }

        private byte[] GetImageAsByteArray(string imageUrl)
        {
            var request = WebRequest.Create(imageUrl);

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        const int bufferSize = 4096;
                        using (var ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[bufferSize];
                            int count;
                            while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                                ms.Write(buffer, 0, count);
                            return ms.ToArray();
                        }
                    }
                }
            }
        }
    }
}
