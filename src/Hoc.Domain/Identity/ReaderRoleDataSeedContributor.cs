using System;
using System.Threading.Tasks;
using Hoc.Identity;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace Hoc.Identity;

public class ReaderRoleDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly ICurrentTenant _currentTenant;

    public ReaderRoleDataSeedContributor(
        IGuidGenerator guidGenerator,
        ILookupNormalizer lookupNormalizer,
        IIdentityRoleRepository roleRepository,
        IdentityRoleManager roleManager,
        ICurrentTenant currentTenant)
    {
        _guidGenerator = guidGenerator;
        _lookupNormalizer = lookupNormalizer;
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _currentTenant = currentTenant;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        using (_currentTenant.Change(context.TenantId))
        {
            var readerRole = await _roleRepository.FindByNormalizedNameAsync(
                _lookupNormalizer.NormalizeName(HocRoleNames.Reader));

            if (readerRole is not null)
            {
                return;
            }

            readerRole = new IdentityRole(
                _guidGenerator.Create(),
                HocRoleNames.Reader,
                context.TenantId);

            var result = await _roleManager.CreateAsync(readerRole);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Failed to create the Reader role.");
            }
        }
    }
}
