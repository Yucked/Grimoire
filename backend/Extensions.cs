using Grimoire.Objects;

namespace Grimoire;

public static class Extensions {
    public static ResponseObject AsResponse(this object @object, int statusCode) {
        return ResponseObject.New(statusCode, @object);
    }

    public static ValueTask<ResponseObject> AsResponseAsync(this object @object, int statusCode) {
        return ValueTask.FromResult(ResponseObject.New(statusCode, @object));
    }
}