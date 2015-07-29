using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Altostratus.Website.Controllers;
using System.Collections.Generic;
using Altostratus.DAL;
using System.Linq;
using Moq;
using System.Data.Entity;

namespace Altostratus.UnitTests.Controllers
{
    [TestClass]
    public class CategoriesControllerTest
    {
        [TestMethod]
        public void GetReturnsCategories()
        {
            var data = new List<Category> 
            { 
                new Category { Name = "C1" }, 
                new Category { Name = "C2" }, 
                new Category { Name = "C3" }, 
            }.AsQueryable();

            var mockSet = Helpers.CreateMockSet(data);

            var controller = new CategoriesController(
                Mock.Of<ApplicationDbContext>(x => x.Categories == mockSet.Object)
                );

            var response = controller.GetCategories().ToArray();

            Assert.AreEqual(3, response.Length);
            Assert.AreEqual("C1", response[0]);
            Assert.AreEqual("C2", response[1]);
            Assert.AreEqual("C3", response[2]);
        }
    }
}
