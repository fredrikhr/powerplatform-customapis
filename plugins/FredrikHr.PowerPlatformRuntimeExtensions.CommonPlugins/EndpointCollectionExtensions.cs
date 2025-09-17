using Microsoft.Xrm.Sdk.Organization;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

internal static class EndpointCollectionExtensions
{
    public static Entity ToEntity(this EndpointCollection endpoints, out string instanceUrl, out string? instanceApiUrl)
    {
        Entity e = new();
        string? localInstanceUrl = null;
        string? localInstanceApiUrl = null;
        foreach ((EndpointType type, string endpoint) in endpoints)
        {
            if (type is EndpointType.WebApplication)
            {
                localInstanceUrl = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
            }
            if (type is EndpointType.OrganizationDataService)
            {
                localInstanceApiUrl = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
            }
            e[type.ToString()] = endpoint;
        }
        instanceUrl = localInstanceUrl
            ?? FallbackInstanceUrl(endpoints, EndpointType.OrganizationService)
            ?? FallbackInstanceUrl(endpoints, EndpointType.OrganizationDataService)!;
        instanceApiUrl = localInstanceApiUrl;
        return e;

        static string? FallbackInstanceUrl(EndpointCollection endpoints, EndpointType type)
        {
            if (!endpoints.TryGetValue(type, out string endpoint)) return null;
            Uri uri = new(endpoint);
            return uri.GetLeftPart(UriPartial.Authority);
        }
    }

    public static void Deconstruct(
        this KeyValuePair<EndpointType, string> entry,
        out EndpointType type,
        out string endpoint
        )
    {
        (type, endpoint) = (entry.Key, entry.Value);
    }
}
