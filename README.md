# MCSC6002_InventoryService
Service responsible for stock items: it consumes product events and tracks each unit.

## Shared libraries

AuthLibrary and ResponseLibrary are bundled in `nuget-local/` (their GitHub Packages feed requires a login). `nuget.config` maps both names to that folder only, because nuget.org hosts unrelated packages with the same names.
