using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using IF.Lastfm.Core.Scrobblers;
using System;

namespace Espera.Core
{
    public class LastFm
    {
        private const string ApiKey = "2ee804dda9230dea97fb769bfc805d6f";
        private const string ApiSecret = "5959a2f7ffe3c58853993fd4c1565228";

        private LastAuth lastAuth;
        private Scrobbler scrobbler;
        private TrackApi trackApi;

        public LastFm()
        {
            this.lastAuth = new LastAuth(ApiKey, ApiSecret);
            this.scrobbler = new Scrobbler(this.lastAuth);
            this.trackApi = new TrackApi(lastAuth);
        }

        public async void Scrobble(Song song, DateTime? scrobbleTime = null)
        {
            try
            {
                if (song != null)
                {
                    LastResponse lastResponse = await this.lastAuth.GetSessionTokenAsync(this.Username, this.Password);

                    if (lastResponse.Success)
                    {
                        Scrobble scrobble = new Scrobble(song.Artist, song.Album, song.Title, scrobbleTime ?? DateTime.Now);
                        ScrobbleResponse scrobbleResponse = await this.scrobbler.ScrobbleAsync(scrobble);
                    }
                }
            }
            catch (Exception exception)
            {
            }
        }

        public async void NowPlaying(Song song, DateTime? nowPlayingTime = null)
        {
            try
            {
                if (song != null)
                {
                    LastResponse lastResponse = await this.lastAuth.GetSessionTokenAsync(this.Username, this.Password);

                    if (lastResponse.Success)
                    {
                        Scrobble scrobble = new Scrobble(song.Artist, song.Album, song.Title, nowPlayingTime ?? DateTime.Now);
                        lastResponse = await this.trackApi.UpdateNowPlayingAsync(scrobble);
                    }
                }
            }
            catch (Exception exception)
            {
            }
        }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
