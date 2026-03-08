using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Playwright;
using Minio;
using Minio.DataModel.Args;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Minio.Exceptions;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Grimoire.Handlers;

public sealed partial class ScrapingHandler(
    ILogger<ScrapingHandler> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    IBrowser browser,
    IMinioClient minioClient) {
    private readonly IBrowsingContext _context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    private readonly SemaphoreSlim _rateLimiter
        = new(configuration.GetValue<int>("Http:RequestConcurrency"));

    private readonly int _requestDelay
        = configuration.GetValue<int>("Http:RequestDelay");

    private readonly HashSet<string> _confirmedBuckets = [];

    private static readonly Regex BlockedPattern = BlockedRegex();

    [GeneratedRegex(@"(adzerk|analytics|doubleclick|facebook|google-analytics|googletagmanager|disqus)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        "en-US")]
    private static partial Regex BlockedRegex();

    public Task<IDocument> ParseHtmlAsync(string html) {
        return _context.OpenAsync(x => x.Content(html));
    }

    public async Task<IDocument> GetHtmlDocumentAsync(string url) {
        await _rateLimiter.WaitAsync();
        try {
            await Task.Delay(_requestDelay);
            using var responseMessage = await httpClient.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var document = await _context.OpenAsync(x => x.Content(stream));

            if (document.All.Length <= 10 || (document.Body?.TextContent?.Trim() ?? "").Length < 100) {
                var page = await GetPageWithPlaywrightAsync(url);
                document = await _context.OpenAsync(x => x.Content(page));
            }

            return document;
        }
        catch {
            logger.LogError("Unable to reach {url}", url);
            throw;
        }
        finally {
            _rateLimiter.Release();
        }
    }

    public async Task<string> GetPageWithPlaywrightAsync(string url) {
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try {
            await page.RouteAsync("**/*", async route => {
                if (BlockedPattern.IsMatch(route.Request.Url)) {
                    logger.LogDebug("Route matched with blocked resources, aborted: {url}", route.Request.Url);
                    await route.AbortAsync();
                    return;
                }

                await route.ContinueAsync();
            });

            var response = await page.GotoAsync(url, new PageGotoOptions {
                WaitUntil = WaitUntilState.Load
            });

            if (response?.Ok is not true) {
                throw new HttpRequestException($"Playwright unable to fetch {url}");
            }

            await response.FinishedAsync();
            return await page.ContentAsync();
        }
        finally {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    public async Task<JsonDocument> GetJsonDocumentAsync(string url) {
        await _rateLimiter.WaitAsync();
        try {
            await Task.Delay(_requestDelay);
            using var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to fetch JSON from {Url}", url);
            throw;
        }
        finally {
            _rateLimiter.Release();
        }
    }

    private async Task EnsureBucketAsync(string bucket) {
        if (_confirmedBuckets.Contains(bucket)) {
            return;
        }

        try {
            var exists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
            if (!exists) {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                var policy = $$"""
                               {
                                 "Version":"2012-10-17",
                                 "Statement":[{
                                   "Effect":"Allow",
                                   "Principal":{"AWS":["*"]},
                                   "Action":["s3:GetObject"],
                                   "Resource":["arn:aws:s3:::{{bucket}}/*"]
                                 }]
                               }
                               """;
                await minioClient.SetPolicyAsync(
                    new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy));
            }

            _confirmedBuckets.Add(bucket);
        }
        catch (MinioException ex) {
            logger.LogError("MinIO bucket setup failed for '{bucket}': {ex.Message}", bucket, ex.Message);
        }
    }

    public async Task<string> SaveCoverAsync(string imageUrl, string sourceId, string mangaId) {
        await _rateLimiter.WaitAsync();
        try {
            await Task.Delay(_requestDelay);
            using var responseMessage = await httpClient.SendAsync(new HttpRequestMessage {
                Method = HttpMethod.Get,
                RequestUri = new Uri(imageUrl)
            });
            responseMessage.EnsureSuccessStatusCode();

            var ext = Path.GetExtension(imageUrl.Split('?')[0]);

            await EnsureBucketAsync(sourceId);
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var objectPath = $"{mangaId}/cover{(string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext)}";
            await minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(sourceId)
                    .WithObject(objectPath)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length));

            logger.LogDebug("Downloaded cover to {sourceId}/{objectPath}", sourceId, objectPath);
            return $"{sourceId}/{objectPath}";
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to download cover from {Url}", imageUrl);
            throw;
        }
        finally {
            _rateLimiter.Release();
        }
    }

    public async Task<string> SaveImageAsync(string imageUrl, string sourceId, string mangaId) {
        static string CleanImagePath(string imagePath) {
            if (string.IsNullOrWhiteSpace(imagePath)) {
                return string.Empty;
            }

            var decoded = WebUtility.UrlDecode(imagePath);
            var extension = Path.GetExtension(decoded);
            return new string([.. decoded.Replace(extension, string.Empty).Where(char.IsLetterOrDigit)]) + extension;
        }

        await _rateLimiter.WaitAsync();
        try {
            await Task.Delay(_requestDelay);

            using var responseMessage = await httpClient.SendAsync(new HttpRequestMessage {
                Method = HttpMethod.Get,
                RequestUri = new Uri(imageUrl)
            });

            responseMessage.EnsureSuccessStatusCode();

            var fileName = CleanImagePath(responseMessage.Content.Headers.ContentDisposition?.FileNameStar
                                          ?? imageUrl.Split('/')[^1]);
            await EnsureBucketAsync(sourceId);
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            await minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(sourceId)
                    .WithObject($"{mangaId}/{fileName}")
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length));

            logger.LogDebug("Downloaded image to {sourceId}/{mangaId}/{fileName}",
                sourceId, mangaId, fileName);

            return $"{sourceId}/{mangaId}/{fileName}";
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to download image from {Url}", imageUrl);
            throw;
        }
        finally {
            _rateLimiter.Release();
        }
    }
}