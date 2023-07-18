using System.Net;

namespace Grimoire.Commons.Proxy;

public sealed class CustomProxy : WebProxy {
    public bool HasFailed { get; set; }
    public string Host { get; }

    public CustomProxy(string proxy, bool hasFailed) :
        base(proxy.Split(':')[0],
            int.Parse(proxy.Split(':')[1])) {
        Host = proxy;
        HasFailed = hasFailed;
    }
}

public sealed class RotatingProxies : IWebProxy {
    public ICredentials Credentials { get; set; }

    private readonly Dictionary<string, CustomProxy> _proxies;
    public CustomProxy Active { get; private set; }
    private CustomProxy Proxy => GetRandom();

    public RotatingProxies(IEnumerable<string> proxies) {
        _proxies = proxies
            .Select(x => new CustomProxy(x, false))
            .ToDictionary(x => x.Host, y => y);

        Active = GetRandom();
    }

    public Uri GetProxy(Uri destination) {
        return Proxy.GetProxy(destination);
    }

    public bool IsBypassed(Uri host) {
        return Proxy.IsBypassed(host);
    }

    public void MarkCurrentFailed() {
        var temp = _proxies[Active.Host];
        temp.HasFailed = true;
        _proxies[Active.Host] = temp;
    }

    public CustomProxy GetRandom() {
        if (Active is { HasFailed: false }) {
            return Active;
        }

        var failMeNot = _proxies
            .Where(x => !x.Value.HasFailed)
            .ToArray();
        Active = failMeNot.ElementAt(Random.Shared.Next(0, failMeNot.Length - 1)).Value;
        return Active;
    }
}