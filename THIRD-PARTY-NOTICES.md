# Third-party attribution inventory

This file records the publication-time inventory for the local Học source tree.
It is not legal advice and does not change any third-party license. Preserve
upstream license and notice text in any distribution where the applicable
license requires it. It records engineering evidence and outstanding approval
gates; it is not legal clearance.

## Scope and ownership boundary

`LICENSE` applies only to Học-authored material for which the copyright owner
has authority to license it. It does not relicense ABP source, NuGet packages,
LeptonX Lite, generated template material, copied client assets, fonts, icons,
or third-party notices.

| Material | Classification | Publication treatment |
| --- | --- | --- |
| Học application and test code under `src/` and `test/` | Học-authored where authorship is established | Apache-2.0 only after Owner/copyright attestation. |
| ABP template files, module registrations, generated migration/snapshot, and resource mapping | Third-party-derived/generated | Retain ABP/Volosoft provenance and LGPL-3.0-only treatment; do not relabel as Học-owned. |
| `src/Hoc.Web/wwwroot/images/**` (14 image/SVG assets) | Exact-byte ABP 10.7.0 template assets | Retain ABP/Volosoft attribution and LGPL-3.0-only provenance; do not relabel as Học-owned. |
| `packages.lock.json` and `yarn.lock` | Dependency-resolution metadata | Keep as inventory; their contents remain governed by component licenses. |
| `wwwroot/libs/` and `node_modules/` | Restored third-party assets | Ignored and regenerated; do not publish from this tree without an approved redistribution/notice process. |

## Resolved component inventory

| Component | Resolved version(s) | License evidence | Authoritative source | Distribution consideration |
| --- | --- | --- | --- | --- |
| ABP Framework, OSS modules, and template-derived source/assets (`Volo.Abp.*`) | 10.7.0; official tag commit `c68e856d06615c6bd512b0649c10a4dc45af33e6` | LGPL-3.0-only in 141 local 10.7.0 NuGet nuspecs; the 14 committed image/SVG assets byte-match the official template | https://github.com/abpframework/abp/tree/10.7.0 | Preserve ABP/Volosoft attribution, LGPL-3.0-only provenance, upstream license text, and the source/tag pointer. Legal/Owner review must approve the final notice, modification-notice, and corresponding-source approach. |
| LeptonX Lite MVC/Razor NuGet theme | 5.7.0 | NuGet nuspec has no license expression or bundled license | https://www.nuget.org/packages/Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite/5.7.0 | External dependency only: no DLL, embedded resource extraction, `node_modules`, or restored client library is in the public-source candidate. Do not claim redistribution rights; authoritative terms remain an upstream clarification gate. |
| LeptonX Lite npm package | ~5.7.0 (locked in `yarn.lock`) | Installed package metadata declares LGPL-3.0 | https://www.npmjs.com/package/@abp/aspnetcore.mvc.ui.theme.leptonxlite | Dependency declaration only. If assets are later redistributed, retain exact upstream notices/licenses and reconcile them with the MVC/Razor NuGet package terms. |
| .NET runtime/package graph | net10.0; Microsoft packages resolved principally at 10.0.11 | Lock-file NuGet metadata; many Microsoft packages use MIT | https://www.nuget.org/ | Preserve package-specific notices for published binaries. Runtime servicing baseline is a separate deployment gate. |
| Client libraries and assets | Versions locked in `src/Hoc.Web/yarn.lock` | Representative installed metadata: Bootstrap, jQuery, SweetAlert2, Select2, and Moment are MIT; Font Awesome is `(CC-BY-4.0 AND OFL-1.1 AND MIT)` | https://www.npmjs.com/ | Preserve exact upstream notices and font attribution when distributing copied assets. |

The 15 `packages.lock.json` files contain 318 distinct resolved external
ID/version pairs: 142 `Volo.Abp.*`, 79 `Microsoft.*`, and 14 `System.*` pairs.
The current Web graph has 10 direct and 185 transitive packages. The lock files
are the authoritative per-package/version inventory for this checkout.

## Items requiring follow-up

- The exact 14 `src/Hoc.Web/wwwroot/images/**` assets match the ABP 10.7.0
  application template byte-for-byte. They are not restored client libraries
  and must retain the ABP LGPL provenance above.
- `NUglify` 1.21.17, `xunit.abstractions` 2.0.3, and LeptonX Lite 5.7.0 do not
  expose an SPDX expression in the inspected NuGet metadata.
- Several package records use file-based license references rather than SPDX
  expressions. Retrieve and preserve the referenced upstream texts before any
  redistribution.
- Owner/legal review must establish Học copyright ownership before the Apache
  license scope can be treated as effective.
- Obtain Volosoft's authoritative license and public-source redistribution
  terms for `Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite` 5.7.0 (MVC/Razor).
