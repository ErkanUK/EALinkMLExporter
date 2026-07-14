# EA17 LinkML Exporter

A 64-bit Sparx Enterprise Architect 17 add-in that exports the package selected in the Browser as:

- a LinkML YAML schema;
- Markdown documentation, similar in spirit to EA's F8 document generation;
- an editable draw.io class diagram; and
- an SVG diagram preview embedded in the Markdown.

Classes, attributes, notes, enumerations, generalizations, associations, aggregations/compositions, roles, and multiplicities are included. Child packages are traversed recursively.

## Install

Prerequisites: Enterprise Architect 17 64-bit and the .NET 9 Desktop Runtime (x64). The .NET 9 SDK is needed only when rebuilding from source.

1. Close Enterprise Architect.
2. Open PowerShell in this folder.
3. Run `powershell -ExecutionPolicy Bypass -File .\install.ps1`.
4. Restart Enterprise Architect.

If an earlier package was installed but the add-in is missing from EA, close EA and run `powershell -ExecutionPolicy Bypass -File .\repair-ea17-registration.ps1`, then restart EA.

The script installs the included prebuilt add-in for the current Windows user and adds the required 64-bit EA and COM registry entries. Administrator rights are not required. To compile it again, add `-BuildFromSource`; if EA is installed elsewhere, also use `-EAInstallDir 'D:\path\to\EA'`.

## Use

1. Select a package in EA's Browser.
2. Open **Specialize > Add-Ins > LinkML Documentation** (the exact ribbon location can vary with workspace layout).
3. Choose **Export selected package…** and select a destination folder.

The add-in creates a subfolder named after the package. Open the `.md` file for the generated document and the `.drawio` file in draw.io/diagrams.net for editing.

## LinkML mapping

| UML | LinkML |
|---|---|
| Class | `classes` entry |
| Attribute | Inline class `attributes` entry |
| Generalization | `is_a` |
| Enumeration | `enums` / `permissible_values` |
| Association end | Attribute whose range is the related class |
| Lower bound > 0 | `required: true` |
| Upper bound > 1 or `*` | `multivalued: true` |

The generated schema uses `https://example.org/<package>` as a placeholder namespace. Change it to the project's canonical URI after export.

## Uninstall

Close EA, then run `powershell -ExecutionPolicy Bypass -File .\uninstall.ps1`.
