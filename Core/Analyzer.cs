using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        private void Log(string str)
        {
            using (var writer = new StreamWriter("log.txt",true))
            {
                writer.WriteLine(str);
            }
            Console.WriteLine(str);
        }

        public async void Run()
        {
            var puns = _dataRepo.GetPuns().ToList();
            var stopWords = _dataRepo.GetStopWords().ToList();
            var seenAlbums = _dataRepo.GetSeenAlbumIds().ToList();

            while (true)
            {
                Log("Fetching albums...");
                var albums = ImgurConnector.Gallery.GetAlbums(_imgurClientId, 500, 1);

                foreach (var album in albums.Select(w => w.Album).Where(i => i.ImagesCount <= 5))
                {
                    if (seenAlbums.Contains(album.Id))
                    {
                        Log("Already seen " + album.Id);
                        continue;
                    }

                    var imageUrl = album.Images.First().Link;

                    const int titleWeight = 5;
                    const int tagWeight = 10;
                    const int descriptionWeight = 1;
                    const int threshHold = 15;
                    const int commentWeight = 2;

                    var labels = new Dictionary<string, double>();

                    foreach (var word in GetWords(album.Title))
                        AddToDic(labels, word, titleWeight);

                    if (album.Description != null)
                    {
                        foreach (var word in GetWords(album.Description))
                            AddToDic(labels, word, descriptionWeight);
                    }

                    //TODO: use tags

                    if (album.CommentCount > 0)
                    {
                        var comments = ImgurConnector.Gallery.GetComments(_imgurClientId, album);

                        foreach (var comment in comments)
                        {
                            foreach (var word in GetWords(comment.CommentText))
                                AddToDic(labels, word, commentWeight);
                        }
                    }

                    var scoredPuns = puns.Select(p => new KeyValuePair<string, double>(p, ScorePun(p, labels, stopWords, false))).OrderByDescending(p => p.Value).ToList();

                    if (scoredPuns.Any(o => o.Value >= threshHold))
                    {
                        Log("");
                        Log($"title: {album.Title}");
                        Log($"description: {album.Description}");
                        Log($"url: {imageUrl}");

                        Log(scoredPuns.First().Key + ": " + scoredPuns.First().Value);
                        ScorePun(scoredPuns.First().Key, labels, stopWords, true);
                        Log("");
                    }
                    else
                        Log("No pun found: " + scoredPuns.First().Value);

                    seenAlbums.Add(album.Id);
                    _dataRepo.AddSeenAlbumId(album.Id);
                }

                Log("Out of albums to process...");
            }
        }

        private IEnumerable<string> GetWords(string s)
        {
            return s.Trim().ToLower().Replace(".", "").Replace(",", "").Replace("?", "").Replace("\"", "").Replace("-", "").Replace(":", "").Split(' ');
        }

        private double ScorePun(string pun, Dictionary<string,double> labels, IEnumerable<string> stopwords, bool trace)
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
                {
                    if (trace)
                        Log("Scored " + val + " points for " + word);

                    ret += val;
                }
            }

            return ret;
        }

        private void AddToDic(Dictionary<string,double> dic,string key, double value)
        {
            key = key.ToLower().Trim();

            if (string.IsNullOrEmpty(key))
                return;

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
    }
}
