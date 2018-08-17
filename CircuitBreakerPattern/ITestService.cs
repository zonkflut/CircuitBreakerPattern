using System.Threading.Tasks;

namespace CircuitBreakerPattern 
{
    public interface ITestService
    {
        Task<string> Operation();
    }
}
