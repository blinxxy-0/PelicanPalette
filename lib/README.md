# Compile-time references

`Automate.dll` is Pathoschild's [Automate](https://github.com/Pathoschild/StardewMods/tree/develop/Automate) 2.6.1 (MIT License, copyright Pathoschild; see `Automate.LICENSE.txt`), kept here only so the optional Automate support (`ArtStudio.Automate/`, built into its own `ArtStudio.Automate.dll` so that `ArtStudio.dll` never refers to Automate) compiles without Automate installed. It's referenced with `Private=False`, so it's never copied into the mod's build output or release zip; players use their own installed copy.
