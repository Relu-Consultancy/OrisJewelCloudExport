namespace OrisJewelCloudExport.Source;

/// <summary>The Oris API could not be reached, or returned a response that cannot be used.</summary>
public sealed class OrisApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
