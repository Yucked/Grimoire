namespace Grimoire.Commons.Parsing;

public sealed record ParserOptions(int MaxDelay,
                                  int MaxRetries,
                                  string[] Proxies,
                                  string[] UserAgents);