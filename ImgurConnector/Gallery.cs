using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Enums;
using Imgur.API.Models.Impl;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImgurConnector
{
    public static class Gallery
    {
        public static IEnumerable<GalleryAlbum> GetAlbums(string clientId)
        {
            var client = new ImgurClient(clientId);

            var endpoint = new GalleryEndpoint(client);

            for (int page = 0; page < 50; page++)
            {
                var albumsTask = endpoint.GetGalleryAsync(GallerySection.User, GallerySortOrder.Rising, TimeWindow.Day, page, true);
                albumsTask.Wait();

                foreach (var album in albumsTask.Result.OfType<GalleryAlbum>())
                {
                    yield return album;
                }
            }
        }
    }
}
