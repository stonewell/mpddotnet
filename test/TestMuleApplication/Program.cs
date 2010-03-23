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
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }
    }
}
