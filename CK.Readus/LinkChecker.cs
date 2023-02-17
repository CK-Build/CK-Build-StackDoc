using System.Net.Http.Headers;
using CK.Core;

namespace CK.Readus;

internal class LinkChecker
{
    private readonly HttpClient _httpClient = new();

    public LinkChecker()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add( new ProductInfoHeaderValue( "StackDoc", "1.0.0" ) );
    }

    public async Task CheckLinkAvailabilityAsync( IActivityMonitor monitor, NormalizedPath link )
    {
        if( link.RootKind != NormalizedPathRootKind.RootedByURIScheme ) return;

        var request = new HttpRequestMessage( HttpMethod.Head, link );
        var response = await _httpClient.SendAsync( request );

        if( response.IsSuccessStatusCode is false )
        {
            monitor.Info( $"Link `{link}` failed on HEAD." );
            request = new HttpRequestMessage( HttpMethod.Get, link );
            response = await _httpClient.SendAsync( request );

            if( response.IsSuccessStatusCode is false )
            {
                monitor.Error( $"Link `{link}` is unreachable. Status code: {response.StatusCode}`" );
                return;
            }
        }

        monitor.Info( $"Link `{link}` is online." );
    }
}
