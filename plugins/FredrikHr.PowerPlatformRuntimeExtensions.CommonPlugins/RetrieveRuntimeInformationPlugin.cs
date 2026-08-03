using System.Reflection;

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
        public const string DataverseEndpoints = nameof(DataverseEndpoints);
        public const string PowerPlatformApiDiscovery = nameof(PowerPlatformApiDiscovery);
    }

    private static readonly HashSet<string> KnownAssemblyNames = new([
        "CoreFramework.CapCoreServices.TopologyModel"
    ], StringComparer.OrdinalIgnoreCase);

    private ITracingService? _trace;

    public RetrieveRuntimeInformationPlugin()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            if (string.IsNullOrEmpty(args.Name)) return null;
            Assembly? loadedAssembly;
            try
            {
                AssemblyName name = new(args.Name);
                if (!KnownAssemblyNames.Contains(name.Name)) return null;

                string filename = $"{name.Name}.dll";
                string filepath = Path.Combine(Environment.CurrentDirectory, filename);
                if (File.Exists(filepath))
                {
                    loadedAssembly = Assembly.LoadFile(filepath);
                    TraceAssemblyLoading(name, loadedAssembly, _trace);
                    return loadedAssembly;
                }

                string cultureDirectory = System.Globalization.CultureInfo.CurrentCulture.Name;
                filepath = Path.Combine(Environment.CurrentDirectory, cultureDirectory, filename);
                if (File.Exists(filepath))
                {
                    loadedAssembly = Assembly.LoadFile(filepath);
                    TraceAssemblyLoading(name, loadedAssembly, _trace);
                    return loadedAssembly;
                }

                cultureDirectory = "en-US";
                filepath = Path.Combine(Environment.CurrentDirectory, cultureDirectory, filename);
                if (File.Exists(filepath))
                {
                    loadedAssembly = Assembly.LoadFile(filepath);
                    TraceAssemblyLoading(name, loadedAssembly, _trace);
                    return loadedAssembly;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception) { return null; }
#pragma warning restore CA1031 // Do not catch general exception types

            return null;
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031: Do not catch general exception types",
            Justification = nameof(ITracingService)
        )]
        static void TraceAssemblyLoading(AssemblyName requestedName, Assembly? loadedAssembly, ITracingService? trace)
        {
            try
            {
                trace?.Trace("Resolving known assembly name '{0}' -> Loaded assembly '{1}'.", requestedName, loadedAssembly?.GetName());
            }
            catch (Exception)
            {
                // Do not do anything here on purpose.
            }
        }
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext7>();
        var envInfo = serviceProvider.Get<IEnvironmentService>();
        ParameterCollection outputs = context.OutputParameters;
        _trace = serviceProvider.Get<ITracingService>();
        var orgService = serviceProvider.GetOrganizationService(context.UserId);

        outputs[OutputParameterNames.EnvironmentInfo] =
            GetEnvironmentInfo(envInfo);
        outputs[OutputParameterNames.WhoAmIDetails] =
            GetWhoAmIExtendedInfo(context);
        outputs[OutputParameterNames.OrganizationDetails] =
            GetOrganizationDetails(orgService, out Entity endpointsEntity);
        outputs[OutputParameterNames.DataverseEndpoints] = endpointsEntity;
        outputs[OutputParameterNames.PowerPlatformApiDiscovery] =
            GetApiDiscovery(serviceProvider);
    }

    private static Entity GetEnvironmentInfo(IEnvironmentService envInfo)
    {
        Entity envDetails = new();
        envDetails[nameof(envInfo.AzureAuthorityHost)] = envInfo.AzureAuthorityHost?.ToString();
        envDetails[nameof(envInfo.AzureRegionName)] = envInfo.AzureRegionName;
        envDetails[nameof(envInfo.Geo)] = envInfo.Geo;
        return envDetails;
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
        if (context.PortalsContactId != Guid.Empty)
        {
            e[nameof(context.PortalsContactId)] = context.PortalsContactId;
        }
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
        endpointsEntity = detail.Endpoints.ToEntity(out string instanceUrl, out string? instanceApiUrl);
        ExtendEndpointEntity(endpointsEntity, instanceUrl, instanceApiUrl, detail.OrganizationVersion);
        e[nameof(OrganizationState)] = detail.State.ToString();
        e[$"{nameof(OrganizationState)}Value"] = (int)detail.State;
        e[nameof(detail.SchemaType)] = detail.SchemaType;
        e[nameof(detail.OrganizationType)] = detail.OrganizationType.ToString();
        e[$"{nameof(detail.OrganizationType)}Value"] = (int)detail.OrganizationType;
        return e;

        static void ExtendEndpointEntity(Entity e, string instanceUrl, string? instanceApiUrl, string version)
        {
            if (string.IsNullOrEmpty(version)) version = "9.2";
            instanceApiUrl ??= instanceUrl;
            string odataUrl = $"{instanceApiUrl}/api/data/v{version}";
            e["ODataApi"] = odataUrl;
            e["ODataMetadata"] = $"{odataUrl}/$metadata";
            e["TokenAudience"] = instanceUrl;
        }
    }

    private static Entity GetApiDiscovery(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.Get<IPluginExecutionContext6>();
        Entity entity = new();
        var apiDiscovery = PowerPlatformApiDiscovery
            .FromPluginServiceProvider(serviceProvider);
        entity[nameof(apiDiscovery.TokenAudience)] = apiDiscovery.TokenAudience;
        entity[nameof(apiDiscovery.GlobalEndpoint)] = apiDiscovery.GlobalEndpoint;
        entity[nameof(apiDiscovery.GlobalUserContentEndpoint)] = apiDiscovery.GlobalUserContentEndpoint;
        entity["TenantEndpoint"] = apiDiscovery.GetTenantEndpoint(context.TenantId);
        entity["TenantIslandClusterEndpoint"] = apiDiscovery.GetTenantIslandClusterEndpoint(context.TenantId);
        entity["EnvironmentEndpoint"] = apiDiscovery.GetEnvironmentEndpoint(context.EnvironmentId);
        entity["EnvironmentUserContentEndpoint"] = apiDiscovery.GetEnvironmentUserContentEndpoint(context.EnvironmentId);
        entity["OrganizationEndpoint"] = apiDiscovery.GetOrganizationEndpoint(context.OrganizationId);
        return entity;
    }
}
