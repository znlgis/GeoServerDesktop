using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;
using System;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// Service for managing GeoServer connections
    /// </summary>
    public class GeoServerConnectionService : IGeoServerConnectionService
    {
        private GeoServerClientFactory? _factory;

        /// <summary>
        /// Gets whether a connection is currently established
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets the current connection options
        /// </summary>
        public GeoServerClientOptions? CurrentOptions { get; private set; }

        /// <summary>
        /// Event raised when connection status changes
        /// </summary>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Connects to a GeoServer instance
        /// </summary>
        /// <param name="options">Connection options</param>
        public void Connect(GeoServerClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Disconnect();

            CurrentOptions = options;
            _factory = new GeoServerClientFactory(options);
            IsConnected = true;

            ConnectionStatusChanged?.Invoke(this, true);
        }

        /// <summary>
        /// Disconnects from the current GeoServer instance
        /// </summary>
        public void Disconnect()
        {
            if (_factory != null)
            {
                _factory.Dispose();
                _factory = null;
            }

            IsConnected = false;
            CurrentOptions = null;

            ConnectionStatusChanged?.Invoke(this, false);
        }

        /// <summary>
        /// Gets the workspace service
        /// </summary>
        /// <returns>WorkspaceService instance</returns>
        public WorkspaceService GetWorkspaceService()
        {
            EnsureConnected();
            return _factory!.CreateWorkspaceService();
        }

        /// <summary>
        /// Gets the data store service
        /// </summary>
        /// <returns>DataStoreService instance</returns>
        public DataStoreService GetDataStoreService()
        {
            EnsureConnected();
            return _factory!.CreateDataStoreService();
        }

        /// <summary>
        /// Gets the layer service
        /// </summary>
        /// <returns>LayerService instance</returns>
        public LayerService GetLayerService()
        {
            EnsureConnected();
            return _factory!.CreateLayerService();
        }

        /// <summary>
        /// Gets the style service
        /// </summary>
        /// <returns>StyleService instance</returns>
        public StyleService GetStyleService()
        {
            EnsureConnected();
            return _factory!.CreateStyleService();
        }

        /// <summary>
        /// Gets the layer group service
        /// </summary>
        /// <returns>LayerGroupService instance</returns>
        public LayerGroupService GetLayerGroupService()
        {
            EnsureConnected();
            return _factory!.CreateLayerGroupService();
        }

        /// <summary>
        /// Gets the preview service
        /// </summary>
        /// <returns>PreviewService instance</returns>
        public PreviewService GetPreviewService()
        {
            EnsureConnected();
            return _factory!.CreatePreviewService();
        }

        private void EnsureConnected()
        {
            if (!IsConnected || _factory == null)
                throw new InvalidOperationException("Not connected to a GeoServer instance");
        }
    }
}
