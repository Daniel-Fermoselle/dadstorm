

using System;

namespace Dadstorm
{
    public interface RepServices
    {
        void Start();

        void Interval(string x_ms);

        void Status();

        void Crash();

        void Freeze();

        void Unfreeze();

        void ShutDown();

        void Populate(RepInfo info);
    }

    public interface PMServices
    {
        void SendToLog(string msg);
    }
}
