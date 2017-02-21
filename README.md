# Image Pipeline for Universal Windows Platform (UWP)
[Fresco’s](http://frescolib.org/) image pipeline will load images from the network, local storage, or local resources. To save data and CPU, it has three levels of cache; two in memory and another in internal storage.

## Installation
[![NuGet](https://img.shields.io/nuget/v/fresco.imagepipeline.svg)](https://www.nuget.org/packages/fresco.imagepipeline/)

## Features
* Has a memory management system that uses the native heap to minimize the impact of garbage collection.
* Easy to apply custom postprocessors.
* Support progressive rendering for JPEG images.
* Configurable disk and memory caching.

## Architecture

![architecture](https://scontent.fbed1-2.fna.fbcdn.net/v/t39.2365-6/11057083_393469260836543_318035251_n.png?oh=7aa5c622b7f9c9f18a599dd70480a813&oe=59443FE7)

## Simple usage

```C#
            // Initializes ImagePipeline
            var imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
            
            // Fetch an encoded image
            BitmapImage bitmap = await imagePipeline.FetchEncodedBitmapImageAsync(uri);
            // Do something with the bitmap
            // ...
            
            // Fetch a decoded image
            WriteableBitmap bitmap = await imagePipeline.FetchDecodedBitmapImageAsync(ImageRequest.FromUri(uri));
            // Do something with the bitmap
            // ...
            
            // Prefetch to disk cache
            await imagePipeline.PrefetchToDiskCacheAsync(uri).ConfigureAwait(false);
            
            // Clear caches
            await imagePipeline.ClearCachesAsync().ConfigureAwait(false);
```

Check out the [Image Pipeline](http://frescolib.org/docs/intro-image-pipeline.html) documentation for further details about the Image Pipeline API in general.
