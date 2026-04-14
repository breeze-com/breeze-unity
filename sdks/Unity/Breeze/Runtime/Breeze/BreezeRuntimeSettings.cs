using UnityEngine;

namespace BreezeSdk.Runtime
{
    /// <summary>
    /// ScriptableObject that stores Breeze SDK configuration so it can be loaded
    /// at runtime via <c>Resources.Load</c>.  Managed automatically by the
    /// Breeze Setup editor window — you should not need to edit this asset by hand.
    /// </summary>
    public class BreezeRuntimeSettings : ScriptableObject
    {
        internal const string ResourcePath = "BreezeRuntimeSettings";

        /// <summary>
        /// Directory inside the Unity project where the runtime settings asset is stored.
        /// </summary>
        public const string AssetDir = "Assets/Breeze/Resources";

        /// <summary>
        /// Full asset path used by the editor to create or load the runtime settings asset.
        /// </summary>
        public const string AssetPath = AssetDir + "/BreezeRuntimeSettings.asset";

        [SerializeField] private string appScheme = "";
        [SerializeField] private BreezeEnvironment environment = BreezeEnvironment.Production;

        /// <summary>
        /// The custom URL scheme registered for the application (e.g. <c>"yourgame"</c>).
        /// Configured via the Breeze Setup editor window (<c>Breeze &gt; Setup</c>).
        /// </summary>
        public string AppScheme => appScheme;

        /// <summary>
        /// The Breeze backend environment the SDK communicates with.
        /// Defaults to <see cref="BreezeEnvironment.Production"/>.
        /// </summary>
        public BreezeEnvironment Environment => environment;

        /// <summary>
        /// Loads the settings asset from <c>Resources</c>.
        /// Returns <c>null</c> when the asset has not been created yet (e.g. Breeze Setup was never saved).
        /// </summary>
        public static BreezeRuntimeSettings Load()
        {
            return Resources.Load<BreezeRuntimeSettings>(ResourcePath);
        }
    }
}
