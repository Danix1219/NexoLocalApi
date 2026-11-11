using MongoDB.Driver;
using StackExchange.Redis;

namespace Nexo.Common.Errors;

public static class DependencyErrors
{
    public static bool IsUnavailable(Exception error) => error is MongoException or RedisException or TimeoutException or HttpRequestException;
}
