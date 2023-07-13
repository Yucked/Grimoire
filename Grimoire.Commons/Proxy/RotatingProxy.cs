using System.Net;

namespace Grimoire.Commons.Parsing;

public sealed class RotatingProxy : IWebProxy {
    public ICredentials? Credentials { get; set; }

    private readonly WebProxy[] _proxies;

    private WebProxy Proxy
        => _proxies.RandomItem();

    public RotatingProxy(IEnumerable<string> proxies) {
        _proxies = proxies
            .Select(x => {
                var proxy = Parse(x);
                return new WebProxy(proxy.Host, proxy.Port);
            })
            .ToArray();
    }

    public Uri? GetProxy(Uri destination) {
        return Proxy.GetProxy(destination);
    }

    public bool IsBypassed(Uri host) {
        return Proxy.IsBypassed(host);
    }

    private static (string Host, int Port) Parse(string proxy) {
        var split = proxy.Split(':');
        return (split[0], int.Parse(split[1]));
    }
}