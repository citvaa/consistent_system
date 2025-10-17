using System;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace consistent_system
{
    [ServiceContract]
    public interface ITemperatureUnit
    {
        [OperationContract]
        double ReadTemperature();
    }
}