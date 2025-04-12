using Blazored.LocalStorage;
using Blazor_WebAssembly.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly ILocalStorageService _blazoredLocalStorage;

        public LocalStorageService(ILocalStorageService blazoredLocalStorage)
        {
            _blazoredLocalStorage = blazoredLocalStorage ?? throw new ArgumentNullException(nameof(blazoredLocalStorage));

            // Subscribe to the events from the underlying localStorage service
            if (_blazoredLocalStorage is Blazored.LocalStorage.ILocalStorageService typedStorage)
            {
                typedStorage.Changing += (sender, args) => Changing?.Invoke(sender, args);
                typedStorage.Changed += (sender, args) => Changed?.Invoke(sender, args);
            }
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            var result = await _blazoredLocalStorage.GetItemAsync<T>(key);
            return result ?? throw new InvalidOperationException($"The item with key '{key}' was not found or is null.");
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            await _blazoredLocalStorage.SetItemAsync(key, value);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _blazoredLocalStorage.RemoveItemAsync(key);
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.ClearAsync(cancellationToken);
        }

        public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.GetItemAsync<T>(key, cancellationToken);
        }

        public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.GetItemAsStringAsync(key, cancellationToken);
        }

        public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.KeyAsync(index, cancellationToken);
        }

        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.KeysAsync(cancellationToken);
        }

        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.ContainKeyAsync(key, cancellationToken);
        }

        public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.LengthAsync(cancellationToken);
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.RemoveItemAsync(key, cancellationToken);
        }

        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.RemoveItemsAsync(keys, cancellationToken);
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.SetItemAsync(key, data, cancellationToken);
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            return _blazoredLocalStorage.SetItemAsStringAsync(key, data, cancellationToken);
        }

        // Implementing and properly using the events
        public event EventHandler<ChangingEventArgs>? Changing;
        public event EventHandler<ChangedEventArgs>? Changed;
    }
}
