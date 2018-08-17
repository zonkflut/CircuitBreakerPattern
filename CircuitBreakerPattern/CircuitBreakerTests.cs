using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CircuitBreakerPattern 
{
    [TestFixture]
    public class CircuitBreakerTests
    {
        private const int MaximumNumberOfAttempts = 3;
        private readonly TimeSpan resetTimeoutThreshold = TimeSpan.FromSeconds(2);

        private ServiceUnderLoad serviceUnderLoad;
        private FailoverService failoverService;
        private ITestService circuitBreakerService;

        [SetUp]
        public void Setup()
        {
            serviceUnderLoad = new ServiceUnderLoad();
            failoverService = new FailoverService();
            circuitBreakerService = new TestServiceCircuitBreaker(serviceUnderLoad, failoverService, resetTimeoutThreshold, MaximumNumberOfAttempts);
        }
        
        [Test]
        public async Task CircuitBreaker_NoFault_CallsServiceUnderLoad()
        {
            var result = await circuitBreakerService.Operation();

            Assert.AreEqual("Service Under Load called.", result);
            Assert.AreEqual(1, serviceUnderLoad.CallsMade);
            Assert.AreEqual(0, failoverService.CallsMade);
        }

        [Test]
        public async Task CircuitBreaker_Fault_CallsFailoverService()
        {
            serviceUnderLoad.ToggleFailure();

            var result = await circuitBreakerService.Operation();
            Assert.AreEqual("Fail-over called", result);
            Assert.AreEqual(MaximumNumberOfAttempts, serviceUnderLoad.CallsMade);
            Assert.AreEqual(1, failoverService.CallsMade);
        }

        [Test]
        public async Task CircuitBreaker_FailedService_ServiceUnderLoadRestoredAfterFailure()
        {
            serviceUnderLoad.ToggleFailure();
            var result = await circuitBreakerService.Operation();
            Assert.AreEqual("Fail-over called", result);
            
            serviceUnderLoad.ToggleFailure();
            await Task.Delay(resetTimeoutThreshold);
            
            result = await circuitBreakerService.Operation();
            Assert.AreEqual("Service Under Load called.", result);
            Assert.AreEqual(MaximumNumberOfAttempts + 1, serviceUnderLoad.CallsMade);
            Assert.AreEqual(1, failoverService.CallsMade);
        }

        [Test]
        public async Task CircuitBreaker_FailedService_ContinuesCallingFailover_ForExtendedTimeout()
        {
            serviceUnderLoad.ToggleFailure();
            var result = await circuitBreakerService.Operation();
            Assert.AreEqual("Fail-over called", result);
            
            await Task.Delay(resetTimeoutThreshold);
            
            result = await circuitBreakerService.Operation();
            Assert.AreEqual("Fail-over called", result);
            Assert.AreEqual(MaximumNumberOfAttempts + 1, serviceUnderLoad.CallsMade);
            Assert.AreEqual(2, failoverService.CallsMade);
        }

        public class TestServiceCircuitBreaker : CircuitBreaker, ITestService
        {
            private readonly ITestService serviceUnderLoad;
            private readonly ITestService failoverService;

            public TestServiceCircuitBreaker(ITestService serviceUnderLoad, ITestService failoverService, TimeSpan resetTimeoutThreshold, int maximumNumberOfAttempts) 
                : base(resetTimeoutThreshold, maximumNumberOfAttempts)
            {
                this.serviceUnderLoad = serviceUnderLoad;
                this.failoverService = failoverService;
            }

            public async Task<string> Operation()
            {
                return await Protect(
                    () => serviceUnderLoad.Operation(),
                    () => failoverService.Operation());
            }
        }

        public class ServiceUnderLoad : ITestService
        {
            private bool fail;
            public int CallsMade { get; private set; }
            public void ToggleFailure() => fail = !fail;
            public async Task<string> Operation()
            {
                CallsMade++;
                return await Task.Run(() => !fail 
                    ? "Service Under Load called." 
                    : throw new Exception());
            }
        }

        public class FailoverService : ITestService
        {
            public int CallsMade { get; private set; }
            public async Task<string> Operation()
            {
                CallsMade++;
                return await Task.Run(() => "Fail-over called");
            }
        }
    }
}
