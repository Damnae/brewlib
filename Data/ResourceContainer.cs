﻿using BrewLib.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace BrewLib.Data
{
    public interface ResourceContainer
    {
        IEnumerable<string> ResourceNames { get; }

        Stream GetStream(string filename, ResourceSource sources = ResourceSource.Embedded);
        byte[] GetBytes(string filename, ResourceSource sources = ResourceSource.Embedded);
        string GetString(string filename, ResourceSource sources = ResourceSource.Embedded);

        SafeWriteStream GetWriteStream(string filename);
    }

    [Flags]
    public enum ResourceSource
    {
        Embedded = 1,
        Relative = 2,
        Absolute = 4,

        None = 0,
        Local = Embedded | Relative,
        Any = Embedded | Relative | Absolute,
    }
}
