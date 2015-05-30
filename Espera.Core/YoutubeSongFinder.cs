using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Espera.Core
{
    public sealed class YoutubeSongFinder : IYoutubeSongFinder
    {
        /// <summary>
        /// The time a search with a given search term is cached.
        /// </summary>
        public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

        private const string ApiKey =
            "AIzaSyAHozS5XUfeWdEJNmtdgfxq_9gi1kd7G7A";

        private const int RequestLimit = 50;

        private readonly IBlobCache requestCache;

        /// <summary>
        /// Creates a new instance of the <see cref="YoutubeSongFinder" /> class.
        /// </summary>
        /// <param name="requestCache">
        /// A <see cref="IBlobCache" /> to cache the search requests. Requests with the same search
        /// term are considered the same.
        /// </param>
        public YoutubeSongFinder(IBlobCache requestCache)
        {
            if (requestCache == null)
                throw new ArgumentNullException("requestCache");

            this.requestCache = requestCache;
        }

        public IObservable<IReadOnlyList<YoutubeSong>> GetSongsAsync(string searchTerm = null)
        {
            searchTerm = searchTerm ?? string.Empty;

            return Observable.Defer(() => requestCache.GetOrFetchObject(BlobCacheKeys.GetKeyForYoutubeCache(searchTerm),
                () => RealSearch(searchTerm), DateTimeOffset.Now + CacheDuration));
        }

        public async Task<YoutubeSong> ResolveYoutubeSongFromUrl(Uri url)
        {
            return (await GetSongsAsync(url.OriginalString)).FirstOrDefault();
        }

        private static IObservable<IReadOnlyList<YoutubeSong>> RealSearch(string searchTerm)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = ApiKey,
                ApplicationName = "API Project"
            });

            return Observable.FromAsync(async () =>
            {
                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.Q = searchTerm;
                searchListRequest.Type = "video";
                searchListRequest.MaxResults = 50;

                var searchListResponse = await searchListRequest.ExecuteAsync();

                var listVideosRequest = youtubeService.Videos.List("snippet, contentDetails, statistics");
                listVideosRequest.Id = string.Join(",", searchListResponse.Items.Select(x => x.Id.VideoId));

                var listVideosResponse = await listVideosRequest.ExecuteAsync();

                var songs = new List<YoutubeSong>();

                foreach (var video in listVideosResponse.Items)
                {

                    var duration = video.ContentDetails.Duration;

                    var url = string.Format("https://www.youtube.com/watch?v={0}", video.Id);

                    var song = new YoutubeSong(url, XmlConvert.ToTimeSpan(duration))
                    {
                        Artist = video.Snippet.ChannelTitle,
                        Title = video.Snippet.Title,
                        Description = video.Snippet.Description,
                        ThumbnailSource = new Uri(video.Snippet.Thumbnails.Default.Url),
                        Views = Convert.ToInt32(video.Statistics.ViewCount.GetValueOrDefault()),
                        Rating = video.Statistics.LikeCount
                    };

                    songs.Add(song);
                }

                return songs;
            })
            // The API gives no clue what can throw, wrap it all up
            .Catch<IReadOnlyList<YoutubeSong>, Exception>(ex => Observable.Throw<IReadOnlyList<YoutubeSong>>(new NetworkSongFinderException("YoutubeSongFinder search failed", ex)));
        }
    }
}