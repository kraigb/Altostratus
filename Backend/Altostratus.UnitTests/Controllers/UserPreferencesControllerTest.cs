using Altostratus.ClientModels;
using Altostratus.DAL;
using Altostratus.Website;
using Altostratus.Website.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace Altostratus.UnitTests.Controllers
{
    [TestClass]
    public class UserPreferencesControllerTest
    {
        Mock<ApplicationDbContext> mockCtx;
        UserPreferencesController controller;

        [TestInitialize()]
        public void Initialize()
        {
            AutoMapperConfig.Configure();

            mockCtx = new Mock<ApplicationDbContext>();

            // Mock user to get User ID
            var claim = new Claim("id", "123");
            var identity = Mock.Of<ClaimsIdentity>(
                x => x.FindFirst(It.IsAny<string>()) == claim
                );

            controller = new UserPreferencesController(mockCtx.Object)
            {
                User = Mock.Of<IPrincipal>(x => x.Identity == identity)
            };
        }

        [TestMethod]
        public async Task GetReturnsUserPreferences()
        {
            var child = new UserCategory() { Category = new Category() { Name = "CAT1" } };
            var parent = new UserPreference
            {
                ApplicationUser_Id = "123",
                UserCategory = new List<UserCategory>()
            };
            parent.UserCategory.Add(child);

            var data = new List<UserPreference> 
            { 
                parent
            }.AsQueryable();

            var childData = new List<UserCategory>
            {
                child
            }.AsQueryable();

            // Mock DbSet
            var mockSet = Helpers.CreateMockSet(data);
            var mockChildSet = Helpers.CreateMockSet(childData);

            mockCtx.SetupGet(mc => mc.UserPreferences).Returns(mockSet.Object);
            mockCtx.SetupGet(mc => mc.UserCategories).Returns(mockChildSet.Object);

            IHttpActionResult actionResult = await controller.GetUserPreferences();
            var contentResult = actionResult as OkNegotiatedContentResult<UserPreferenceDTO>;

            Assert.IsNotNull(contentResult);
            Assert.IsNotNull(contentResult.Content);

            var dto = contentResult.Content;
            Assert.IsNotNull(dto.Categories);
            Assert.AreEqual("CAT1", dto.Categories.First());
        }

        [TestMethod]
        public async Task NoPrefsReturnsNotFound()
        {
            var data = new List<UserPreference>().AsQueryable(); 

            // Mock DbSet with no user preferences
            var mockSet = Helpers.CreateMockSet(data);
            mockCtx.SetupGet(mc => mc.UserPreferences).Returns(mockSet.Object);

            IHttpActionResult actionResult = await controller.GetUserPreferences();

            Assert.IsInstanceOfType(actionResult, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task PutNullReturnsBadRequest()
        {
            var result = await controller.PutUserPreference(null);

            Assert.IsInstanceOfType(result, typeof(BadRequestErrorMessageResult));
        }

        [TestMethod]
        public async Task PutNullCategoryReturnsBadRequest()
        {
            var parent = new UserPreferenceDTO
            {
                Categories = null
            };

            var result = await controller.PutUserPreference(parent);
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
        }

        [TestMethod]
        public async Task PutInvalidCategoryReturnsBadRequest()
        {
            var categories = new List<Category>()
            {
                new Category { Name = "CAT1" },
                new Category { Name = "CAT2" },
            }.AsQueryable();

            // Mock DbSet with no existing user preferences
            var mockCategorySet = Helpers.CreateMockSet(categories);
            mockCtx.SetupGet(mc => mc.Categories).Returns(mockCategorySet.Object);

            var dto = new UserPreferenceDTO
            {
                Categories = new string[] { "CAT1", "CAT3" }  // Includes bogus category
            };

            var result = await controller.PutUserPreference(dto);
            Assert.IsInstanceOfType(result, typeof(BadRequestErrorMessageResult));
        }

        [TestMethod]
        public async Task PutUpdatesDB()
        {
            var data = new List<UserPreference>().AsQueryable();

            var categories = new List<Category>()
            {
                new Category { Name = "CAT1" },
                new Category { Name = "CAT2" },
            }.AsQueryable();

            // Mock DbSet with no existing user preferences
            var mockSet = Helpers.CreateMockSet(data);
            mockCtx.SetupGet(mc => mc.UserPreferences).Returns(mockSet.Object);

            var mockCategorySet = Helpers.CreateMockSet(categories);
            mockCtx.SetupGet(mc => mc.Categories).Returns(mockCategorySet.Object);

            var dto = new UserPreferenceDTO
            {
                Categories = new string[] { "CAT1", "CAT2" }
            };

            var result = await controller.PutUserPreference(dto);

            mockSet.Verify(m => m.Add(It.IsAny<UserPreference>()), Times.Once());
            mockCtx.Verify(m => m.SaveChangesAsync(), Times.Once()); 
        }
    }
}
