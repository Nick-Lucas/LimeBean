using System.Reflection;
using Xunit.Runners.UI;

namespace LimeBean.Tests {
    public sealed partial class App : RunnerApplication {
        protected override void OnInitializeRunner() {
            AddTestAssembly(GetType().GetTypeInfo().Assembly);
        }
    }
}