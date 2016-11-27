using System;


namespace Dadstorm
{
    public interface RepServices
    {
        void Start(RepInfo info);

        void Interval(string x_ms);

        void Status();

        void Crash();

        void Freeze();

        void Unfreeze();

        void ping(string msg);

        RepInfo getRepInfoFromRep();

        void updateRepInfo(RepInfo repInfo);
    }

    public interface PMServices
    {
        void SendToLog(string msg);
    }
}
