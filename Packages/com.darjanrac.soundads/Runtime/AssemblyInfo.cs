using System.Runtime.CompilerServices;

// Internals are visible to the SDK's own editor tooling and test assemblies only.
// Publishers interact exclusively with the public API (SoundAdsSdk + DTOs/enums).
[assembly: InternalsVisibleTo("SoundAds.Editor")]
[assembly: InternalsVisibleTo("SoundAds.Tests.Editor")]
[assembly: InternalsVisibleTo("SoundAds.Tests.Runtime")]
