using System;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace consistent_system
{
    [ServiceContract]
    public interface ITemperatureSensor
    {
        [OperationContract]
        /*[FaultContract(typeof(string))]*/
        double ReadTemperature();

        [OperationContract]
        void SyncTemperature(double value);
    }
}