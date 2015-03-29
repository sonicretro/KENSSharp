namespace SonicRetro.KensSharp.Tests
{
    using System;
    using System.Reflection;
    using NUnit.Gui;

    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            AppEntry.Main(new string[] { Assembly.GetAssembly(typeof(Program)).Location });
        }
    }
}
