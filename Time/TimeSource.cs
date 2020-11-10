namespace BrewLib.Time
{
    public interface ReadOnlyTimeSource
    {
        double Current { get; }

        double TimeFactor { get; }
        bool Playing { get; }
    }

    public interface TimeSource : ReadOnlyTimeSource
    {
        new bool Playing { get; set; }
        new double TimeFactor { get; set; }

        bool Seek(double time);
    }
}
