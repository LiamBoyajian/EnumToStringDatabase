using System;
using Main.addons.EnumToIcon.main;
using Main.main.scripts.core.plants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Main.addons.EnumToIcon.tests;

[TestClass]
public class AccessIconsDbTest
{
    [TestMethod]
    public void TestGetIcon()
    {
        //Console.WriteLine(AbstractPlant.Rt.Health.GetType().Name);
        Assert.AreEqual("AGA", AccessIconsDb.GetFile(AbstractPlant.Rt.Health, 32));
    }
}