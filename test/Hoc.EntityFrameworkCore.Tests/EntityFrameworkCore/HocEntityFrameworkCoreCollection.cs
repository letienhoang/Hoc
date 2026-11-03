using Xunit;

namespace Hoc.EntityFrameworkCore;

[CollectionDefinition(HocTestConsts.CollectionDefinitionName)]
public class HocEntityFrameworkCoreCollection : ICollectionFixture<HocEntityFrameworkCoreFixture>
{

}
