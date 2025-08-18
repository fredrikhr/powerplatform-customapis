using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Microsoft.PowerApps.CoreFramework.CapCoreServices.TopologyModel;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.PowerApps.CoreFramework.PowerPlatform.Api;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class PowerPlatformApiDiscovery(ClusterCategory clusterCategory)
{
    private const string TenantInfix = "tenant";

    private const string EnvironmentInfix = "environment";

    private const string OrganizationInfix = "organization";

    private const string TenantIslandPrefix = "il-";
    private readonly int _idSuffixLength = GetIdSuffixLength(clusterCategory);

    public string TokenAudience => "https://" + GlobalEndpoint;

    public string GlobalEndpoint { get; } = GetEndpointSuffix(clusterCategory);

    public string GlobalUserContentEndpoint { get; } = GetUserContentEndpointSuffix(clusterCategory);

    public string GetTenantEndpoint(Guid tenantId)
    {
        return BuildEndpoint(TenantInfix, tenantId.ToString("N"));
    }

    public string GetTenantIslandClusterEndpoint(Guid tenantId)
    {
        return BuildEndpoint(TenantInfix, tenantId.ToString("N"), TenantIslandPrefix);
    }

    public string GetEnvironmentEndpoint(string environmentId)
    {
        ThrowIfStringIsNullOrEmpty(environmentId);
        return BuildEndpoint(EnvironmentInfix, environmentId);
    }

    public string GetEnvironmentUserContentEndpoint(string environmentId)
    {
        ThrowIfStringIsNullOrEmpty(environmentId);
        return BuildEndpoint(EnvironmentInfix, environmentId, "", userContentEndpoint: true);
    }

    public string GetOrganizationEndpoint(Guid organizationId)
    {
        return BuildEndpoint(OrganizationInfix, organizationId.ToString("N"));
    }

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "URL")]
    [SuppressMessage("CodeQuality", "IDE0079: Remove unnecessary suppression", Justification = "false negative")]
    private string BuildEndpoint(string infix, string resourceId, string prefix = "", bool userContentEndpoint = false)
    {
        string urlSafeResourceId = resourceId.ToLowerInvariant().Replace("-", "");
        string resourceIdPrefix = urlSafeResourceId[..^_idSuffixLength];
        string resourceIdSuffix = urlSafeResourceId[^_idSuffixLength..];
        // string text3 = text.Substring(text.Length - idSuffixLength, idSuffixLength);
        string suffix = userContentEndpoint ? GlobalUserContentEndpoint : GlobalEndpoint;
        return prefix + resourceIdPrefix + "." + resourceIdSuffix + "." + infix + "." + suffix;
    }

    private static string GetUserContentEndpointSuffix(ClusterCategory category)
    {
        return category switch
        {
            ClusterCategory.Local => "api.powerplatformusercontent.localhost",
            ClusterCategory.Exp => "api.exp.powerplatformusercontent.com",
            ClusterCategory.Dev => "api.dev.powerplatformusercontent.com",
            ClusterCategory.Prv => "api.prv.powerplatformusercontent.com",
            ClusterCategory.Test => "api.test.powerplatformusercontent.com",
            ClusterCategory.Preprod => "api.preprod.powerplatformusercontent.com",
            ClusterCategory.FirstRelease => "api.powerplatformusercontent.com",
            ClusterCategory.Prod => "api.powerplatformusercontent.com",
            ClusterCategory.GovFR => "api.gov.powerplatformusercontent.microsoft.us",
            ClusterCategory.Gov => "api.gov.powerplatformusercontent.microsoft.us",
            ClusterCategory.High => "api.high.powerplatformusercontent.microsoft.us",
            ClusterCategory.DoD => "api.appsplatformusercontent.us",
            ClusterCategory.Mooncake => "api.powerplatformusercontent.partner.microsoftonline.cn",
            ClusterCategory.Ex => "api.powerplatformusercontent.eaglex.ic.gov",
            ClusterCategory.Rx => "api.powerplatformusercontent.microsoft.scloud",
            _ => throw new ArgumentException($"Invalid cluster category value: {category}", nameof(category)),
        };
    }

    private static string GetEndpointSuffix(ClusterCategory category)
    {
        return category switch
        {
            ClusterCategory.Local => "api.powerplatform.localhost",
            ClusterCategory.Exp => "api.exp.powerplatform.com",
            ClusterCategory.Dev => "api.dev.powerplatform.com",
            ClusterCategory.Prv => "api.prv.powerplatform.com",
            ClusterCategory.Test => "api.test.powerplatform.com",
            ClusterCategory.Preprod => "api.preprod.powerplatform.com",
            ClusterCategory.FirstRelease => "api.powerplatform.com",
            ClusterCategory.Prod => "api.powerplatform.com",
            ClusterCategory.GovFR => "api.gov.powerplatform.microsoft.us",
            ClusterCategory.Gov => "api.gov.powerplatform.microsoft.us",
            ClusterCategory.High => "api.high.powerplatform.microsoft.us",
            ClusterCategory.DoD => "api.appsplatform.us",
            ClusterCategory.Mooncake => "api.powerplatform.partner.microsoftonline.cn",
            ClusterCategory.Ex => "api.powerplatform.eaglex.ic.gov",
            ClusterCategory.Rx => "api.powerplatform.microsoft.scloud",
            _ => throw new ArgumentException($"Invalid cluster category value: {category}", nameof(category)),
        };
    }

    private static int GetIdSuffixLength(ClusterCategory category)
    {
        return (uint)(category - 4) <= 1u ? 2 : 1;
    }

    private static void ThrowIfStringIsNullOrEmpty(
        [NotNull] string? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null
        )
    {
        switch (argument)
        {
            case null:
                throw new ArgumentNullException(paramName);
            case "":
                throw new ArgumentException(message: default, paramName);
        }
    }

    public static PowerPlatformApiDiscovery FromClusterCategoryName(
        string? clusterCategoryName
        )
    {
        if (!Enum.TryParse(clusterCategoryName, ignoreCase: true, out ClusterCategory clusterCategory))
            clusterCategory = ClusterCategory.Prod;
        return new(clusterCategory);
    }
}
