using FBCore.Concurrency;
using ImagePipeline.Image;
using System;
using System.IO;
using System.Threading;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Network fetcher that uses the simplest Windows stack.
    ///
    /// <para /> Apps requiring more sophisticated networking should implement their own
    /// <see cref="INetworkFetcher{FetchState}"/>.
    /// </summary>
    public class HttpUrlConnectionNetworkFetcher : BaseNetworkFetcher<FetchState>
    {
        private const int NUM_NETWORK_THREADS = 3;
        private const int MAX_REDIRECTS = 5;

        private readonly IExecutorService _executorService;
        private readonly TaskCancellationManager<string> _tasks;
        private readonly HttpClient _client;

        /// <summary>
        /// Instantiates the <see cref="HttpUrlConnectionNetworkFetcher"/>
        /// </summary>
        public HttpUrlConnectionNetworkFetcher() : this(Executors.NewFixedThreadPool(NUM_NETWORK_THREADS))
        {
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
        /// Cleanup resources
        /// </summary>
        ~HttpUrlConnectionNetworkFetcher()
        {
            _client.Dispose();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FetchState"/>-derived object used to store state.
        ///
        /// <param name="consumer">the consumer</param>
        /// <param name="context">the producer's context</param>
        /// @return a new fetch state instance
        /// </summary>
        public override FetchState CreateFetchState(
            IConsumer<EncodedImage> consumer, 
            IProducerContext context)
        {
            return new FetchState(consumer, context);
        }

        /// <summary>
        /// Initiates the network fetch and informs the producer when a response is received via the
        /// provided callback.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// <param name="callback">the callback used to inform the network fetch producer</param>
        /// </summary>
        public override void Fetch(
            FetchState fetchState, 
            INetworkFetcherCallback callback)
        {
            _tasks.Add(fetchState.Id, token =>
                _executorService.Execute(
                () =>
                {
                    FetchSync(fetchState, callback, token);
                }));

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

        internal void FetchSync(
            FetchState fetchState, 
            INetworkFetcherCallback callback,
            CancellationToken token)
        {
            HttpResponseMessage response = default(HttpResponseMessage);

            try
            {
                response = DownloadFrom(fetchState.Uri, MAX_REDIRECTS, token);

                if (response != null)
                {
                    using (var inputStream = response.Content.ReadAsInputStreamAsync().AsTask().Result)
                    {
                        callback.OnResponse(inputStream.AsStreamForRead(), -1);
                    }
                }
            }
            catch (IOException e)
            {
                callback.OnFailure(e);
            }
        }

        private HttpResponseMessage DownloadFrom(Uri uri, int maxRedirects, CancellationToken token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var asyncInfo = _client.SendRequestAsync(request);
            using (token.Register(asyncInfo.Cancel))
            {
                try
                {
                    HttpResponseMessage response = asyncInfo.AsTask().Result;
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
                            return DownloadFrom(nextUri, maxRedirects - 1, token);
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
                catch (OperationCanceledException)
                {
                    token.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        private bool IsHttpSuccess(HttpStatusCode responseCode)
        {
            return (responseCode >= HttpStatusCode.Ok &&
                responseCode < HttpStatusCode.MultipleChoices);
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
