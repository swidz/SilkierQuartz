using Quartz;
using SilkierQuartz;

namespace TestJobs.Jobs
{
    [SilkierQuartz(5, "this is ext lib job", "HelloJobAutoExt", TriggerGroup = "SilkierQuartz")]
    public class HelloJobAutoExt : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"Hello from Ext {DateTime.Now}");
            return Task.CompletedTask;
        }
    }
}