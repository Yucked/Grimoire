namespace Grimoire.Commons.Parsing;

public readonly record struct ParserOptions(int MaxDelay,
                                            int MaxRetries,
                                            string[] Proxies,
                                            string[] UserAgents);