using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Hoc.Pages;

public class Index_Tests : HocWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
