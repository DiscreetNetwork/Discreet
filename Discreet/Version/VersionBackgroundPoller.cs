using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Version
{
    internal class VersionBackgroundPoller
    {
        /// <summary>
        /// returns: (localVersion, newVersion)
        /// </summary>
        public Action<System.Version, System.Version> UpdateAvailable;

        private HttpClient _httpClient;

        private const int _delayTimeMilliseconds = 1000 * 60;

        private CancellationTokenSource _cts;

        /// <summary>
        /// Starts the background poller
        /// </summary>
        /// <returns></returns>
        public async Task StartBackgroundPoller()
        {
            _cts = new CancellationTokenSource();
            _httpClient = new HttpClient();

            System.Version localVersion = Assembly.GetExecutingAssembly().GetName().Version;

            while (!_cts.IsCancellationRequested)
            {
                System.Version remoteVersion = await GetRemoteVersion();
                if (remoteVersion is null)
                {
                    await Task.Delay(_delayTimeMilliseconds);
                    continue;
                }

                if (remoteVersion.Major > localVersion.Major || remoteVersion.Minor > localVersion.Minor || remoteVersion.Build > localVersion.Build)
                {
                    UpdateAvailable.Invoke(localVersion, remoteVersion);
                }

                await Task.Delay(_delayTimeMilliseconds);
            }

            _httpClient.Dispose();
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task<System.Version> GetRemoteVersion()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://releases.discreet.net/versions/daemon", _cts.Token);
                var content = await response.Content.ReadAsStringAsync(_cts.Token);
                if (!response.IsSuccessStatusCode) return null;

                content = content.Replace("\"", "");
                string[] contentSplit = content.Split(".");
                System.Version remoteVersion = new System.Version(int.Parse(contentSplit[0]), int.Parse(contentSplit[1]), int.Parse(contentSplit[2]));
                return remoteVersion;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
