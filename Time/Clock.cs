using System.Diagnostics;

namespace BrewLib.Time
{
    public class Clock : TimeSource
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private double timeOrigin;

        public double Current => timeOrigin + stopwatch.Elapsed.TotalSeconds * timeFactor;

        private double timeFactor = 1;
        public double TimeFactor
        {
            get => timeFactor;
            set
            {
                if (timeFactor == value)
                    return;

                var elapsed = stopwatch.Elapsed.TotalSeconds;
                var previousTime = timeOrigin + elapsed * timeFactor;
                timeFactor = value;
                timeOrigin = previousTime - elapsed * timeFactor;
            }
        }

        private bool playing;
        public bool Playing
        {
            get => playing;
            set
            {
                if (playing == value)
                    return;

                playing = value;

                if (playing)
                    stopwatch.Start();
                else
                    stopwatch.Stop();
            }
        }

        public bool Seek(double time)
        {
            timeOrigin = time - stopwatch.Elapsed.TotalSeconds * timeFactor;
            return true;
        }
    }
}
