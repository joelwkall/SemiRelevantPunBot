using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Enums;
using Imgur.API.Models.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImgurConnector
{
    public static class Gallery
    {
        public static IEnumerable<Comment> GetComments(string clientId, GalleryAlbum album)
        {
            var client = new ImgurClient(clientId);

            var endpoint = new GalleryEndpoint(client);

            //TODO catch errors
            var albumsTask = endpoint.GetGalleryItemCommentsAsync(album.Id, CommentSortOrder.Best);
            albumsTask.Wait();

            return albumsTask.Result.OfType<Comment>();
        }

        public static IEnumerable<GalleryAlbumWrapper> GetAlbums(string clientId, int limit, int tries)
        {
            var client = new ImgurClient(clientId);

            var endpoint = new GalleryEndpoint(client);

            var ret = new List<GalleryAlbumWrapper>(limit);

            int errors = 0;

            for (int page = 0; page < 50; page++)
            {
                try
                {
                    var albumsTask = endpoint.GetGalleryAsync(GallerySection.User, GallerySortOrder.Rising, TimeWindow.Day, page, true);
                    albumsTask.Wait();

                    foreach (var album in albumsTask.Result.OfType<GalleryAlbum>())
                    {
                        if (ret.Count == limit)
                            break;

                        ret.Add(new GalleryAlbumWrapper(album));
                    }

                    //reset the error counter
                    errors = 0;
                }
                catch (AggregateException ex) // when(ex.InnerException.Message== "Imgur is temporarily over capacity.Please try again later.")
                {
                    //after 3 errors just bail and return what we got
                    errors++;
                    if (errors >= tries)
                        return ret;

                    //otherwise try the same page again
                    page--;
                    Thread.Sleep(1000);
                }

                if (ret.Count == limit)
                    return ret;
            }

            return ret;
        }

        public class GalleryAlbumWrapper
        {
            public GalleryAlbum Album;

            public GalleryAlbumWrapper(GalleryAlbum album)
            {
                this.Album = album;
            }

            public override string ToString()
            {
                return Album.Title + " by " + Album.AccountId;
            }
        }
    }
}
