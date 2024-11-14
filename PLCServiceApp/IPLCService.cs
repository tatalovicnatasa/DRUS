using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace PLCServiceApp
{

    [ServiceContract]
    public interface IPub
        // ovo se salje HMI - salju se vrednosti novih promenljivih
    {
        [OperationContract(IsOneWay = true)]
        void SendVariable(string message, int value); // metoda za slanje poruka od Publishera
    }


    [ServiceContract(CallbackContract = typeof(ICallback))] // dvosmerna komunikacija, ima povratni kanal 
    public interface ISub // registracija HMI da moze da prati promenljive
    {
        [OperationContract(IsOneWay = true)] // IMA SMISLA
        void InitSub(string nameOrAddress); // subsrciber se registruje i kaze sta hoce da prati odnosno vrednost ilia dresu
    }
    // Metoda koja se poziva kad stigne nova vrednost promenljive 
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void VariableValueUpdated(string nameOrAddress, int value); // poruka je stigla 
    }

}
