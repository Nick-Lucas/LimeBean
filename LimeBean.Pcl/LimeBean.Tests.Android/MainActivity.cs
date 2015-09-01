using System;
using System.Reflection;
using Android.App;
using Android.OS;
using Xunit.Runners.UI;
using Android.Database.Sqlite;

namespace LimeBean.Tests {

    [Activity(Label = "LimeBean.Tests", MainLauncher = true)]
    public class MainActivity : RunnerActivity {

        protected override void OnCreate(Bundle bundle) {
            AddTestAssembly(Assembly.GetExecutingAssembly());
            base.OnCreate(bundle);

            if(!CheckSqliteVersion()) {
                new AlertDialog.Builder(this)
                    .SetMessage("SQLite binary version is too old on this device")
                    .SetCancelable(false)
                    .Show();
            }
        }

        bool CheckSqliteVersion() {
            var cursor = SQLiteDatabase.OpenOrCreateDatabase(":memory:", null).RawQuery("select sqlite_version()", null);
            cursor.MoveToFirst();
            var version = new Version(cursor.GetString(0));
            return version >= new Version(3, 7, 15);
        }

    }

}

