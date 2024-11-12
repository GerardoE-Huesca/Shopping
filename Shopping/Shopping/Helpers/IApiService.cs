using Shopping.Common;

namespace Shopping.Helpers
{
    public interface IApiService
    {
        Task<Response> GetListAsync<T>(string servicePrefix, string controller);
    }
}
