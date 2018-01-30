using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    public class Analyzer
    {
        private string _imgurClientId;
        private string _imgurClientSecret;
        private string _azureKey;
        private string _azureRegion;
        private DataRepository _dataRepo;

        public Analyzer(string imgurClientId, string imgurClientSecret, string azureKey, string azureRegion, DataRepository dataRepository)
        {
            _imgurClientId = imgurClientId;
            _imgurClientSecret = imgurClientSecret;
            _azureKey = azureKey;
            _azureRegion = azureRegion;
            _dataRepo = dataRepository;
        }

        public async void Run()
        {
            var puns = _dataRepo.GetPuns().ToList();
            var stopWords = _dataRepo.GetStopWords().ToList();

            var albums = ImgurConnector.Gallery.GetAlbums(_imgurClientId);

            foreach (var album in albums.Where(i => i.ImagesCount == 1))
            {
                var imageUrl = album.Images.First().Link;

                dynamic j = await MakeAnalysisRequest(imageUrl);

                if (j.statusCode != null && j.statusCode != 200)
                {
                    Console.WriteLine("Rate limited.Waiting");
                    Thread.Sleep(5000);
                    continue;
                }

                const int titleWeight = 5;
                const int tagWeight = 10;
                const int descriptionWeight = 1;
                const int captionWeight = 15;
                const int threshHold = 31;

                var labels = new Dictionary<string, double>();

                foreach (var word in GetWords(album.Title))
                    AddToDic(labels, word, titleWeight);

                if (album.Description != null)
                {
                    foreach (var word in GetWords(album.Description))
                        AddToDic(labels, word, descriptionWeight);
                }

                if (j.description != null)
                {
                    foreach (var tag in j.description.tags)
                        AddToDic(labels, tag.ToObject<string>(), tagWeight);

                    foreach (var cap in j.description.captions)
                    {
                        foreach (var word in cap.text.ToObject<string>().Split(' '))
                            AddToDic(labels, word, captionWeight);
                    }
                }

                var scoredPuns = puns.Select(p=>new KeyValuePair<string,double>(p,ScorePun(p, labels, stopWords))).OrderByDescending(p => p.Value).ToList();

                if (scoredPuns.Any(o => o.Value >= threshHold))
                {
                    Console.WriteLine();
                    Console.WriteLine($"title: {album.Title}");
                    Console.WriteLine($"description: {album.Description}");
                    Console.WriteLine($"url: {imageUrl}");

                    if (j.description != null)
                    {
                        foreach (var tag in j.description.tags)
                            Console.WriteLine(tag);

                        foreach (var c in j.description.captions)
                            Console.WriteLine(c.text);
                    }

                    Console.WriteLine(scoredPuns.First().Key + ": " + scoredPuns.First().Value);
                    Console.WriteLine();
                }
                else
                    Console.WriteLine("No pun found: " + scoredPuns.First().Value);
            }
        }

        private IEnumerable<string> GetWords(string s)
        {
            return s.Trim().ToLower().Replace(".", "").Replace(",", "").Replace("?", "").Replace("\"", "").Replace("-", "").Split(' ');
        }

        private double ScorePun(string pun, Dictionary<string,double> labels, IEnumerable<string> stopwords)
        {
            var words = GetWords(pun).Distinct(); //only count words once

            var ret = 0.0;

            var stopEndings = new[]
            {
                "ing"
            };

            foreach(var word in words)
            {
                if (stopwords.Contains(word))
                    continue;

                if (stopEndings.Any(e=>word.EndsWith(e)))
                    continue;

                if (labels.TryGetValue(word, out var val))
                    ret += val;
            }

            return ret;
        }

        private void AddToDic(Dictionary<string,double> dic,string key, double value)
        {
            if(dic.TryGetValue(key,out var val))
            {
                val += value;
                dic[key] = val;
            }
            else
            {
                dic.Add(key, value);
            }
        }

        /// <summary>
        /// Gets the analysis of the specified image url by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file.</param>
        private async Task<JObject> MakeAnalysisRequest(string imageUrl)
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

                return JObject.Parse(contentString);
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
