using GeoServerDesktop.GeoServerClient.Configuration;
using GeoServerDesktop.GeoServerClient.Services;
using System;

namespace GeoServerDesktop.App.Services
{
    /// <summary>
    /// Interface for managing GeoServer connections
    /// </summary>
    public interface IGeoServerConnectionService
    {
        /// <summary>
        /// Gets whether a connection is currently established
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current connection options
        /// </summary>
        GeoServerClientOptions? CurrentOptions { get; }

        /// <summary>
        /// Event raised when connection status changes
        /// </summary>
        event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Connects to a GeoServer instance
        /// </summary>
        /// <param name="options">Connection options</param>
        void Connect(GeoServerClientOptions options);

        /// <summary>
        /// Disconnects from the current GeoServer instance
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets the workspace service
        /// </summary>
        /// <returns>WorkspaceService instance</returns>
        WorkspaceService GetWorkspaceService();

        /// <summary>
        /// Gets the data store service
        /// </summary>
        /// <returns>DataStoreService instance</returns>
        DataStoreService GetDataStoreService();

        /// <summary>
        /// Gets the layer service
        /// </summary>
        /// <returns>LayerService instance</returns>
        LayerService GetLayerService();

        /// <summary>
        /// Gets the style service
        /// </summary>
        /// <returns>StyleService instance</returns>
        StyleService GetStyleService();

        /// <summary>
        /// Gets the layer group service
        /// </summary>
        /// <returns>LayerGroupService instance</returns>
        LayerGroupService GetLayerGroupService();

        /// <summary>
        /// Gets the preview service
        /// </summary>
        /// <returns>PreviewService instance</returns>
        PreviewService GetPreviewService();
    }
}
