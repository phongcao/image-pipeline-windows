using FBCore.Concurrency;
using ImagePipeline.Image;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Network fetcher that uses the simplest Windows stack.
    ///
    /// <para />Apps requiring more sophisticated networking should
    /// implement their own <see cref="INetworkFetcher{FetchState}"/>.
    /// </summary>
    public sealed class HttpUrlConnectionNetworkFetcher : BaseNetworkFetcher<FetchState>, IDisposable
    {
        private const int NUM_NETWORK_THREADS = 5;
        private const int MAX_REDIRECTS = 5;

        private readonly IExecutorService _executorService;
        private readonly TaskCancellationManager<string> _tasks;
        private readonly HttpClient _client;

        /// <summary>
        /// Instantiates the <see cref="HttpUrlConnectionNetworkFetcher"/>.
        /// </summary>
        public HttpUrlConnectionNetworkFetcher() : 
            this(Executors.NewFixedThreadPool(NUM_NETWORK_THREADS))
        {
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal HttpUrlConnectionNetworkFetcher(IExecutorService executorService)
        {
            _executorService = executorService;
            _tasks = new TaskCancellationManager<string>();
            _client = new HttpClient(
                new HttpBaseProtocolFilter
                {
                    AllowAutoRedirect = false,
                });
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FetchState"/>-derived
        /// object used to store state.
        /// </summary>
        /// <param name="consumer">The consumer.</param>
        /// <param name="context">The producer's context.</param>
        /// <returns>A new fetch state instance.</returns>
        public override FetchState CreateFetchState(
            IConsumer<EncodedImage> consumer, 
            IProducerContext context)
        {
            return new FetchState(consumer, context);
        }

        /// <summary>
        /// Initiates the network fetch and informs the producer when a
        /// response is received via the provided callback.
        /// </summary>
        /// <param name="fetchState">
        /// The fetch-specific state.
        /// </param>
        /// <param name="callback">
        /// The callback used to inform the network fetch producer.
        /// </param>
        public override void Fetch(
            FetchState fetchState, 
            INetworkFetcherCallback callback)
        {
            _tasks.Add(fetchState.Id, token => FetchASync(fetchState, callback, token));

            fetchState.Context.AddCallbacks(
                new BaseProducerContextCallbacks(
                    () =>
                    {
                        bool isCanceled = _tasks.Cancel(fetchState.Id);
                        if (isCanceled)
                        {
                            callback.OnCancellation();
                        }
                    },
                    () => { },
                    () => { },
                    () => { }));
        }

        internal Task FetchASync(
            FetchState fetchState, 
            INetworkFetcherCallback callback,
            CancellationToken token)
        {
            return _executorService.Execute(
            async () =>
            {
                try
                {
                    using (var response = await DownloadFrom(fetchState.Uri, MAX_REDIRECTS, token).ConfigureAwait(false))
                    {
                        if (response != null)
                        {
                            using (var inputStream = await response.Content.ReadAsInputStreamAsync().AsTask().ConfigureAwait(false))
                            using (var stream = inputStream.AsStreamForRead())
                            {
                                callback.OnResponse(stream, -1);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    callback.OnFailure(e);
                }
            })
            .Result;
        }

        private async Task<HttpResponseMessage> DownloadFrom(
            Uri uri, int maxRedirects, CancellationToken token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var asyncInfo = _client.SendRequestAsync(request);
            using (token.Register(asyncInfo.Cancel))
            {
                try
                {
                    HttpResponseMessage response = await asyncInfo.AsTask().ConfigureAwait(false);
                    HttpStatusCode responseCode = response.StatusCode;
                    if (IsHttpSuccess(responseCode))
                    {
                        return response;
                    }
                    else if (IsHttpRedirect(responseCode))
                    {
                        Uri nextUri = response.Headers.Location;
                        string originalScheme = uri.Scheme;

                        if (maxRedirects > 0 && nextUri != null && !nextUri.Scheme.Equals(originalScheme))
                        {
                            return await DownloadFrom(nextUri, maxRedirects - 1, token)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            string message = (maxRedirects == 0) ? 
                                string.Format("URL {0} follows too many redirects", uri.ToString()) :
                                string.Format("URL {0} returned {1} without a valid redirect", uri.ToString(), responseCode);

                            throw new IOException(message);
                        }
                    }
                    else
                    {
                        throw new IOException(
                            string.Format(
                                "Image URL {0} returned HTTP code {1}", 
                                uri.ToString(), 
                                responseCode));
                    }
                }
                catch (Exception)
                {
                    token.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        private bool IsHttpSuccess(HttpStatusCode responseCode)
        {
            return (responseCode == HttpStatusCode.Ok);
        }

        private bool IsHttpRedirect(HttpStatusCode responseCode)
        {
            switch (responseCode)
            {
                case HttpStatusCode.MultipleChoices:
                case HttpStatusCode.MovedPermanently:
                case HttpStatusCode.Found:
                case HttpStatusCode.SeeOther:
                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.PermanentRedirect:
                    return true;
                default:
                    return false;
            }
        }
    }
}
