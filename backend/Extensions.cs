using Grimoire.Objects;

namespace Grimoire;

public static class Extensions {
    public static ResponseObject AsResponse(this object @object, int statusCode)
        => ResponseObject.New(statusCode, @object);

    public static ValueTask<ResponseObject> AsResponseAsync(this object @object, int statusCode)
        => ValueTask.FromResult(ResponseObject.New(statusCode, @object));
}