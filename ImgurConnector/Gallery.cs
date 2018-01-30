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
        public static async Task<IEnumerable<GalleryAlbum>> GetAlbums(string clientId)
        {
            var client = new ImgurClient(clientId);

            var endpoint = new GalleryEndpoint(client);
            var albums = await endpoint.GetGalleryAsync(GallerySection.Hot);

            return albums.OfType<GalleryAlbum>();
        }
    }
}
