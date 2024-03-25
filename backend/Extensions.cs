using System.Text;

namespace Grimoire;

public static class Extensions {
    public static RestResponse AsResponse(this object @object, int statusCode) {
        return RestResponse.New(statusCode, @object);
    }
}