using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule;
using System.IO;

namespace TestMuleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MuleApplication app = MuleApplication.Instance;

                app.InitApplication();
                app.Preference.Save();
                app.StartUp();
            }
            catch (Exception ex)
            {
                Mpd.Logging.MpdLogger.Log(ex);
            }

            MuleApplication.Instance.Stop();

            Console.ReadKey();
        }
    }
}
