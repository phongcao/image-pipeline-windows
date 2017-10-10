﻿using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Represents a local file fetch producer.
    /// </summary>
    public class LocalFileFetchProducer : LocalFetchProducer
    {
        internal const string PRODUCER_NAME = "LocalFileFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="LocalFileFetchProducer"/>.
        /// </summary>
        public LocalFileFetchProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory) : base(
                executor,
                pooledByteBufferFactory)
        {
        }

        /// <summary>
        /// Gets the encoded image.
        /// </summary>
        protected override Task<EncodedImage> GetEncodedImage(ImageRequest imageRequest)
        {
            Task<StorageFile> uriToFilePathTask = StorageFile.GetFileFromApplicationUriAsync(imageRequest.SourceUri).AsTask();
            return uriToFilePathTask.ContinueWith<EncodedImage>((filepathTask) => {
                FileInfo file = new FileInfo(filepathTask.Result.Path);
                return GetEncodedImage(file.OpenRead(), (int)(file.Length));
            });
        }

        /// <summary>
        /// The name of the Producer.
        /// </summary>
        protected override string ProducerName
        {
            get
            {
                return PRODUCER_NAME;
            }
        }
    }
}
