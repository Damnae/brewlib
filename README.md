# BrewLib

## Native audio dependencies

BrewLib references `ManagedBass` and `ManagedBass.Fx`, which are managed wrappers
and do not contain the native BASS libraries. Applications using BrewLib audio
must provide `bass.dll` and `bass_fx.dll` in their output directory, with binaries
that match the application's target architecture.

Native runtime assets belong to the executable application so that it can select
the correct platform and architecture through its dependency management setup.
