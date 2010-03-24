using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule;

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

                int j = 0;
                int i = 1000 / j;
            }
            catch (Exception ex)
            {
                Mpd.Logging.MpdLogger.Log(ex);
            }

            Console.ReadKey();
        }
    }
}
