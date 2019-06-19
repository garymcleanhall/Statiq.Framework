﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common.Content;
using Statiq.Common.Documents;
using Statiq.Common.IO;
using Statiq.Common.Meta;
using Statiq.Common.Tracing;
using Statiq.Common.Util;
using Statiq.Core.Meta;

namespace Statiq.Core.Documents
{
    // Because it's immutable, document metadata can still be accessed after disposal
    // Document source must be unique within the pipeline
    internal class Document : IDocument
    {
        private readonly MetadataStack _metadata;
        private readonly IContentProvider _contentProvider;
        private bool _disposed;

        internal Document(
            MetadataDictionary initialMetadata,
            FilePath source,
            FilePath destination,
            IContentProvider contentProvider,
            IEnumerable<KeyValuePair<string, object>> items)
            : this(
                  Guid.NewGuid().ToString(),
                  0,
                  new MetadataStack(initialMetadata),
                  source,
                  destination,
                  contentProvider,
                  items)
        {
        }

        internal Document(
            Document sourceDocument,
            int version,
            FilePath source,
            FilePath destination,
            IContentProvider contentProvider,
            IEnumerable<KeyValuePair<string, object>> items = null)
            : this(
                sourceDocument.Id,
                version,
                sourceDocument._metadata,
                sourceDocument.Source ?? source,
                destination ?? sourceDocument.Destination,
                contentProvider ?? sourceDocument._contentProvider,
                items)
        {
            sourceDocument.CheckDisposed();
        }

        private Document(
            string id,
            int version,
            MetadataStack metadata,
            FilePath source,
            FilePath destination,
            IContentProvider contentProvider,
            IEnumerable<KeyValuePair<string, object>> items)
        {
            if (source?.IsAbsolute == false)
            {
                throw new ArgumentException("Document sources must be absolute", nameof(source));
            }

            Id = id ?? throw new ArgumentNullException(nameof(id));
            Version = version;
            Source = source;
            Destination = destination;
            _metadata = items == null ? metadata : metadata.Clone(items);

            // Special case to set the content provider to null when cloning
            _contentProvider = contentProvider is NullContent ? null : contentProvider;
        }

        public FilePath Source { get; }

        public FilePath Destination { get; }

        public string Id { get; }

        public int Version { get; }

        public IMetadata Metadata => _metadata;

        public async Task<string> GetStringAsync()
        {
            CheckDisposed();
            Stream stream = await GetStreamAsync();
            if (stream == null || stream == Stream.Null)
            {
                return string.Empty;
            }
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public async Task<Stream> GetStreamAsync()
        {
            CheckDisposed();
            return _contentProvider == null ? Stream.Null : await _contentProvider.GetStreamAsync();
        }

        public IContentProvider ContentProvider
        {
            get
            {
                CheckDisposed();
                return _contentProvider;
            }
        }

        public bool HasContent
        {
            get
            {
                CheckDisposed();
                return _contentProvider != null;
            }
        }

        public override string ToString() => _disposed ? string.Empty : Source?.FullPath ?? string.Empty;

        public void Dispose()
        {
            CheckDisposed();

            Trace.Verbose($"Disposing document with ID {Id}.{Version} and source {Source.ToDisplayString()}");

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(Document),
                    $"Attempted to access disposed document with ID {Id}.{Version} and source {Source.ToDisplayString()}");
            }
        }

        public IMetadata WithoutSettings => new MetadataStack(_metadata.Stack.Reverse().Skip(1));

        public async Task<int> GetCacheHashCodeAsync()
        {
            HashCode hash = default;
            using (Stream stream = await GetStreamAsync())
            {
                hash.Add(await Crc32.CalculateAsync(stream));
            }
            foreach (KeyValuePair<string, object> item in WithoutSettings)
            {
                hash.Add(item.Key);
                hash.Add(item.Value);
            }

            return hash.ToHashCode();
        }

        // IMetadata

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _metadata.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(string key) => _metadata.ContainsKey(key);

        public object this[string key] => _metadata[key];

        public IEnumerable<string> Keys => _metadata.Keys;

        public IEnumerable<object> Values => _metadata.Values;

        public object GetRaw(string key) => _metadata.GetRaw(key);

        public bool TryGetValue<T>(string key, out T value) => _metadata.TryGetValue<T>(key, out value);

        public bool TryGetValue(string key, out object value) => TryGetValue<object>(key, out value);

        public IMetadata GetMetadata(params string[] keys) => _metadata.GetMetadata(keys);

        public int Count => _metadata.Count;
    }
}