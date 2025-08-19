using Microsoft.Xrm.Sdk.Organization;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

internal static class EndpointCollectionExtensions
{
    public static Entity ToEntity(this EndpointCollection endpoints, out string instanceUrl, out Uri? instanceApiUri)
    {
        Entity e = new();
        string? localInstanceUrl = null;
        Uri? localInstanceApiUri = null;
        foreach ((EndpointType type, string endpoint) in endpoints)
        {
            if (type is EndpointType.WebApplication)
            {
                localInstanceUrl = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
            }
            if (type is EndpointType.OrganizationDataService)
            {
                localInstanceApiUri = new(new Uri(endpoint), "/");
            }
            e[type.ToString()] = endpoint;
        }
        instanceUrl = localInstanceUrl
            ?? FallbackInstanceUrl(endpoints, EndpointType.OrganizationService)
            ?? FallbackInstanceUrl(endpoints, EndpointType.OrganizationDataService)!;
        instanceApiUri = localInstanceApiUri;
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
