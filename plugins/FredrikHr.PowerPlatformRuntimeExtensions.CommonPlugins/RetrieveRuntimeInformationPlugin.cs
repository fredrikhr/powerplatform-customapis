using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerApps.CoreFramework.PowerPlatform.Api;
using Microsoft.Xrm.Sdk.Organization;

namespace FredrikHr.PowerPlatformRuntimeExtensions.CommonPlugins;

public class RetrieveRuntimeInformationPlugin : IPlugin
{
    internal static class OutputParameterNames
    {
        public const string EnvironmentInfo = nameof(EnvironmentInfo);
        public const string WhoAmIDetails = nameof(WhoAmIDetails);
        public const string OrganizationDetails = nameof(OrganizationDetails);
        public const string Endpoints = nameof(OrganizationDetail.Endpoints);
        public const string ApiDiscovery = nameof(ApiDiscovery);
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext7>();
        ParameterCollection outputs = context.OutputParameters;
        var trace = serviceProvider.Get<ITracingService>();
        var orgService = serviceProvider.GetOrganizationService(context.UserId);

        outputs[OutputParameterNames.EnvironmentInfo] =
            GetEnvironmentInfo(serviceProvider, out string? clusterCategoryName);
        outputs[OutputParameterNames.WhoAmIDetails] =
            GetWhoAmIExtendedInfo(context);
        outputs[OutputParameterNames.OrganizationDetails] =
            GetOrganizationDetails(orgService, out Entity endpointsEntity);
        outputs[OutputParameterNames.Endpoints] = endpointsEntity;
        outputs[OutputParameterNames.ApiDiscovery] =
            GetApiDiscovery(context, clusterCategoryName);
    }

    private static Entity GetEnvironmentInfo(IServiceProvider serviceProvider, out string? clusterCategory)
    {
        var envInfo = serviceProvider.Get<IEnvironmentService>();
        Entity envDetails = new();
        WriteToEntity(envDetails, envInfo);
        WriteToEntityInternal(envDetails, serviceProvider.Get<IInternalEnvironmentService>(), out clusterCategory);
        return envDetails;

        static void WriteToEntity(Entity e, IEnvironmentService envInfo)
        {
            e[nameof(envInfo.AzureAuthorityHost)] = envInfo.AzureAuthorityHost?.ToString();
            e[nameof(envInfo.AzureRegionName)] = envInfo.AzureRegionName;
            e[nameof(envInfo.Geo)] = envInfo.Geo;
        }

        static void WriteToEntityInternal(Entity e, IInternalEnvironmentService? envInfo, out string? clusterCategory)
        {
            if (envInfo is null)
            {
                clusterCategory = null;
                return;
            }

            WriteToEntity(e, envInfo);
            e[nameof(envInfo.ClusterCategory)] = clusterCategory =
                envInfo.ClusterCategory;
        }
    }

    private static Entity GetWhoAmIExtendedInfo(IPluginExecutionContext7 context)
    {
        Entity e = new();
        e[nameof(context.EnvironmentId)] = context.EnvironmentId;
        e[nameof(context.TenantId)] = context.TenantId;
        e[nameof(context.OrganizationId)] = context.OrganizationId;
        e[nameof(context.BusinessUnitId)] = context.BusinessUnitId;
        e[nameof(context.UserId)] = context.UserId;
        e["UserEntraObjectId"] = context.UserAzureActiveDirectoryObjectId;
        e[nameof(context.AuthenticatedUserId)] = context.AuthenticatedUserId;
        e[nameof(context.InitiatingUserAgent)] = context.InitiatingUserAgent;
        e[nameof(context.InitiatingUserId)] = context.InitiatingUserId;
        e[nameof(context.InitiatingUserApplicationId)] = context.InitiatingUserApplicationId;
        e["InitiatingUserEntraObjectId"] = context.InitiatingUserAzureActiveDirectoryObjectId;
        e[nameof(context.IsApplicationUser)] = context.IsApplicationUser;
        e[nameof(context.IsPortalsClientCall)] = context.IsPortalsClientCall;
        e[nameof(context.PortalsContactId)] = context.PortalsContactId;
        return e;
    }

    private static Entity GetOrganizationDetails(
        IOrganizationService orgService,
        out Entity endpointsEntity
        )
    {
        Entity e = new();
        RetrieveCurrentOrganizationRequest request = new()
        { AccessType = EndpointAccessType.Default };
        var response = (RetrieveCurrentOrganizationResponse)orgService
            .Execute(request);
        OrganizationDetail detail = response.Detail;
        e[nameof(detail.OrganizationId)] = detail.OrganizationId;
        e[nameof(detail.FriendlyName)] = detail.FriendlyName;
        e[nameof(detail.OrganizationVersion)] = detail.OrganizationVersion;
        e[nameof(detail.EnvironmentId)] = detail.EnvironmentId;
        e[nameof(detail.DatacenterId)] = detail.DatacenterId;
        e[nameof(detail.Geo)] = detail.Geo;
        e[nameof(detail.TenantId)] = detail.TenantId;
        e[nameof(detail.UrlName)] = detail.UrlName;
        e[nameof(detail.UniqueName)] = detail.UniqueName;
        endpointsEntity = detail.Endpoints.ToEntity(out string instanceUrl);
        ExtendEndpointEntity(endpointsEntity, instanceUrl, detail.OrganizationVersion);
        e[nameof(OrganizationState)] = detail.State.ToString();
        e[$"{nameof(OrganizationState)}Value"] = (int)detail.State;
        e[nameof(detail.SchemaType)] = detail.SchemaType;
        e[nameof(detail.OrganizationType)] = detail.OrganizationType.ToString();
        e[$"{nameof(detail.OrganizationType)}Value"] = (int)detail.OrganizationType;
        return e;

        static void ExtendEndpointEntity(Entity e, string instanceUrl, string version)
        {
            if (string.IsNullOrEmpty(version)) version = "9.2";
            Uri instanceUri = new(instanceUrl);
            Uri odataUri = new(instanceUri, $"/api/data/v{version}/");
            e["ODataApi"] = odataUri.ToString();
            e["ODataMetadata"] = new Uri(odataUri, "$metadata").ToString();
            e["TokenAudience"] = instanceUri.GetLeftPart(UriPartial.Authority);
        }
    }

    private static Entity GetApiDiscovery(
        IPluginExecutionContext7 context,
        string? clusterCategoryName
        )
    {
        Entity entity = new();
        var apiDiscovery = PowerPlatformApiDiscovery
            .FromClusterCategoryName(clusterCategoryName);
        entity[nameof(apiDiscovery.TokenAudience)] = apiDiscovery.TokenAudience;
        entity[nameof(apiDiscovery.GlobalEndpoint)] = apiDiscovery.GlobalEndpoint;
        entity[nameof(apiDiscovery.GlobalUserContentEndpoint)] = apiDiscovery.GlobalUserContentEndpoint;
        entity["TenantEndpoint"] = apiDiscovery.GetTenantEndpoint(context.TenantId);
        entity["OrganizationEndpoint"] = apiDiscovery.GetOrganizationEndpoint(context.OrganizationId);
        entity["EnvironmentEndpoint"] = apiDiscovery.GetEnvironmentEndpoint(context.EnvironmentId);
        entity["EnvironmentUserContentEndpoint"] = apiDiscovery.GetEnvironmentUserContentEndpoint(context.EnvironmentId);
        return entity;
    }
}
