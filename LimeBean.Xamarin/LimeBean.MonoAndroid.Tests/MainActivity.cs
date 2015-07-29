using System;
using System.Reflection;
using Android.App;
using Android.OS;
using Xunit.Runners.UI;

namespace LimeBean.MonoAndroid.Tests {
    [Activity(Label = "xUnit Android Runner", MainLauncher = true)]
    public class MainActivity : RunnerActivity {

        protected override void OnCreate(Bundle bundle) {
            AddTestAssembly(Assembly.GetExecutingAssembly());
            base.OnCreate(bundle);
        }
    }
}

