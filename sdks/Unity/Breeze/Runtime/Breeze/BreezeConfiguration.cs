namespace BreezeSdk.Runtime
{

    /// <summary>
    /// Configuration object required to initialize the Breeze SDK via <see cref="Breeze.Initialize"/>.
    /// </summary>
    public sealed class BreezeConfiguration
    {
        /// <summary>
        /// The custom URL scheme registered for your application (without the colon or slashes).
        /// Used by the Breeze payment page to redirect back into your game after a payment flow.
        /// Must match the scheme declared in your iOS <c>Info.plist</c> and Android <c>AndroidManifest.xml</c>.
        /// Example: <c>"yourgame"</c> (results in the redirect URL <c>yourgame://...</c>).
        /// </summary>
        public string AppScheme { get; set; }

        /// <summary>
        /// Specifies which Breeze backend environment the SDK communicates with.
        /// Defaults to <see cref="BreezeEnvironment.Production"/>.
        /// Use <see cref="BreezeEnvironment.Development"/> during development and QA.
        /// </summary>
        public BreezeEnvironment Environment { get; set; } = BreezeEnvironment.Production;
    }

    /// <summary>
    /// Selects the Breeze backend environment used by the SDK.
    /// </summary>
    public enum BreezeEnvironment
    {
        /// <summary>The live production environment. Use for released builds.</summary>
        Production = 0,

        /// <summary>The development/sandbox environment. Use during testing and QA.</summary>
        Development = 1,
    }
}