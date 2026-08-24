namespace Api.Services;

public sealed class SharedServiceConfigurationException(string message) : Exception(message);

public sealed class SharedServiceException(string message) : Exception(message);
