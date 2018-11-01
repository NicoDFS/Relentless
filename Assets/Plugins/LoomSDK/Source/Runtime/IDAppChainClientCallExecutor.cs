using System;
using System.Threading.Tasks;

namespace Loom.Client
{
    public interface IDAppChainClientCallExecutor
    {
        Task<T> Call<T>(Func<Task<T>> taskProducer);
        Task Call(Func<Task> taskProducer);
        Task<T> StaticCall<T>(Func<Task<T>> taskProducer);
        Task StaticCall(Func<Task> taskProducer);
        Task<T> UnsafeStaticCall<T>(Func<Task<T>> taskProducer);
        Task UnsafeStaticCall(Func<Task> taskProducer);
    }
}
